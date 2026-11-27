import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, computed, inject, signal } from "@angular/core";
import { rxResource, takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { ActivatedRoute, Router } from "@angular/router";
import { LucideCheck, LucideCircleExclamationPoint, LucideLink, LucideUsers, LucideWifiOff } from "@lucide/angular";
import { Api, ErroRede, mensagemDeErro } from "../api";
import { dataLonga, diaEmFrase, diaLocal, duracaoEmTexto, formatarHora, horaEmFrase, iniciais, juntar } from "../datas";
import { MensagemErro } from "../estados";

// Padrão C do sistema de design (passos + resumo), no painel: cliente, data, hora e duração livres.
// Serviço e profissional não aparecem enquanto POST /api/agendamentos gravar os valores fixos (#163).

const soDigitos = (s: string) => s.replace(/\D/g, "");

/** O valor da URL só entra no formulário no formato do campo; senão, fica o padrão. */
const valido = (valor: string | null, formato: RegExp) => (valor && formato.test(valor) ? valor : null);

/** Dia que existe no calendário: "2026-02-30" vira 2 de março na ida e não volta igual. */
const diaValido = (valor: string | null) => {
  const dia = valido(valor, /^\d{4}-\d{2}-\d{2}$/);
  return dia && diaLocal(new Date(`${dia}T00:00`)) === dia ? dia : null;
};

const semAcento = (s: string) =>
  s
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .toLowerCase();

/** "(11) 98877-4410" para números do Brasil, com ou sem 55; senão, como veio. */
function formatarTelefone(t: string): string {
  const d = soDigitos(t).replace(/^55(?=\d{10,11}$)/, "");
  if (d.length === 11) return `(${d.slice(0, 2)}) ${d.slice(2, 7)}-${d.slice(7)}`;
  if (d.length === 10) return `(${d.slice(0, 2)}) ${d.slice(2, 6)}-${d.slice(6)}`;
  return t;
}

@Component({
  selector: "app-novo-agendamento",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MensagemErro, LucideCheck, LucideCircleExclamationPoint, LucideLink, LucideUsers, LucideWifiOff],
  templateUrl: "./novo-agendamento.html",
  host: { class: "conteudo" },
})
export class NovoAgendamento {
  private readonly api = inject(Api);
  private readonly router = inject(Router);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly destroyRef = inject(DestroyRef);
  /** ?dia=AAAA-MM-DD&hora=HH:MM: o vazio da Agenda que foi clicado. */
  private readonly pedido = inject(ActivatedRoute).snapshot.queryParamMap;

  protected readonly hoje = diaLocal(new Date());

  // ── Cliente ─────────────────────────────────────────────────
  protected readonly clientes = rxResource({ stream: () => this.api.listarClientes() });
  protected readonly semConexao = computed(() => this.clientes.error() instanceof ErroRede);
  protected readonly erroClientes = computed(() => (this.clientes.error() ? mensagemDeErro(this.clientes.error()) : ""));

  protected readonly busca = signal("");
  protected readonly clienteId = signal("");

  private readonly listaClientes = computed(() =>
    (this.clientes.hasValue() ? this.clientes.value() : []).map((c) => ({
      ...c,
      iniciais: iniciais(c.nome),
      meta: c.telefone ? formatarTelefone(c.telefone) : "Sem WhatsApp",
    })),
  );

  protected readonly semClientes = computed(() => this.clientes.hasValue() && this.listaClientes().length === 0);

  /** Link da página de agendamento, para o vazio sem clientes: a empresa só é buscada quando ele aparece. */
  private readonly eu = rxResource({ params: () => this.semClientes() || undefined, stream: () => this.api.eu() });
  protected readonly linkPublico = computed(() => {
    const slug = this.eu.hasValue() ? this.eu.value().empresa_ativa?.slug : undefined;
    return slug ? `${location.origin}/agendar/${slug}` : "";
  });
  protected readonly copia = signal<"" | "feita" | "falhou">("");
  private relogioCopia?: ReturnType<typeof setTimeout>;

  constructor() {
    this.destroyRef.onDestroy(() => clearTimeout(this.relogioCopia));
  }

  protected readonly clientesFiltrados = computed(() => {
    const termo = semAcento(this.busca().trim());
    const digitos = soDigitos(termo);
    return this.listaClientes().filter(
      (c) => !termo || semAcento(c.nome).includes(termo) || (!!digitos && soDigitos(c.telefone ?? "").includes(digitos)),
    );
  });

