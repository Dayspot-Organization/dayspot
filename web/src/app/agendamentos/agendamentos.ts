import { NgTemplateOutlet } from "@angular/common";
import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, linkedSignal, signal } from "@angular/core";
import { rxResource } from "@angular/core/rxjs-interop";
import { RouterLink } from "@angular/router";
import {
  LucideCheckCheck,
  LucideChevronLeft,
  LucideChevronRight,
  LucideCircleCheck,
  LucideCircleExclamationPoint,
  LucideCircleX,
  LucideClock,
  LucidePlus,
  LucideX,
} from "@lucide/angular";
import { Api, type Agendamento, mensagemDeErro } from "../api";
import {
  dataLonga,
  diaCurto,
  diaEMes,
  diaLocal,
  diaLongo,
  dinheiro,
  duracaoEmTexto,
  formatarHora,
  inicioDoDia,
  minutosDoDia,
  minutosEntre,
  numeroDoDia,
  segundaDaSemana,
  somarDias,
} from "../datas";

// Agenda da semana da proposta v3 (DESIGN.md › Peças da Agenda): barra, cabeçalho dos dias,
// grade de horas e detalhe. A equipe, o telefone, o serviço e as ações do detalhe esperam a API.

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

const HORA_PX = 72; // 1 h na grade (--ds-l-hora); meia hora, 36
const PRIMEIRA_HORA = 8;
const ULTIMA_HORA = 19;

/** 570 → "09:30". */
const hhmm = (minutos: number) =>
  `${String(Math.floor(minutos / 60)).padStart(2, "0")}:${String(minutos % 60).padStart(2, "0")}`;

interface Bloco {
  a: Agendamento;
  status: StatusTela;
  inicio: number; // minutos do dia
  fim: number;
  topo: number;
  altura: number;
  esquerda: number; // % da coluna, para horários que se sobrepõem
  largura: number;
  curto: boolean;
  nome: string;
  rotulo: string;
}

interface Coluna {
  dia: string;
  rotulo: string;
  numero: string;
  hoje: boolean;
  blocos: Bloco[];
}

/** Meia hora livre da grade: dia e início em minutos do dia. */
interface MeiaHora {
  dia: string;
  inicio: number;
}

@Component({
  selector: "app-agendamentos",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    NgTemplateOutlet,
    RouterLink,
    LucideCheckCheck,
    LucideChevronLeft,
    LucideChevronRight,
    LucideCircleCheck,
    LucideCircleExclamationPoint,
    LucideCircleX,
    LucideClock,
    LucidePlus,
    LucideX,
  ],
  templateUrl: "./agendamentos.html",
  host: { "(document:keydown.escape)": "fecharDetalhe()" },
})
export class Agendamentos {
  private readonly api = inject(Api);

  /** Relógio da linha "agora" e de "hoje": anda de minuto em minuto. */
  protected readonly agora = signal(new Date());
  protected readonly hoje = computed(() => diaLocal(this.agora()));

  protected readonly modo = signal<"semana" | "dia">("semana");
  /** Dia de referência: na semana, qualquer dia dela; no dia, o próprio. */
  protected readonly referencia = signal(diaLocal(new Date()));
  /** Agendamento aberto no painel de detalhe; trocar de período ou de modo fecha. */
  protected readonly selecionadoId = linkedSignal<unknown, string | null>({
    source: () => this.periodo(),
    computation: () => null,
  });
  /** A meia hora livre sob o mouse, onde o novo horário entraria. */
  protected readonly vazio = signal<MeiaHora | null>(null, {
    equal: (x, y) => x?.dia === y?.dia && x?.inicio === y?.inicio,
  });

  constructor() {
    const relogio = setInterval(() => this.agora.set(new Date()), 60_000);
    inject(DestroyRef).onDestroy(() => clearInterval(relogio));
  }

  /** Período pedido à API: [de, ate). Semana de segunda a domingo. */
  // Mesmo de e dias = mesmo período: sem o equal, cada recálculo seria um objeto novo e
  // fecharia o detalhe (selecionadoId) e recarregaria a lista à toa.
  protected readonly periodo = computed(
    () => {
      const de = this.modo() === "semana" ? segundaDaSemana(this.referencia()) : this.referencia();
      return { de, dias: this.modo() === "semana" ? 7 : 1 };
    },
    { equal: (a, b) => a.de === b.de && a.dias === b.dias },
  );

  // Troca de período cancela a busca anterior: resposta velha não sobrescreve a nova.
  protected readonly agendamentos = rxResource({
    params: () => this.periodo(),
    stream: ({ params }) =>
      this.api.listarAgendamentos(inicioDoDia(params.de), inicioDoDia(somarDias(params.de, params.dias))),
  });

