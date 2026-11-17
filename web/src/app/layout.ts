// Peças do layout (app.html): logo, menu e botão de tema.
import { ChangeDetectionStrategy, Component, input, output } from "@angular/core";
import { RouterLink, RouterLinkActive } from "@angular/router";
import type { Tema } from "./tema";

/** Símbolo (o "d" com o ponto: um lugar no dia) e nome. Sobre a nav: traço claro, ponto pêssego. */
@Component({
  selector: "app-logo",
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: "logo" },
  template: `
    <svg width="28" height="28" viewBox="20 16 64 64" aria-hidden="true">
      <path
        d="M32 29V70H50A20 20 0 0 0 70 50V44"
        fill="none"
        stroke="#fbf7f5"
        stroke-width="13"
        stroke-linecap="round"
        stroke-linejoin="round"
      />
      <circle cx="52.5" cy="29" r="7.8" fill="var(--ponto)" />
    </svg>
    <span>dayspot</span>
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
        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" aria-hidden="true">
          <rect x="3.5" y="5" width="17" height="15" rx="2.5" stroke="currentColor" stroke-width="1.8" />
          <path d="M3.5 10h17M8 3v4M16 3v4" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" />
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
