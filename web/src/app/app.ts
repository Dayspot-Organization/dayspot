import { ChangeDetectionStrategy, Component, type ElementRef, inject, viewChild } from "@angular/core";
import { toSignal } from "@angular/core/rxjs-interop";
import { NavigationEnd, Router, RouterLink, RouterOutlet } from "@angular/router";
import { filter, map } from "rxjs";
import { trilhaDe } from "./app.routes";
import { BotaoTema, Logo, Menu } from "./layout";
import { TemaService } from "./tema";

// Estrutura do DESIGN.md: celular com top-bar e gaveta (<dialog>), desktop com menu
// lateral fixo (o CSS troca a partir de 1024 px).
@Component({
  selector: "app-root",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, RouterLink, Logo, Menu, BotaoTema],
  templateUrl: "./app.html",
})
export class App {
  private readonly router = inject(Router);
  private readonly gaveta = viewChild.required<ElementRef<HTMLDialogElement>>("gaveta");
  protected readonly tema = inject(TemaService);

  /** Trilha da top-bar, recalculada a cada navegação concluída (o título da aba fica no TituloDayspot). */
  protected readonly trilha = toSignal(
    this.router.events.pipe(
      filter((e) => e instanceof NavigationEnd),
      map(() => trilhaDe(this.router.routerState.snapshot.root)),
    ),
    { initialValue: [] },
  );

  /** Rotas com data.publica (página do cliente) aparecem sem a casca do painel. */
  protected readonly publica = toSignal(
    this.router.events.pipe(
      filter((e) => e instanceof NavigationEnd),
      map(() => {
        let r = this.router.routerState.snapshot.root;
        while (r.firstChild) r = r.firstChild;
        return r.data["publica"] === true;
      }),
    ),
    { initialValue: false },
  );

  protected abrir() {
    this.gaveta().nativeElement.showModal();
  }

  protected fechar() {
    this.gaveta().nativeElement.close();
  }

  /** Clique no fundo escurecido (fora do conteúdo) fecha a gaveta. */
  protected cliqueNaGaveta(e: MouseEvent) {
    if (e.target === e.currentTarget) this.fechar();
  }
}
