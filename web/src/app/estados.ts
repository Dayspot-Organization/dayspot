// Estados de tela comuns: carregando, erro e vazio. Nenhuma tela fica em branco.
import { ChangeDetectionStrategy, Component, booleanAttribute, input, output } from "@angular/core";

@Component({
  selector: "app-carregando",
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<p role="status">Carregando…</p>`,
})
export class Carregando {}

/** Erro com "Tentar de novo" quando há o que repetir (ex.: a busca que falhou): ligue `podeRepetir`. */
@Component({
  selector: "app-mensagem-erro",
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="erro" role="alert">
      <span>{{ mensagem() }}</span>
      @if (podeRepetir()) {
        <button type="button" class="botao-texto" (click)="tentarDeNovo.emit()">Tentar de novo</button>
      }
    </div>
  `,
})
export class MensagemErro {
  readonly mensagem = input.required<string>();
  readonly podeRepetir = input(false, { transform: booleanAttribute });
  readonly tentarDeNovo = output();
}

@Component({
  selector: "app-vazio",
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<p class="secundario"><ng-content /></p>`,
})
export class Vazio {}
