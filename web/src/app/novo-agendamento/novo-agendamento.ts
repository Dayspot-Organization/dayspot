import { NgTemplateOutlet } from "@angular/common";
import { ChangeDetectionStrategy, Component, computed, inject, signal } from "@angular/core";
import { rxResource } from "@angular/core/rxjs-interop";
import { ActivatedRoute, Router } from "@angular/router";
import { firstValueFrom } from "rxjs";
import { Api, ApiError, ErroRede, mensagemDeErro } from "../api";
import {
  dataLonga,
  diaCurto,
  diaEMes,
  diaLocal,
  diaLongo,
  dinheiro,
  duracaoEmTexto,
  formatarHora,
  horaEmFrase,
  iniciais,
  inicioDoDia,
  nomeDoMes,
  numeroDoDia,
  somarDias,
} from "../datas";
import { MensagemErro, Vazio } from "../estados";
import { Logo } from "../layout";

// Padrão C do sistema de design (passos + resumo). A mesma tela serve a dois públicos:
// - página do cliente (/agendar, data.publica): serviço, com quem, dia, horário e os dados dele;
// - painel (/agendamentos/novo): o dono escolhe o cliente e segue os mesmos passos.
// A duração vem do serviço. Horários de 30 em 30 min entre 08:00 e 19:00 até a disponibilidade
// existir; o 409 da API continua sendo a palavra final.

const PRIMEIRO_MINUTO = 8 * 60;
const ULTIMO_MINUTO = 19 * 60;
const PASSO = 30;
const DIAS_NA_FILA = 8;

interface Horario {
  hora: string; // "09:30"
  inicio: Date;
  fim: Date;
  ocupado: boolean;
}

const semAcento = (s: string) =>
  s
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .toLowerCase();

const soDigitos = (s: string) => s.replace(/\D/g, "");

/** "(11) 98877-4410" para números do Brasil, com ou sem 55; senão, como veio. */
export function formatarTelefone(t: string): string {
  const d = soDigitos(t).replace(/^55(?=\d{10,11}$)/, "");
  if (d.length === 11) return `(${d.slice(0, 2)}) ${d.slice(2, 7)}-${d.slice(7)}`;
  if (d.length === 10) return `(${d.slice(0, 2)}) ${d.slice(2, 6)}-${d.slice(6)}`;
  return t;
}

/** WhatsApp do Brasil: DDD + número (10 ou 11 dígitos), com ou sem 55 na frente. */
const whatsappValido = (t: string) => /^(55)?\d{10,11}$/.test(soDigitos(t));

@Component({
  selector: "app-novo-agendamento",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgTemplateOutlet, MensagemErro, Vazio, Logo],
  templateUrl: "./novo-agendamento.html",
})
export class NovoAgendamento {
  private readonly api = inject(Api);
  private readonly router = inject(Router);

  /** Página do cliente (sem menu): ele mesmo agenda e informa nome e WhatsApp no fim. */
  protected readonly publica = inject(ActivatedRoute).snapshot.data["publica"] === true;
  protected readonly hoje = diaLocal(new Date());

  // ── Dados da empresa ────────────────────────────────────────
  protected readonly empresa = rxResource({ stream: () => this.api.empresa() });
  protected readonly servicos = rxResource({ stream: () => this.api.listarServicos() });
  protected readonly prestadores = rxResource({ stream: () => this.api.listarPrestadores() });
  protected readonly clientes = rxResource({
    params: () => (this.publica ? undefined : true), // o cliente não vê a lista de clientes
    stream: () => this.api.listarClientes(),
  });

  private readonly erroBase = computed(
    () => this.servicos.error() ?? this.prestadores.error() ?? (this.publica ? null : this.clientes.error()) ?? null,
  );
  protected readonly semConexao = computed(() => this.erroBase() instanceof ErroRede);
  protected readonly mensagemErroBase = computed(() => (this.erroBase() ? mensagemDeErro(this.erroBase()) : ""));
  protected readonly carregandoBase = computed(
    () => !this.servicos.hasValue() || !this.prestadores.hasValue() || (!this.publica && !this.clientes.hasValue()),
  );

