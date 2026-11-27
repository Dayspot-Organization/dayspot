// Aviso de erro das telas ainda no CSS v2.1. Na v3, o erro é o .ds-aviso e o vazio, a peça
// .ds-vazio (DESIGN.md › Estado vazio); o esqueleto ainda não está desenhado.
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
