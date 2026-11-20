// Tratamento centralizado das respostas de erro da API. Formato: docs/erros-api.md.
import { HttpErrorResponse, type HttpInterceptorFn } from "@angular/common/http";
import { catchError, throwError } from "rxjs";

/** Resposta não-2xx da API. Decida pelo `codigo`; `mensagem` pode ser exibida. */
export class ApiError extends Error {
  status: number;
  codigo: string;
  mensagem: string;
  /** `detalhes.campo` do 400 requisicao_invalida. */
  campo?: string;
  /** Cabeçalho `Retry-After` do 429 muitas_tentativas, em segundos. */
  esperaSegundos?: number;

  constructor(status: number, codigo: string, mensagem: string, campo?: string, esperaSegundos?: number) {
    super(mensagem);
    this.name = "ApiError";
    this.status = status;
    this.codigo = codigo;
    this.mensagem = mensagem;
    this.campo = campo;
    this.esperaSegundos = esperaSegundos;
  }
}

/** A requisição nem chegou a ter resposta (API fora do ar, sem conexão). */
export class ErroRede extends Error {
  constructor(causa: unknown) {
    super("Não foi possível conectar ao servidor.", { cause: causa });
    this.name = "ErroRede";
  }
}

/** Converte o HttpErrorResponse do Angular em ApiError ou ErroRede. */
export function converterErro(resp: HttpErrorResponse): Error {
  // status 0: o navegador não recebeu resposta nenhuma.
  if (resp.status === 0) return new ErroRede(resp.error);
  // 2xx com corpo que não é JSON: o HttpClient acusa erro de parse. Não é erro da API.
  if (resp.status >= 200 && resp.status < 300) return new Error("Resposta inválida da API.", { cause: resp });

  const erro = (resp.error as { erro?: { codigo?: unknown; mensagem?: unknown; detalhes?: { campo?: unknown } } } | null)?.erro;
  if (typeof erro?.codigo === "string" && typeof erro?.mensagem === "string") {
    const campo = erro.detalhes?.campo;
    const espera = Number(resp.headers.get("Retry-After"));
    return new ApiError(
      resp.status,
      erro.codigo,
      erro.mensagem,
      typeof campo === "string" ? campo : undefined,
      espera > 0 ? espera : undefined,
    );
  }
  // Corpo fora do formato: 502/503/504 é o proxy (ng serve em dev) sem alcançar a API;
  // o resto (HTML de erro etc.) vira erro_interno.
  if ([502, 503, 504].includes(resp.status)) return new ErroRede(resp.status);
  return new ApiError(resp.status, "erro_interno", "Erro interno. Tente novamente mais tarde.");
}

/** Interceptor registrado em app.config.ts: toda chamada à API sai com erro já tipado. */
export const errosApiInterceptor: HttpInterceptorFn = (req, next) =>
  next(req).pipe(catchError((e: unknown) => throwError(() => (e instanceof HttpErrorResponse ? converterErro(e) : e))));
