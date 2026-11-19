// Estados de tela comuns: carregando, erro e vazio. Nenhuma tela fica em branco.
// Desenho: docs/design-system/README.md §4.11 (aviso), §4.19 (vazio) e §4.20 (esqueleto).
import { ChangeDetectionStrategy, Component, booleanAttribute, input, output } from "@angular/core";

/**
 * Aviso de erro no fluxo da página: ícone + o que aconteceu e o que fazer.
 * "Tentar de novo" quando há o que repetir (ex.: a busca que falhou): ligue `podeRepetir`.
 */
@Component({
  selector: "app-mensagem-erro",
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="aviso aviso-erro" role="alert">
      <svg width="18" height="18" viewBox="0 0 24 24" aria-hidden="true">
        <path d="M12 6v8M12 18h.01" fill="none" stroke="currentColor" stroke-width="2.6" stroke-linecap="round" />
      </svg>
      <span class="aviso-texto">{{ mensagem() }}</span>
      @if (podeRepetir()) {
        <button type="button" class="botao botao-secundario botao-pequeno" (click)="tentarDeNovo.emit()">
          Tentar de novo
        </button>
      }
    </div>
  `,
})
export class MensagemErro {
  readonly mensagem = input.required<string>();
  readonly podeRepetir = input(false, { transform: booleanAttribute });
  readonly tentarDeNovo = output();
}

/**
 * Estado vazio dentro de um cartão: ilustração da marca (176 × 132), título que diz o fato e,
 * no conteúdo projetado, a frase curta e no máximo uma ação.
 */
@Component({
  selector: "app-vazio",
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="vazio">
      <svg width="176" height="132" viewBox="0 0 200 150" aria-hidden="true">
        @switch (ilustracao()) {
          @case ("clientes") {
            <rect class="ilu-tr" x="40" y="30" width="80" height="90" rx="14" />
            <circle class="ilu-ac" cx="80" cy="62" r="11" />
            <path class="ilu-tr" d="M58 104a22 22 0 0 1 44 0" />
            <path class="ilu-li" d="M134 62h30M134 80h22M134 98h30" />
          }
          @case ("conexao") {
            <path class="ilu-tr" d="M118 26H66a14 14 0 0 0-14 14v70a14 14 0 0 0 14 14h68a14 14 0 0 0 14-14V60" />
            <path class="ilu-li" d="M72 76h16M98 76h16M72 98h48" />
            <path class="ilu-tracejado" d="M126 26a22 22 0 0 1 22 22" />
            <circle class="ilu-ac" cx="150" cy="46" r="9" />
          }
          @case ("horarios") {
            <rect class="ilu-tr" x="34" y="34" width="132" height="86" rx="16" />
            <rect class="ilu-pr" x="50" y="52" width="44" height="18" rx="9" />
            <rect class="ilu-pr" x="104" y="52" width="44" height="18" rx="9" />
            <rect class="ilu-pr" x="50" y="82" width="44" height="18" rx="9" />
            <rect class="ilu-pr" x="104" y="82" width="44" height="18" rx="9" />
            <circle class="ilu-ac" cx="164" cy="34" r="10" />
          }
          @default {
            <path class="ilu-tr" d="M114 22H62a14 14 0 0 0-14 14v86a14 14 0 0 0 14 14h76a14 14 0 0 0 14-14V62" />
            <path class="ilu-li" d="M66 56h48M66 80h68M66 104h68" />
            <rect class="ilu-pr" x="62" y="44" width="58" height="22" rx="11" />
            <circle class="ilu-ac" cx="146" cy="30" r="10" />
          }
        }
      </svg>
      @if (titulo()) {
        <h2 class="titulo-cartao">{{ titulo() }}</h2>
      }
      <ng-content />
    </div>
  `,
})
export class Vazio {
  readonly ilustracao = input<"agenda" | "clientes" | "conexao" | "horarios">("agenda");
  readonly titulo = input("");
}
