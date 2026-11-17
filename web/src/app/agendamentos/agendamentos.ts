import { ChangeDetectionStrategy, Component, computed, inject, signal } from "@angular/core";
import { rxResource } from "@angular/core/rxjs-interop";
import { RouterLink } from "@angular/router";
import { Api, mensagemDeErro } from "../api";
import { diaLocal, formatarData, formatarHora, inicioDoDia, minutosEntre, somarDias } from "../datas";
import { Carregando, MensagemErro, Vazio } from "../estados";

@Component({
  selector: "app-agendamentos",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, Carregando, MensagemErro, Vazio],
  templateUrl: "./agendamentos.html",
})
export class Agendamentos {
  private readonly api = inject(Api);

  // Mesmo padrão da API (7 dias a partir de hoje), mas com "hoje" no fuso local.
  protected readonly de = signal(diaLocal(new Date()));
  protected readonly ate = signal(somarDias(diaLocal(new Date()), 6));

  protected readonly invalido = computed(() => {
    const de = this.de();
    const ate = this.ate();
    if (!de || !ate) return "Escolha as duas datas.";
    if (ate < de) return "A data final é anterior à inicial.";
    return "";
  });

  // Período inválido: sem params, o resource fica parado (não busca).
  // Troca de período cancela a busca anterior: resposta velha não sobrescreve a nova.
  protected readonly agendamentos = rxResource({
    params: () => (this.invalido() ? undefined : { de: this.de(), ate: this.ate() }),
    // "Até" inclui o dia inteiro: a API usa [de, ate), então vai a meia-noite seguinte.
    stream: ({ params }) => this.api.listarAgendamentos(inicioDoDia(params.de), inicioDoDia(somarDias(params.ate, 1))),
  });

  protected readonly erro = computed(() => {
    const e = this.agendamentos.error();
    return e ? mensagemDeErro(e) : "";
  });

  protected readonly formatarData = formatarData;
  protected readonly formatarHora = formatarHora;
  protected readonly minutosEntre = minutosEntre;
}