  protected readonly cliente = computed(() => this.listaClientes().find((c) => c.id === this.clienteId()) ?? null);

  // ── Data, hora e duração ────────────────────────────────────
  protected readonly data = signal(diaValido(this.pedido.get("dia")) ?? this.hoje);
  protected readonly hora = signal(valido(this.pedido.get("hora"), /^([01]\d|2[0-3]):[0-5]\d$/) ?? "");
  protected readonly duracao = signal("30");
  /** Depois da primeira tentativa de envio, os campos vazios aparecem como erro. */
  protected readonly tentou = signal(false);

  protected readonly duracaoValida = computed(() => {
    const d = Number(this.duracao());
    return Number.isInteger(d) && d >= 5 && d <= 1440 && d % 5 === 0;
  });

  // "AAAA-MM-DDTHH:MM" sem fuso é hora local; toISOString manda em UTC.
  private readonly inicio = computed(() => (this.data() && this.hora() ? new Date(`${this.data()}T${this.hora()}`) : null));
  protected readonly fim = computed(() => {
    const inicio = this.inicio();
    return inicio && this.duracaoValida() ? new Date(inicio.getTime() + Number(this.duracao()) * 60_000) : null;
  });

  // ── Resumo e envio ──────────────────────────────────────────
  /** O que falta, na ordem da tela; `alvo` é o id que recebe o foco. */
  private readonly pendencias = computed(() => {
    const lista: { alvo: string; texto: string }[] = [];
    if (!this.cliente()) lista.push({ alvo: "secao-cliente", texto: "o cliente" });
    if (!this.data()) lista.push({ alvo: "data", texto: "a data" });
    if (!this.hora()) lista.push({ alvo: "hora", texto: "a hora" });
    if (!this.duracaoValida()) lista.push({ alvo: "duracao", texto: "a duração" });
    return lista;
  });
  protected readonly falta = computed(() => {
    const textos = this.pendencias().map((p) => p.texto);
    return textos.length ? `Para agendar, falta ${juntar(textos)}.` : "";
  });

  /** "Agendar quarta, 10h" (hoje, amanhã, dia da semana ou a data). */
  protected readonly rotuloBotao = computed(() =>
    this.data() && this.hora() ? `Agendar ${diaEmFrase(this.data(), this.hoje)}, ${horaEmFrase(this.hora())}` : "Agendar",
  );

  protected readonly enviando = signal(false);
  protected readonly erro = signal("");

  protected readonly dataLonga = dataLonga;
  protected readonly duracaoEmTexto = duracaoEmTexto;
  protected readonly formatarHora = formatarHora;

  protected escolherCliente(id: string) {
    this.clienteId.set(id);
    this.busca.set("");
  }

  /** Sem permissão ou fora de https a cópia falha, e o link aparece para copiar à mão. */
  protected async copiarLink(link: string) {
    clearTimeout(this.relogioCopia);
    try {
      await navigator.clipboard.writeText(link);
      this.copia.set("feita");
      // "Link copiado" por 2 s e o botão volta a "Copiar link".
      this.relogioCopia = setTimeout(() => this.copia.set(""), 2000);
    } catch {
      this.copia.set("falhou");
    }
  }

  protected agendar(e: SubmitEvent) {
    e.preventDefault();
    if (this.enviando()) return;
    this.tentou.set(true);
    const [pendente] = this.pendencias();
    const c = this.cliente();
    const inicio = this.inicio();
    const fim = this.fim();
    if (pendente || !c || !inicio || !fim) {
      if (pendente) this.focar(pendente.alvo);
      return;
    }
    this.erro.set("");
    this.enviando.set(true); // aria-busy no botão e sem segundo POST
    // Saiu da tela no meio do envio: o takeUntilDestroyed cancela o POST e nada navega depois.
    this.api
      .criarAgendamento({ cliente_id: c.id, inicio: inicio.toISOString(), fim: fim.toISOString() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => void this.router.navigateByUrl("/agendamentos"),
        error: (err: unknown) => {
          this.erro.set(mensagemDeErro(err)); // 409 inclusive: o formulário continua preenchido
          this.enviando.set(false);
        },
      });
  }

  /** Leva o foco ao campo ou ao primeiro controle da seção. */
  private focar(id: string) {
    const alvo = this.host.nativeElement.querySelector<HTMLElement>(`#${id}`);
    const campo = alvo?.matches("input") ? alvo : alvo?.querySelector<HTMLElement>("input, button:not(:disabled)");
    (campo ?? alvo)?.focus();
  }
}
