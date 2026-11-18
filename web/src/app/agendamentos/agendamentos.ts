import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  Injector,
  afterNextRender,
  computed,
  inject,
  signal,
} from "@angular/core";
import { rxResource } from "@angular/core/rxjs-interop";
import { RouterLink } from "@angular/router";
import { Api, type Agendamento, ErroRede, mensagemDeErro } from "../api";
import {
  dataLonga,
  diaCurto,
  diaEMes,
  diaLocal,
  dinheiro,
  duracaoEmTexto,
  formatarHora,
  iniciais,
  inicioDoDia,
  juntar,
  minutosDoDia,
  minutosEntre,
  numeroDoDia,
  segundaDaSemana,
  somarDias,
} from "../datas";
import { MensagemErro, Vazio } from "../estados";

/** Status como a tela mostra (README §2.1). Faltou e Confirmado dependem de colunas que o banco ainda não tem. */
export type StatusTela = "aguardando" | "confirmado" | "feito" | "cancelado";

/**
 * Origem no banco → status da tela. `confirmado` sem presença confirmada pelo cliente é
 * "Aguardando" (tabela de status do README); a coluna presenca_confirmada_em ainda não existe.
 */
export function statusDaTela(a: Agendamento): StatusTela {
  if (a.status === "concluido") return "feito";
  if (a.status === "cancelado") return "cancelado";
  return "aguardando";
}

export const ROTULO_STATUS: Record<StatusTela, string> = {
  aguardando: "Aguardando",
  confirmado: "Confirmado",
  feito: "Feito",
  cancelado: "Cancelado",
};

/** Traço do ícone de cada status (grade 24, traço 2,6). */
export const ICONE_STATUS: Record<StatusTela, string> = {
  aguardando: "M12 7v5l3 2M12 21a9 9 0 1 1 0-18 9 9 0 0 1 0 18z",
  confirmado: "M5 12.5l4.5 4.5L19 7.5",
  feito: "M2 13l4 4 8.5-9M10 15.5l1.5 1.5L20 8.5",
  cancelado: "M6 6l12 12M18 6L6 18",
};

const HORA_PX = 72; // 1 h na grade (--ds-l-hora-agenda)
const PRIMEIRA_HORA = 8;
const ULTIMA_HORA = 19;

interface Bloco {
  a: Agendamento;
  status: StatusTela;
  topo: number;
  altura: number;
  esquerda: number; // % da coluna, para horários que se sobrepõem
  largura: number;
  linhas: 1 | 2;
  rotulo: string;
}

interface Coluna {
  dia: string;
  rotulo: string;
  numero: string;
  hoje: boolean;
  blocos: Bloco[];
}

@Component({
  selector: "app-agendamentos",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, MensagemErro, Vazio],
  templateUrl: "./agendamentos.html",
  host: { "(document:keydown.escape)": "fecharDetalhe()" },
})
export class Agendamentos {
  private readonly api = inject(Api);
  private readonly injector = inject(Injector);

  /** Relógio da linha "agora" e de "hoje": anda de minuto em minuto. */
  protected readonly agora = signal(new Date());
  protected readonly hoje = computed(() => diaLocal(this.agora()));

  protected readonly modo = signal<"semana" | "dia">("semana");
  /** Dia de referência: na semana, qualquer dia dela; no dia, o próprio. */
  protected readonly referencia = signal(diaLocal(new Date()));
  protected readonly mostrarCancelados = signal(false);
  protected readonly selecionadoId = signal<string | null>(null);

  constructor() {
    const relogio = setInterval(() => this.agora.set(new Date()), 60_000);
    inject(DestroyRef).onDestroy(() => clearInterval(relogio));
  }

  /** Período pedido à API: [de, ate). Semana de segunda a domingo. */
  protected readonly periodo = computed(() => {
    const de = this.modo() === "semana" ? segundaDaSemana(this.referencia()) : this.referencia();
    return { de, dias: this.modo() === "semana" ? 7 : 1 };
  });

  // Troca de período cancela a busca anterior: resposta velha não sobrescreve a nova.
  protected readonly agendamentos = rxResource({
    params: () => this.periodo(),
    stream: ({ params }) =>
      this.api.listarAgendamentos(inicioDoDia(params.de), inicioDoDia(somarDias(params.de, params.dias))),
  });

  /** Esqueleto só quando chegam dados novos (troca de período); recarregar o mesmo período não pisca. */
  protected readonly carregando = computed(() => this.agendamentos.status() === "loading");

  protected readonly erro = computed(() => this.agendamentos.error() ?? null);
  protected readonly semConexao = computed(() => this.erro() instanceof ErroRede);
  protected readonly mensagemErro = computed(() => (this.erro() ? mensagemDeErro(this.erro()) : ""));

  private readonly lista = computed(() => (this.agendamentos.hasValue() ? this.agendamentos.value() : []));