  protected readonly carregando = computed(() => this.agendamentos.status() === "loading");
  protected readonly erro = computed(() => this.agendamentos.error() ?? null);
  protected readonly mensagemErro = computed(() => (this.erro() ? mensagemDeErro(this.erro()) : ""));

  private readonly lista = computed(() => (this.agendamentos.hasValue() ? this.agendamentos.value() : []));

  /** Dias na grade: segunda a sábado; o domingo só entra se tiver horário ou se for hoje. */
  protected readonly dias = computed(() => {
    const { de, dias } = this.periodo();
    if (dias === 1) return [de];
    const semana = Array.from({ length: 7 }, (_, i) => somarDias(de, i));
    const temDomingo = this.lista().some((a) => diaLocal(new Date(a.inicio)) === semana[6]);
    return temDomingo || semana[6] === this.hoje() ? semana : semana.slice(0, 6);
  });

  /** Faixa de horas: 08–19 h, esticada se algum horário cair fora. */
  protected readonly horas = computed(() => {
    let primeira = PRIMEIRA_HORA;
    let ultima = ULTIMA_HORA;
    for (const a of this.lista()) {
      primeira = Math.min(primeira, Math.floor(minutosDoDia(a.inicio) / 60));
      ultima = Math.max(ultima, Math.ceil(this.fimNoDia(a) / 60));
    }
    return { primeira, ultima };
  });

  protected readonly alturaGrade = computed(() => (this.horas().ultima - this.horas().primeira) * HORA_PX);

  protected readonly colunas = computed<Coluna[]>(() => {
    const porDia = new Map<string, Agendamento[]>();
    for (const a of this.lista()) {
      const dia = diaLocal(new Date(a.inicio));
      porDia.set(dia, [...(porDia.get(dia) ?? []), a]);
    }
    return this.dias().map((dia) => ({
      dia,
      rotulo: diaCurto(dia),
      numero: numeroDoDia(dia),
      hoje: dia === this.hoje(),
      blocos: this.posicionar(porDia.get(dia) ?? []),
    }));
  });

  /** Topo da linha "agora": só com hoje na grade e dentro da faixa de horas. */
  protected readonly agoraTopo = computed(() => {
    if (!this.dias().includes(this.hoje())) return null;
    const px = ((minutosDoDia(this.agora()) - this.horas().primeira * 60) / 60) * HORA_PX;
    return px < 0 || px > this.alturaGrade() ? null : px;
  });

  /** Horas da coluna da esquerda, centradas na linha; some a que a pílula de agora cobriria. */
  protected readonly rotulosHoras = computed(() => {
    const { primeira, ultima } = this.horas();
    const agora = this.agoraTopo();
    return Array.from({ length: ultima - primeira + 1 }, (_, i) => ({
      texto: hhmm((primeira + i) * 60),
      topo: i * HORA_PX,
      escondido: agora !== null && Math.abs(i * HORA_PX - agora) < 18,
    }));
  });

  /** Semana: "23 a 28 de novembro"; dia: "Quarta, 25 de novembro". */
  protected readonly titulo = computed(() => {
    const { de, dias } = this.periodo();
    if (dias === 1) return dataLonga(de);
    const ultimo = this.dias().at(-1)!;
    return `${de.slice(0, 7) === ultimo.slice(0, 7) ? numeroDoDia(de) : diaEMes(de)} a ${diaEMes(ultimo)}`;
  });

  /** Só o que pede ação: o total do período e quantos ainda não responderam. Cancelados não contam. */
  protected readonly contagem = computed(() => {
    if (!this.agendamentos.hasValue()) return null;
    const ativos = this.lista().filter((a) => a.status !== "cancelado");
    const total = ativos.length;
    return {
      total:
        total === 0
          ? `${this.modo() === "semana" ? "Semana" : "Dia"} livre, sem horário marcado`
          : `${total} ${total === 1 ? "horário" : "horários"}`,
      aguardando: ativos.filter((a) => statusDaTela(a) === "aguardando").length,
    };
  });

  protected readonly rotuloAnterior = computed(() => (this.modo() === "semana" ? "Semana anterior" : "Dia anterior"));
  protected readonly rotuloProximo = computed(() => (this.modo() === "semana" ? "Próxima semana" : "Próximo dia"));

  protected readonly selecionado = computed(() => {
    const id = this.selecionadoId();
    const a = id ? this.lista().find((x) => x.id === id) : undefined;
    if (!a) return null;
    const status = statusDaTela(a);
    const dia = diaLocal(new Date(a.inicio));
    return {
      a,
      status,
      rotuloStatus: ROTULO_STATUS[status],
      inicio: formatarHora(a.inicio),
      fim: formatarHora(a.fim),
      dia: `${diaLongo(dia)}, ${diaEMes(dia)}`,
      duracao: duracaoEmTexto(minutosEntre(a.inicio, a.fim)),
      valor: dinheiro(a.valor_centavos),
    };
  });

