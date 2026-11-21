// Guarda das rotas do painel (#120, especificacao-fluxo §4): sem sessão, leva a /entrar.
import { inject } from "@angular/core";
import { type CanActivateFn, Router } from "@angular/router";
import { catchError, map, of } from "rxjs";
import { Api, ApiError } from "./api";

/**
 * GET /api/me decide: 401 → /entrar. Qualquer outro erro (sem conexão, erro interno) deixa
 * passar, para a tela do painel mostrar o próprio estado de erro em vez de uma tela em branco.
 */
export const sessaoGuard: CanActivateFn = () => {
  const router = inject(Router);
  return inject(Api)
    .eu()
    .pipe(
      map(() => true),
      catchError((e: unknown) => of(e instanceof ApiError && e.status === 401 ? router.parseUrl("/entrar") : true)),
    );
};
