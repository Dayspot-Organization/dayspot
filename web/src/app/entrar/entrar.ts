import { ChangeDetectionStrategy, Component, ElementRef, inject, signal } from "@angular/core";
import { RouterLink } from "@angular/router";
import { LucideCircleExclamationPoint, LucideEye, LucideEyeOff } from "@lucide/angular";
import { PaginaEntrada, erroDeEmail, valorDe } from "../entrada/entrada";

// Entrar (/entrar, #120, especificacao-fluxo §4.2). Só e-mail e senha, sem escolher empresa.
// Por enquanto só a tela: o envio confere os campos e para aí. POST /api/login entra com o #113;
// os avisos de credencial errada, muitas tentativas, sem conexão e sessão expirada vêm com ele.
@Component({
  selector: "app-entrar",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PaginaEntrada, RouterLink, LucideCircleExclamationPoint, LucideEye, LucideEyeOff],
  templateUrl: "./entrar.html",
})
export class Entrar {
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  readonly email = signal("");
  readonly senha = signal("");
  readonly senhaVisivel = signal(false);
  readonly erroEmail = signal("");
  readonly erroSenha = signal("");

  protected readonly valorDe = valorDe;

  digitarEmail(valor: string) {
    this.email.set(valor);
    this.erroEmail.set("");
  }

  digitarSenha(valor: string) {
    this.senha.set(valor);
    this.erroSenha.set("");
  }

  alternarSenha() {
    this.senhaVisivel.update((v) => !v);
  }

  enviar(evento: Event) {
    evento.preventDefault();
    const erroEmail = erroDeEmail(this.email());
    const erroSenha = this.senha() ? "" : "Escreva a senha.";
    this.erroEmail.set(erroEmail);
    this.erroSenha.set(erroSenha);
    if (erroEmail || erroSenha) {
      this.focar(erroEmail ? "e-email" : "e-senha");
    }
  }

  private focar(id: string) {
    this.host.nativeElement.querySelector<HTMLElement>(`#${id}`)?.focus();
  }
}
