// Peças do layout (app.html): logo, menu e botão de tema.
import { ChangeDetectionStrategy, Component, input, output } from "@angular/core";
import { RouterLink, RouterLinkActive } from "@angular/router";
import type { Tema } from "./tema";

/** Logo horizontal da marca (SVG, nunca redigitado); a versão escura entra no tema escuro. */
@Component({
  selector: "app-logo",
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: "logo" },
  template: `
    <img class="logo-claro" src="/logo-horizontal.svg" alt="Dayspot" width="125" height="26" />
    <img class="logo-escuro" src="/logo-horizontal-escuro.svg" alt="Dayspot" width="125" height="26" />
  `,
})
export class Logo {}

/** O menu só lista telas que existem. `navegar` avisa a gaveta para fechar. */
@Component({
  selector: "app-menu",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, RouterLinkActive],
  template: `
    <nav class="menu" aria-label="Menu principal">
      <a routerLink="/agendamentos" routerLinkActive="active" ariaCurrentWhenActive="page" (click)="navegar.emit()">
        <svg width="20" height="20" viewBox="0 0 24 24" aria-hidden="true">
          <g fill="none" stroke="currentColor" stroke-width="2.25" stroke-linecap="round" stroke-linejoin="round">
            <path d="M14.5 4.5H7.5a4 4 0 0 0-4 4v8a4 4 0 0 0 4 4h9a4 4 0 0 0 4-4v-5.5" />
            <path d="M8 11.5h4M8 15.5h7.5" />
          </g>
          <circle cx="19" cy="5.5" r="2.1" fill="var(--ds-acao)" />
        </svg>
        Agenda
      </a>
    </nav>
  `,
})
export class Menu {
  readonly navegar = output();
}

/** Botão redondo da top-bar: o ícone mostra o tema em uso; o clique troca. */
@Component({
  selector: "app-botao-tema",
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button type="button" class="botao-tema" [attr.aria-label]="rotulo()" [title]="rotulo()" (click)="trocar.emit()">
      @if (tema() === "escuro") {
        <svg width="18" height="18" viewBox="0 0 24 24" aria-hidden="true">
          <path d="M20 14.5A8 8 0 0 1 9.5 4a8 8 0 1 0 10.5 10.5z" fill="currentColor" />
        </svg>
      } @else {
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" aria-hidden="true">
          <circle cx="12" cy="12" r="4" fill="currentColor" />
          <path
            d="M12 2.5v2M12 19.5v2M2.5 12h2M19.5 12h2M5.3 5.3l1.4 1.4M17.3 17.3l1.4 1.4M5.3 18.7l1.4-1.4M17.3 6.7l1.4-1.4"
            stroke="currentColor"
            stroke-width="1.8"
            stroke-linecap="round"
          />
        </svg>
      }
    </button>
  `,
})
export class BotaoTema {
  readonly tema = input.required<Tema>();
  readonly trocar = output();

  protected rotulo() {
    return this.tema() === "escuro" ? "Tema escuro. Mudar para o claro" : "Tema claro. Mudar para o escuro";
  }
}
