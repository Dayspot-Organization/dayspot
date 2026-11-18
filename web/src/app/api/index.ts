// Funções por recurso. Contrato de JSON definido aqui (colunas das migrations
// 00004 e 00007); os handlers das cards 1.4, 1.5 e 1.6 seguem este formato.
import { HttpClient, HttpParams } from "@angular/common/http";
import { Injectable, inject } from "@angular/core";
import { ApiError, ErroRede } from "./http";

export { ApiError, ErroRede };

// Só os códigos em que um texto fixo diz mais que a mensagem da API; nos outros
// (requisicao_invalida, nao_encontrado, desconhecidos) a mensagem da API já diz o campo.
// Map, não objeto: um codigo como "constructor" não pode cair no protótipo.
const mensagens = new Map([
  ["conflito_horario", "Esse horário acabou de ser ocupado. Escolha outro."],
  ["erro_interno", "Algo deu errado do nosso lado. Tente de novo em instantes."],
]);

/** Texto em português para mostrar ao usuário, decidido pelo `codigo` (docs/erros-api.md). */
export function mensagemDeErro(e: unknown): string {
  if (e instanceof ErroRede) return "Não foi possível falar com o servidor. Tente de novo em instantes.";
  if (e instanceof ApiError) return mensagens.get(e.codigo) ?? e.mensagem;
  return "Erro inesperado. Tente de novo.";
}

/** Valores da coluna agendamentos.status (migration 00007). */
export type StatusAgendamento = "confirmado" | "cancelado" | "concluido";

/** Datas em ISO 8601 UTC com offset, ex.: "2026-10-09T13:00:00Z". */
export interface Agendamento {
  id: string;
  cliente_id: string;
  cliente_nome: string;
  servico_id: string;
  prestador_id: string;
  inicio: string;
  fim: string;
  status: StatusAgendamento;
  valor_centavos: number;
}

export type NovoAgendamento = Pick<Agendamento, "cliente_id" | "inicio" | "fim">;

export interface Cliente {
  id: string;
  nome: string;
  telefone: string | null;
}

/** Única porta de entrada para a API. Os erros chegam convertidos pelo errosApiInterceptor. */
@Injectable({ providedIn: "root" })
export class Api {
  private readonly http = inject(HttpClient);

  /** GET /api/agendamentos. Sem de/ate, a API usa o período padrão; sem prestador_id, todos do tenant. */
  listarAgendamentos(de?: string, ate?: string, prestador_id?: string) {
    let params = new HttpParams();
    if (de) params = params.set("de", de);
    if (ate) params = params.set("ate", ate);
    if (prestador_id) params = params.set("prestador_id", prestador_id);
    return this.http.get<Agendamento[]>("/api/agendamentos", { params });
  }

  /** POST /api/agendamentos. 409 conflito_horario quando sobrepõe outro. */
  criarAgendamento(novo: NovoAgendamento) {
    return this.http.post<Omit<Agendamento, "cliente_nome">>("/api/agendamentos", novo);
  }

  /** GET /api/clientes. */
  listarClientes() {
    return this.http.get<Cliente[]>("/api/clientes");
  }
}
