// Peças do layout (app.html): logo, menu e botão de tema.
import { ChangeDetectionStrategy, Component, input, output } from "@angular/core";
import { RouterLink, RouterLinkActive } from "@angular/router";
import type { Tema } from "./tema";

/** Símbolo (grade de horários com a pílula reservada) e nome. Sobre a nav: pontos brancos. */
@Component({
  selector: "app-logo",
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: "logo" },
  template: `
    <svg width="28" height="28" viewBox="0 0 120 120" aria-hidden="true">
      @for (p of pontos; track p) {
        <circle [attr.cx]="p[0]" [attr.cy]="p[1]" r="11" fill="#ffffff" />
      }
      <rect x="49" y="49" width="60" height="22" rx="11" fill="var(--marca)" />
    </svg>
    <span>smart<b>booking</b></span>
  `,
})
export class Logo {
  protected readonly pontos = [
    [22, 22],
    [60, 22],
    [98, 22],
    [22, 60],
    [22, 98],
    [60, 98],
    [98, 98],
  ];
}

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