  protected tentarDeNovo() {
    this.servicos.reload();
    this.prestadores.reload();
    if (!this.publica) this.clientes.reload();
  }

  /** Números dos passos: no painel o cliente vem primeiro; na página pública, os dados vêm no fim. */
  protected readonly n = this.publica
    ? { servico: 1, prestador: 2, dia: 3, horario: 4, dados: 5, cliente: 0 }
    : { cliente: 1, servico: 2, prestador: 3, dia: 4, horario: 5, dados: 0 };

  // ── Cliente (painel) ────────────────────────────────────────
  protected readonly busca = signal("");
  protected readonly clienteId = signal("");

  private readonly listaClientes = computed(() =>
    (this.clientes.hasValue() ? (this.clientes.value() ?? []) : []).map((c) => ({
      ...c,
      iniciais: iniciais(c.nome),
      meta: c.telefone ? formatarTelefone(c.telefone) : "Sem WhatsApp",
    })),
  );

  protected readonly semClientes = computed(() => !this.publica && this.clientes.hasValue() && this.listaClientes().length === 0);

  protected readonly clientesFiltrados = computed(() => {
    const termo = semAcento(this.busca().trim());
    const digitos = soDigitos(termo);
    return this.listaClientes().filter(
      (c) => !termo || semAcento(c.nome).includes(termo) || (!!digitos && soDigitos(c.telefone ?? "").includes(digitos)),
    );
  });

  protected readonly cliente = computed(() => this.listaClientes().find((c) => c.id === this.clienteId()) ?? null);

  // ── Serviço e profissional ──────────────────────────────────
  protected readonly servicoId = signal("");
  protected readonly servico = computed(
    () => (this.servicos.hasValue() ? this.servicos.value() : []).find((s) => s.id === this.servicoId()) ?? null,
  );

  protected readonly listaServicos = computed(() =>
    (this.servicos.hasValue() ? this.servicos.value() : []).map((s) => ({
      ...s,
      duracao: duracaoEmTexto(s.duracao_min),
      preco: dinheiro(s.preco_centavos).replace(",00", ""),
    })),
  );

  protected readonly prestadorIdEscolhido = signal("");
  protected readonly listaPrestadores = computed(() =>
    (this.prestadores.hasValue() ? this.prestadores.value() : []).map((p) => ({ ...p, iniciais: iniciais(p.nome) })),
  );
  /** Com um só profissional, ele já vem escolhido. */
  protected readonly prestador = computed(() => {
    const lista = this.listaPrestadores();
    if (lista.length === 1) return lista[0];
    return lista.find((p) => p.id === this.prestadorIdEscolhido()) ?? null;
  });

  // ── Dia ─────────────────────────────────────────────────────
  protected readonly inicioDaFila = signal(this.hoje);
  protected readonly dia = signal(this.hoje);

  protected readonly fila = computed(() =>
    Array.from({ length: DIAS_NA_FILA }, (_, i) => {
      const dia = somarDias(this.inicioDaFila(), i);
      return { dia, curto: diaCurto(dia), numero: numeroDoDia(dia), hoje: dia === this.hoje, rotulo: dataLonga(dia) };
    }),
  );

  protected readonly mesDaFila = computed(() => {
    const primeiro = nomeDoMes(this.fila()[0].dia);
    const ultimo = nomeDoMes(this.fila()[DIAS_NA_FILA - 1].dia);
    return primeiro === ultimo ? primeiro : `${primeiro} e ${ultimo.toLowerCase()}`;
  });

  // ── Horário ─────────────────────────────────────────────────
  protected readonly hora = signal("");