  protected readonly HORA_PX = HORA_PX;
  protected readonly hhmm = hhmm;
  protected readonly formatarHora = formatarHora;

  protected andar(direcao: -1 | 1) {
    this.referencia.update((d) => somarDias(d, direcao * (this.modo() === "semana" ? 7 : 1)));
  }

  protected irParaHoje() {
    this.referencia.set(this.hoje());
  }

  protected trocarModo(modo: "semana" | "dia") {
    if (modo === this.modo()) return;
    // Da semana para o dia: hoje se estiver nela, senão a segunda.
    if (modo === "dia" && !this.dias().includes(this.hoje())) this.referencia.set(this.periodo().de);
    this.modo.set(modo);
  }

  /** O detalhe abre ao lado, empurrando a grade; clicar de novo no mesmo horário fecha. */
  protected selecionar(id: string) {
    if (this.selecionadoId() === id) this.fecharDetalhe();
    else this.selecionadoId.set(id);
  }

  /** Esc ou o X: some o detalhe e o foco volta ao horário de origem. */
  protected fecharDetalhe() {
    const id = this.selecionadoId();
    if (!id) return;
    this.selecionadoId.set(null);
    document.getElementById(`bloco-${id}`)?.focus();
  }

  /**
   * Mostra no vazio onde o novo horário entraria: a meia hora sob o ponteiro, se der para marcar
   * nela (sem horário em cima, dentro da grade e ainda por vir). O clique abre o novo horário com
   * o dia e a hora preenchidos.
   */
  protected aoMover(e: MouseEvent, c: Coluna) {
    this.vazio.set(this.meiaHoraLivre(e, c));
  }

  private meiaHoraLivre(e: MouseEvent, c: Coluna): MeiaHora | null {
    if ((e.target as Element).closest(".agenda-bloco")) return null;
    const y = e.clientY - (e.currentTarget as Element).getBoundingClientRect().top;
    const inicio = this.horas().primeira * 60 + Math.floor(y / (HORA_PX / 2)) * 30;
    const fim = inicio + 30;
    if (inicio < this.horas().primeira * 60 || fim > this.horas().ultima * 60) return null;
    if (new Date(`${c.dia}T${hhmm(inicio)}`) < this.agora()) return null;
    const ocupada = c.blocos.some((b) => b.status !== "cancelado" && inicio < b.fim && fim > b.inicio);
    return ocupada ? null : { dia: c.dia, inicio };
  }

  /** Fim em minutos do dia do início; horário que passa da meia-noite vai até o fim da grade. */
  private fimNoDia(a: Agendamento) {
    return diaLocal(new Date(a.fim)) === diaLocal(new Date(a.inicio)) ? minutosDoDia(a.fim) : 24 * 60;
  }

  /**
   * Topo = início, altura = duração menos 2 px de folga entre horários seguidos; os que se
   * sobrepõem dividem a coluna. O de até 30 min fica numa linha, com o sobrenome abreviado.
   */
  private posicionar(lista: Agendamento[]): Bloco[] {
    const primeira = this.horas().primeira * 60;
    const ordenados = [...lista].sort((x, y) => Date.parse(x.inicio) - Date.parse(y.inicio));
    const blocos: Bloco[] = [];
    let grupo: { bloco: Bloco; faixa: number }[] = [];
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
      const fim = this.fimNoDia(a);
      if (inicio >= fimDoGrupo) fecharGrupo();
      const ocupadas = new Set(grupo.filter((g) => g.bloco.fim > inicio).map((g) => g.faixa));
      let faixa = 0;
      while (ocupadas.has(faixa)) faixa++;
      const status = statusDaTela(a);
      const curto = fim - inicio <= 30;
      const bloco: Bloco = {
        a,
        status,
        inicio,
        fim,
        topo: ((inicio - primeira) / 60) * HORA_PX + 1,
        altura: Math.max(((fim - inicio) / 60) * HORA_PX - 2, 18),
        esquerda: 0,
        largura: 100,
        curto,
        nome: curto ? a.cliente_nome.replace(/ (\S)\S*$/, " $1.") : a.cliente_nome,
        rotulo: `${formatarHora(a.inicio)} às ${formatarHora(a.fim)}, ${a.cliente_nome}, ${ROTULO_STATUS[status]}`,
      };
      grupo.push({ bloco, faixa });
      blocos.push(bloco);
      fimDoGrupo = Math.max(fimDoGrupo, fim);
    }
    fecharGrupo();
    return blocos;
  }
}
