import { ChangeDetectionStrategy, Component, computed, inject, signal } from "@angular/core";
import { rxResource } from "@angular/core/rxjs-interop";
import { Router } from "@angular/router";
import { firstValueFrom } from "rxjs";
import { Api, ApiError, ErroRede, mensagemDeErro } from "../api";
import {
  dataLonga,
  diaCurto,
  diaEMes,
  diaLocal,
  diaLongo,
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

// Padrão C do sistema de design: passos numerados + resumo ao lado. Sem serviço nem "com quem"
// por enquanto: a API ainda usa o serviço e o prestador fixos da Fatia 1, então a duração vem
// de um campo. Horários de 30 em 30 min entre 08:00 e 19:00 (a mesma faixa da grade da agenda),
// até a disponibilidade existir; o 409 da API continua sendo a palavra final.

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

/** "(11) 98877-4410" para números do Brasil com DDI; senão, como veio. */
export function formatarTelefone(t: string): string {
  const d = t.replace(/\D/g, "").replace(/^55(?=\d{10,11}$)/, "");
  if (d.length === 11) return `(${d.slice(0, 2)}) ${d.slice(2, 7)}-${d.slice(7)}`;
  if (d.length === 10) return `(${d.slice(0, 2)}) ${d.slice(2, 6)}-${d.slice(6)}`;
  return t;
}

@Component({
  selector: "app-novo-agendamento",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MensagemErro, Vazio],
  templateUrl: "./novo-agendamento.html",
})
export class NovoAgendamento {
  private readonly api = inject(Api);
  private readonly router = inject(Router);

  protected readonly hoje = diaLocal(new Date());

  // ── 1. Cliente ──────────────────────────────────────────────
  protected readonly clientes = rxResource({ stream: () => this.api.listarClientes() });
  protected readonly erroClientes = computed(() => this.clientes.error() ?? null);
  protected readonly clientesSemConexao = computed(() => this.erroClientes() instanceof ErroRede);
  protected readonly mensagemErroClientes = computed(() =>
    this.erroClientes() ? mensagemDeErro(this.erroClientes()) : "",
  );

  protected readonly busca = signal("");
  protected readonly clienteId = signal("");

  protected readonly clientesFiltrados = computed(() => {
    const lista = this.clientes.hasValue() ? this.clientes.value() : [];
    const termo = semAcento(this.busca().trim());
    return lista
      .filter((c) => !termo || semAcento(c.nome).includes(termo) || (c.telefone ?? "").includes(termo))
      .map((c) => ({ ...c, iniciais: iniciais(c.nome), meta: c.telefone ? formatarTelefone(c.telefone) : "Sem WhatsApp" }));
  });

  protected readonly cliente = computed(() => {
    const lista = this.clientes.hasValue() ? this.clientes.value() : [];
    const c = lista.find((x) => x.id === this.clienteId());
    return c
      ? { ...c, iniciais: iniciais(c.nome), meta: c.telefone ? formatarTelefone(c.telefone) : "Sem WhatsApp" }
      : null;
  });

  // ── 2. Duração ──────────────────────────────────────────────
  protected readonly duracaoTexto = signal("30");
  protected readonly duracaoTocada = signal(false);
  protected readonly duracao = computed(() => {
    const n = Number(this.duracaoTexto());
    return Number.isInteger(n) && n >= 5 && n <= 1440 ? n : null;
  });

  // ── 3. Dia ──────────────────────────────────────────────────
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

  // ── 4. Horário ──────────────────────────────────────────────
  protected readonly hora = signal("");

  protected readonly doDia = rxResource({
    params: () => ({ dia: this.dia() }),
    stream: ({ params }) =>
      this.api.listarAgendamentos(inicioDoDia(params.dia), inicioDoDia(somarDias(params.dia, 1))),
  });
  protected readonly erroDoDia = computed(() => (this.doDia.error() ? mensagemDeErro(this.doDia.error()) : ""));

  /** Horários do dia para a duração escolhida; os que já passaram nem aparecem. */
  protected readonly horarios = computed<Horario[]>(() => {
    const duracao = this.duracao();
    if (!duracao || !this.doDia.hasValue()) return [];
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

  /** Horário escolhido, se ainda estiver livre (trocar a duração pode ocupá-lo). */
  protected readonly escolhido = computed(() => this.livres().find((h) => h.hora === this.hora()) ?? null);

  protected readonly legendaHorarios = computed(() => {
    const d = this.duracao();
    const quando = this.dia() === this.hoje ? "hoje" : `na ${diaLongo(this.dia())}`.replace(/^na (sábado|domingo)/, "no $1");
    return d ? `Livres ${quando}, para ${duracaoEmTexto(d)}` : "";
  });

  // ── Resumo e envio ──────────────────────────────────────────
  protected readonly pronto = computed(() => !!this.cliente() && !!this.escolhido());

  /** "Agendar quarta, 10h" (hoje, amanhã, dia da semana ou a data). */
  protected readonly rotuloBotao = computed(() => {
    const h = this.escolhido();
    if (!h || !this.cliente()) return "Agendar";
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

  protected readonly dataLonga = dataLonga;
  protected readonly formatarHora = formatarHora;
  protected readonly duracaoEmTexto = duracaoEmTexto;

  protected escolherCliente(id: string) {
    this.clienteId.set(id);
    this.busca.set("");
  }

  protected trocarCliente() {
    this.clienteId.set("");
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
    const c = this.cliente();
    const h = this.escolhido();
    if (!c || !h || this.enviando()) return;
    this.erro.set("");
    this.horaRecusada.set("");
    this.enviando.set(true); // aria-busy no botão e sem segundo POST
    try {
      await firstValueFrom(
        this.api.criarAgendamento({ cliente_id: c.id, inicio: h.inicio.toISOString(), fim: h.fim.toISOString() }),
      );
      await this.router.navigateByUrl("/agendamentos");
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

  private quando(dia: string): string {
    if (dia === this.hoje) return "hoje";
    if (dia === somarDias(this.hoje, 1)) return "amanhã";
    if (dia < somarDias(this.hoje, 7)) return diaLongo(dia);
    return diaEMes(dia);
  }
}