  // Ocupados do dia. Quando os profissionais vierem da API, filtrar por prestador_id aqui.
  protected readonly doDia = rxResource({
    params: () => ({ dia: this.dia() }),
    stream: ({ params }) =>
      this.api.listarAgendamentos(inicioDoDia(params.dia), inicioDoDia(somarDias(params.dia, 1))),
  });
  protected readonly erroDoDia = computed(() => (this.doDia.error() ? mensagemDeErro(this.doDia.error()) : ""));

  /** Horários do dia para a duração do serviço; os que já passaram nem aparecem. */
  protected readonly horarios = computed<Horario[]>(() => {
    const s = this.servico();
    if (!s || !this.doDia.hasValue()) return [];
    const duracao = s.duracao_min;
    const ocupados = this.doDia
      .value()
      .filter((a) => a.status !== "cancelado")
      .map((a) => [Date.parse(a.inicio), Date.parse(a.fim)] as const);
    const agora = Date.now();
    const lista: Horario[] = [];
    for (let m = PRIMEIRO_MINUTO; m + duracao <= ULTIMO_MINUTO; m += PASSO) {
      const hora = `${String(Math.floor(m / 60)).padStart(2, "0")}:${String(m % 60).padStart(2, "0")}`;
      const inicio = new Date(`${this.dia()}T${hora}`);
      const fim = new Date(inicio.getTime() + duracao * 60_000);
      if (inicio.getTime() <= agora) continue;
      const ocupado = ocupados.some(([i, f]) => inicio.getTime() < f && fim.getTime() > i);
      lista.push({ hora, inicio, fim, ocupado });
    }
    return lista;
  });

  protected readonly manha = computed(() => this.horarios().filter((h) => h.hora < "12:00"));
  protected readonly tarde = computed(() => this.horarios().filter((h) => h.hora >= "12:00"));
  protected readonly livres = computed(() => this.horarios().filter((h) => !h.ocupado));

  /** Horário escolhido, se ainda estiver livre (trocar o serviço pode ocupá-lo). */
  protected readonly escolhido = computed(() => this.livres().find((h) => h.hora === this.hora()) ?? null);

  protected readonly legendaHorarios = computed(() => {
    const s = this.servico();
    const p = this.prestador();
    if (!s || !p) return "";
    const quando =
      this.dia() === this.hoje ? "hoje" : `${/^(sábado|domingo)/.test(diaLongo(this.dia())) ? "no" : "na"} ${diaLongo(this.dia())}`;
    return `Livres ${quando} com ${p.nome.split(" ")[0]}, para ${duracaoEmTexto(s.duracao_min)}`;
  });

  // ── Seus dados (página pública) ─────────────────────────────
  protected readonly nome = signal("");
  protected readonly whatsapp = signal("");
  protected readonly nomeTocado = signal(false);
  protected readonly whatsappTocado = signal(false);
  protected readonly nomeValido = computed(() => this.nome().trim().length >= 2);
  protected readonly whatsappOk = computed(() => whatsappValido(this.whatsapp()));

  // ── Resumo e envio ──────────────────────────────────────────
  protected readonly pronto = computed(
    () =>
      !!this.servico() &&
      !!this.prestador() &&
      !!this.escolhido() &&
      (this.publica ? this.nomeValido() && this.whatsappOk() : !!this.cliente()),
  );

  /** "Agendar quarta, 10h" (hoje, amanhã, dia da semana ou a data). */
  protected readonly rotuloBotao = computed(() => {
    const h = this.escolhido();
    if (!h || !this.servico() || !this.prestador()) return "Agendar";
    return `Agendar ${this.quando(this.dia())}, ${horaEmFrase(h.inicio)}`;
  });

  protected readonly enviando = signal(false);
  protected readonly erro = signal("");
  /** Horário que a API recusou com 409; as alternativas saem da lista recarregada. */
  protected readonly horaRecusada = signal("");
  protected readonly alternativas = computed(() => {
    const recusada = this.horaRecusada();
    if (!recusada) return [];
    const depois = this.livres().filter((h) => h.hora > recusada);
    return (depois.length ? depois : this.livres()).slice(0, 2);
  });

