import { ChangeDetectionStrategy, Component, ElementRef, computed, inject, signal } from "@angular/core";
import { rxResource } from "@angular/core/rxjs-interop";
import { Router } from "@angular/router";
import { firstValueFrom } from "rxjs";
import { Api, ErroRede, mensagemDeErro } from "../api";
import { dataLonga, diaEmFrase, diaLocal, duracaoEmTexto, formatarHora, horaEmFrase, iniciais, juntar } from "../datas";
import { MensagemErro, Vazio } from "../estados";

// Padrão C do sistema de design (passos + resumo), no painel: cliente, data, hora e duração livres.
// Serviço e profissional não aparecem enquanto POST /api/agendamentos gravar os valores fixos (#163).

const soDigitos = (s: string) => s.replace(/\D/g, "");

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
  imports: [MensagemErro, Vazio],
  templateUrl: "./novo-agendamento.html",
})
export class NovoAgendamento {
  private readonly api = inject(Api);
  private readonly router = inject(Router);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

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

  protected readonly clientesFiltrados = computed(() => {
    const termo = semAcento(this.busca().trim());
    const digitos = soDigitos(termo);
    return this.listaClientes().filter(
      (c) => !termo || semAcento(c.nome).includes(termo) || (!!digitos && soDigitos(c.telefone ?? "").includes(digitos)),
    );
  });

  protected readonly cliente = computed(() => this.listaClientes().find((c) => c.id === this.clienteId()) ?? null);

  // ── Data, hora e duração ────────────────────────────────────
  protected readonly data = signal(this.hoje);
  protected readonly hora = signal("");
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

  protected async agendar(e: SubmitEvent) {
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
    try {
      await firstValueFrom(
        this.api.criarAgendamento({ cliente_id: c.id, inicio: inicio.toISOString(), fim: fim.toISOString() }),
      );
      await this.router.navigateByUrl("/agendamentos");
    } catch (err) {
      this.erro.set(mensagemDeErro(err)); // 409 inclusive: o formulário continua preenchido
      this.enviando.set(false);
    }
  }

  /** Leva o foco ao campo ou ao primeiro controle da seção. */
  private focar(id: string) {
    const alvo = this.host.nativeElement.querySelector<HTMLElement>(`#${id}`);
    const campo = alvo?.matches("input") ? alvo : alvo?.querySelector<HTMLElement>("input, button:not(:disabled)");
    (campo ?? alvo)?.focus();
  }
}