  protected readonly totalCancelados = computed(() => this.lista().filter((a) => a.status === "cancelado").length);

  private readonly visiveis = computed(() =>
    this.lista().filter((a) => this.mostrarCancelados() || a.status !== "cancelado"),
  );

  /** Dias na grade: segunda a sábado; o domingo só entra se tiver horário ou se for hoje. */
  protected readonly dias = computed(() => {
    const { de, dias } = this.periodo();
    if (dias === 1) return [de];
    const semana = Array.from({ length: 7 }, (_, i) => somarDias(de, i));
    const temDomingo = this.visiveis().some((a) => diaLocal(new Date(a.inicio)) === semana[6]);
    return temDomingo || semana[6] === this.hoje() ? semana : semana.slice(0, 6);
  });

  /** Faixa de horas: 08–19 h, esticada se algum horário cair fora. */
  protected readonly horas = computed(() => {
    let primeira = PRIMEIRA_HORA;
    let ultima = ULTIMA_HORA;
    for (const a of this.visiveis()) {
      const inicio = minutosDoDia(a.inicio);
      const fim = diaLocal(new Date(a.fim)) === diaLocal(new Date(a.inicio)) ? minutosDoDia(a.fim) : 24 * 60;
      primeira = Math.min(primeira, Math.floor(inicio / 60));
      ultima = Math.max(ultima, Math.ceil(fim / 60));
    }
    return { primeira, ultima, rotulos: Array.from({ length: ultima - primeira }, (_, i) => primeira + i) };
  });

  protected readonly alturaGrade = computed(() => (this.horas().ultima - this.horas().primeira) * HORA_PX);

  protected readonly colunas = computed<Coluna[]>(() => {
    const porDia = new Map<string, Agendamento[]>();
    for (const a of this.visiveis()) {
      const dia = diaLocal(new Date(a.inicio));
      porDia.set(dia, [...(porDia.get(dia) ?? []), a]);
    }
    return this.dias().map((dia) => {
      const hoje = dia === this.hoje();
      return {
        dia,
        rotulo: hoje ? `${diaCurto(dia)}, hoje` : diaCurto(dia),
        numero: numeroDoDia(dia),
        hoje,
        blocos: this.posicionar(porDia.get(dia) ?? []),
      };
    });
  });

  /** Topo da linha "agora" na coluna de hoje; nulo fora da faixa de horas. */
  protected readonly agoraTopo = computed(() => {
    const min = minutosDoDia(this.agora()) - this.horas().primeira * 60;
    const px = (min / 60) * HORA_PX;
    return px < 0 || px > this.alturaGrade() ? null : px;
  });

  protected readonly titulo = computed(() => {
    const { de, dias } = this.periodo();
    if (dias === 1) return dataLonga(de);
    if (this.contemHoje()) return dataLonga(this.hoje());
    const ultimo = this.dias().at(-1)!;
    return new Date(`${de}T00:00`).getMonth() === new Date(`${ultimo}T00:00`).getMonth()
      ? `${numeroDoDia(de)} a ${diaEMes(ultimo)}`
      : `${diaEMes(de)} a ${diaEMes(ultimo)}`;
  });

  private readonly contemHoje = computed(() => {
    const { de, dias } = this.periodo();
    return this.hoje() >= de && this.hoje() < somarDias(de, dias);
  });

  /** "5 horários hoje: 1 feito e 4 aguardando." Cancelados não contam. */
  protected readonly resumo = computed(() => {
    const semana = this.periodo().dias === 7;
    const soHoje = this.contemHoje();
    const onde = soHoje ? "hoje" : semana ? "nesta semana" : "nesse dia";
    const contados = this.lista().filter(
      (a) => a.status !== "cancelado" && (!soHoje || diaLocal(new Date(a.inicio)) === this.hoje()),
    );
    if (contados.length === 0) return `Nenhum horário ${onde}.`;
    const conta = (s: StatusTela) => contados.filter((a) => statusDaTela(a) === s).length;
    const partes: string[] = [];
    const feitos = conta("feito");
    const confirmados = conta("confirmado");
    const aguardando = conta("aguardando");
    if (feitos) partes.push(`${feitos} ${feitos === 1 ? "feito" : "feitos"}`);
    if (confirmados) partes.push(`${confirmados} ${confirmados === 1 ? "confirmado" : "confirmados"}`);
    if (aguardando) partes.push(`${aguardando} aguardando`);
    const total = `${contados.length} ${contados.length === 1 ? "horário" : "horários"} ${onde}`;
    return partes.length ? `${total}: ${juntar(partes)}.` : `${total}.`;
  });

  protected readonly rotuloAnterior = computed(() => (this.modo() === "semana" ? "Semana anterior" : "Dia anterior"));
  protected readonly rotuloProximo = computed(() => (this.modo() === "semana" ? "Próxima semana" : "Próximo dia"));