  /** Página pública: depois de agendar, a confirmação toma o lugar do formulário. */
  protected readonly agendado = signal<{ frase: string; detalhe: string } | null>(null);

  protected readonly dataLonga = dataLonga;
  protected readonly formatarHora = formatarHora;
  protected readonly formatarTelefone = formatarTelefone;
  protected readonly dinheiro = dinheiro;
  protected readonly duracaoEmTexto = duracaoEmTexto;
  protected readonly iniciais = iniciais;

  protected escolherCliente(id: string) {
    this.clienteId.set(id);
    this.busca.set("");
  }

  protected escolherServico(id: string) {
    this.servicoId.set(id);
    this.horaRecusada.set("");
  }

  protected escolherPrestador(id: string) {
    this.prestadorIdEscolhido.set(id);
    this.hora.set("");
    this.horaRecusada.set("");
  }

  protected andarFila(direcao: -1 | 1) {
    const novo = somarDias(this.inicioDaFila(), direcao * DIAS_NA_FILA);
    this.inicioDaFila.set(novo < this.hoje ? this.hoje : novo);
  }

  protected escolherDia(dia: string) {
    if (dia === this.dia()) return;
    this.dia.set(dia);
    this.hora.set("");
    this.horaRecusada.set("");
  }

  protected escolherHora(hora: string) {
    this.hora.set(hora);
    this.horaRecusada.set("");
    this.erro.set("");
  }

  protected async agendar(e: SubmitEvent) {
    e.preventDefault();
    this.nomeTocado.set(true);
    this.whatsappTocado.set(true);
    const s = this.servico();
    const p = this.prestador();
    const h = this.escolhido();
    if (!this.pronto() || !s || !p || !h || this.enviando()) return;
    this.erro.set("");
    this.horaRecusada.set("");
    this.enviando.set(true); // aria-busy no botão e sem segundo POST
    try {
      // Na página pública o cliente se identifica pelo nome e WhatsApp; no painel ele já existe.
      const clienteId = this.publica
        ? (await firstValueFrom(this.api.criarCliente({ nome: this.nome().trim(), telefone: soDigitos(this.whatsapp()) }))).id
        : this.cliente()!.id;
      await firstValueFrom(
        this.api.criarAgendamento({ cliente_id: clienteId, inicio: h.inicio.toISOString(), fim: h.fim.toISOString() }),
      );
      if (this.publica) {
        this.agendado.set({
          frase: `Combinado. Até ${this.quando(this.dia())}, às ${horaEmFrase(h.inicio)}.`,
          detalhe: `${s.nome} com ${p.nome} · ${dataLonga(this.dia())}, ${h.hora} às ${formatarHora(h.fim)} · ${dinheiro(s.preco_centavos)}, pago no local`,
        });
        this.enviando.set(false);
      } else {
        await this.router.navigateByUrl("/agendamentos");
      }
    } catch (err) {
      if (err instanceof ApiError && err.codigo === "conflito_horario") {
        this.horaRecusada.set(h.hora);
        this.hora.set("");
        this.doDia.reload(); // o horário ocupado aparece riscado e as alternativas se atualizam
      } else {
        this.erro.set(mensagemDeErro(err));
      }
      this.enviando.set(false);
    }
  }

  /** Marcar outro horário depois de confirmar (página pública): mantém nome e WhatsApp. */
  protected marcarOutro() {
    this.agendado.set(null);
    this.hora.set("");
    this.doDia.reload();
  }

  private quando(dia: string): string {
    if (dia === this.hoje) return "hoje";
    if (dia === somarDias(this.hoje, 1)) return "amanhã";
    if (dia < somarDias(this.hoje, 7)) return diaLongo(dia);
    return diaEMes(dia);
  }
}
