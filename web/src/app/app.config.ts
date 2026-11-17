import { provideHttpClient, withFetch, withInterceptors } from "@angular/common/http";
import { type ApplicationConfig, provideBrowserGlobalErrorListeners } from "@angular/core";
import { provideRouter, TitleStrategy } from "@angular/router";
import { errosApiInterceptor } from "./api/http";
import { routes, TituloDayspot } from "./app.routes";

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    { provide: TitleStrategy, useClass: TituloDayspot },
    provideHttpClient(withFetch(), withInterceptors([errosApiInterceptor])),
  ],
};
