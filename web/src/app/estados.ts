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

/** Vazio com a ilustração da marca (agenda-vazia.svg ou sem-clientes.svg): `agenda` ou `clientes`. */
@Component({
  selector: "app-vazio",
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="vazio">
      <svg viewBox="0 0 200 150" aria-hidden="true">
        @if (ilustracao() === "clientes") {
          <path class="ilu-tr" d="M92 30H46a12 12 0 0 0-12 12v66a12 12 0 0 0 12 12h66a12 12 0 0 0 12-12V62" />
          <circle class="ilu-ac" cx="79" cy="66" r="13" />
          <path class="ilu-tr" d="M54 106a25 25 0 0 1 50 0" />
          <path class="ilu-li" d="M138 60h32M138 80h24M138 100h28" />
          <circle class="ilu-a2" cx="120" cy="38" r="8" />
        } @else {
          <path class="ilu-tr" d="M114 22H62a14 14 0 0 0-14 14v86a14 14 0 0 0 14 14h76a14 14 0 0 0 14-14V62" />
          <path class="ilu-li" d="M66 56h48M66 80h68M66 104h68" />
          <rect class="ilu-pr" x="62" y="44" width="58" height="22" rx="11" />
          <circle class="ilu-ac" cx="146" cy="30" r="10" />
        }
      </svg>
      <p><ng-content /></p>
    </div>
  `,
})
export class Vazio {
  readonly ilustracao = input<"agenda" | "clientes">("agenda");
}
