import { provideHttpClient, withFetch, withInterceptors } from "@angular/common/http";
import { type ApplicationConfig, provideBrowserGlobalErrorListeners } from "@angular/core";
import { provideRouter, TitleStrategy, withComponentInputBinding } from "@angular/router";
import { provideLucideConfig } from "@lucide/angular";
import { errosApiInterceptor } from "./api/http";
import { routes, TituloDayspot } from "./app.routes";

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    // Parâmetros da rota chegam como input() do componente (o :slug do /agendar).
    provideRouter(routes, withComponentInputBinding()),
    { provide: TitleStrategy, useClass: TituloDayspot },
    provideHttpClient(withFetch(), withInterceptors([errosApiInterceptor])),
    // Ícones Lucide com o traço da v3 (DESIGN.md › Ícones); o tamanho vem da classe .ds-ic.
    provideLucideConfig({ strokeWidth: 1.75 }),
  ],
};