  protected readonly selecionado = computed(() => {
    const id = this.selecionadoId();
    const a = id ? this.visiveis().find((x) => x.id === id) : undefined;
    if (!a) return null;
    const status = statusDaTela(a);
    return {
      a,
      status,
      rotuloStatus: ROTULO_STATUS[status],
      icone: ICONE_STATUS[status],
      inicio: formatarHora(a.inicio),
      fim: formatarHora(a.fim),
      dia: dataLonga(diaLocal(new Date(a.inicio))),
      duracao: duracaoEmTexto(minutosEntre(a.inicio, a.fim)),
      valor: dinheiro(a.valor_centavos),
      iniciais: iniciais(a.cliente_nome),
    };
  });

  /** Blocos do esqueleto, nas medidas de horários de 50 e 30 min (deslocados por coluna). */
  protected readonly esqueleto = [
    { topo: 73, altura: 57 },
    { topo: 289, altura: 33 },
    { topo: 505, altura: 57 },
  ];

  protected readonly ROTULO_STATUS = ROTULO_STATUS;
  protected readonly ICONE_STATUS = ICONE_STATUS;
  protected readonly formatarHora = formatarHora;
  protected readonly pad = (h: number) => `${String(h).padStart(2, "0")}:00`;

  protected andar(direcao: -1 | 1) {
    this.referencia.update((d) => somarDias(d, direcao * (this.modo() === "semana" ? 7 : 1)));
  }

  protected irParaHoje() {
    this.referencia.set(this.hoje());
  }

  protected trocarModo(modo: "semana" | "dia") {
    if (modo === this.modo()) return;
    // Da semana para o dia: hoje se estiver nela, senão a segunda.
    if (modo === "dia" && !this.contemHoje()) this.referencia.set(this.periodo().de);
    this.modo.set(modo);
  }

  protected selecionar(id: string) {
    if (this.selecionadoId() === id) {
      this.fecharDetalhe();
      return;
    }
    this.selecionadoId.set(id);
    // Abaixo de 1024 px o painel fica depois da grade: leva o usuário até ele.
    if (window.matchMedia("(min-width: 1024px)").matches) return;
    afterNextRender(
      () => {
        const painel = document.getElementById("painel-detalhe");
        const semAnimacao = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
        painel?.focus({ preventScroll: true });
        painel?.scrollIntoView({ behavior: semAnimacao ? "auto" : "smooth", block: "start" });
      },
      { injector: this.injector },
    );
  }

  /** Esc ou o botão fechar: some o painel e o foco volta ao bloco de origem. */
  protected fecharDetalhe() {
    const id = this.selecionadoId();
    if (!id) return;
    this.selecionadoId.set(null);
    document.getElementById(`bloco-${id}`)?.focus();
  }

  /** Topo = início, altura = duração; horários que se sobrepõem dividem a coluna. */
  private posicionar(lista: Agendamento[]): Bloco[] {
    const primeira = this.horas().primeira * 60;
    const ordenados = [...lista].sort((x, y) => Date.parse(x.inicio) - Date.parse(y.inicio));
    const blocos: Bloco[] = [];
    let grupo: { bloco: Bloco; fim: number; faixa: number }[] = [];
    let fimDoGrupo = -1;

    const fecharGrupo = () => {
      const faixas = Math.max(1, ...grupo.map((g) => g.faixa + 1));
      for (const g of grupo) {
        g.bloco.largura = 100 / faixas;
        g.bloco.esquerda = g.faixa * (100 / faixas);
      }
      grupo = [];
    };

    for (const a of ordenados) {
      const inicio = minutosDoDia(a.inicio);
      const fim =
        diaLocal(new Date(a.fim)) === diaLocal(new Date(a.inicio)) ? minutosDoDia(a.fim) : 24 * 60;
      if (inicio >= fimDoGrupo) fecharGrupo();
      const ocupadas = new Set(grupo.filter((g) => g.fim > inicio).map((g) => g.faixa));
      let faixa = 0;
      while (ocupadas.has(faixa)) faixa++;
      const altura = Math.max(((fim - inicio) / 60) * HORA_PX - 3, 18);
      const status = statusDaTela(a);
      const bloco: Bloco = {
        a,
        status,
        topo: ((inicio - primeira) / 60) * HORA_PX + 1,
        altura,
        esquerda: 0,
        largura: 100,
        // < 41 px: só hora e ícone; daí para cima, também o nome (o serviço ainda não vem da API).
        linhas: altura < 41 ? 1 : 2,
        rotulo: `${formatarHora(a.inicio)}, ${a.cliente_nome}, ${ROTULO_STATUS[status]}`,
      };
      grupo.push({ bloco, fim, faixa });
      blocos.push(bloco);
      fimDoGrupo = Math.max(fimDoGrupo, fim);
    }
    fecharGrupo();
    return blocos;
  }
}
