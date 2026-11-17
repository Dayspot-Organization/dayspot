import { ChangeDetectionStrategy, Component, computed, inject, signal } from "@angular/core";
import { rxResource } from "@angular/core/rxjs-interop";
import { Router } from "@angular/router";
import { firstValueFrom } from "rxjs";
import { Api, mensagemDeErro } from "../api";
import { diaLocal } from "../datas";
import { Carregando, MensagemErro, Vazio } from "../estados";

// Hora digitada, sem slots: os horários livres entram na Fatia 6.
// O título ("Agenda › Novo agendamento") fica na top-bar (app.html).
@Component({
  selector: "app-novo-agendamento",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Carregando, MensagemErro, Vazio],
  templateUrl: "./novo-agendamento.html",
})
export class NovoAgendamento {
  private readonly api = inject(Api);
  private readonly router = inject(Router);

  protected readonly clientes = rxResource({ stream: () => this.api.listarClientes() });
  protected readonly erroClientes = computed(() => {
    const e = this.clientes.error();
    return e ? mensagemDeErro(e) : "";
  });

  protected readonly clienteId = signal("");
  protected readonly data = signal(diaLocal(new Date()));
  protected readonly hora = signal("");
  protected readonly duracao = signal("30");
  protected readonly erro = signal("");
  protected readonly enviando = signal(false);

  // Sem FormsModule: o <form> fica com a validação nativa (required, min, max, step),
  // e o navegador barra o envio incompleto antes daqui.
  protected async salvar(e: SubmitEvent) {
    e.preventDefault();
    if (this.enviando()) return;
    this.erro.set("");
    this.enviando.set(true); // o botão desabilitado barra o segundo clique: sem POST duplicado
    // "AAAA-MM-DDTHH:MM" sem fuso é hora local; toISOString manda em UTC.
    const inicio = new Date(`${this.data()}T${this.hora()}`);
    const fim = new Date(inicio.getTime() + Number(this.duracao()) * 60_000);
    try {
      await firstValueFrom(
        this.api.criarAgendamento({ cliente_id: this.clienteId(), inicio: inicio.toISOString(), fim: fim.toISOString() }),
      );
      await this.router.navigateByUrl("/agendamentos");
    } catch (e) {
      this.erro.set(mensagemDeErro(e)); // 409 inclusive: o formulário continua preenchido
      this.enviando.set(false);
    }
  }
}
