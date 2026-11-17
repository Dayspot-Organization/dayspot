import { Injectable, inject } from "@angular/core";
import { Title } from "@angular/platform-browser";
import { type ActivatedRouteSnapshot, type RouterStateSnapshot, type Routes, TitleStrategy } from "@angular/router";
import { Agendamentos } from "./agendamentos/agendamentos";
import { NovoAgendamento } from "./novo-agendamento/novo-agendamento";

// data.titulo vira a trilha da top-bar (app.ts): "Agenda › Novo agendamento".
export const routes: Routes = [
  { path: "", pathMatch: "full", redirectTo: "agendamentos" },
  {
    path: "agendamentos",
    data: { titulo: "Agenda" },
    children: [
      { path: "", component: Agendamentos },
      { path: "novo", component: NovoAgendamento, data: { titulo: "Novo agendamento" } },
    ],
  },
  { path: "**", redirectTo: "agendamentos" },
];

/** Um passo da trilha: título declarado na própria rota e o caminho até ela. */
export interface Passo {
  titulo: string;
  caminho: string;
}

/**
 * Rotas com data.titulo, da raiz até a ativa. Lê o data da configuração de cada rota
 * (não o herdado), para "Agenda" não se repetir na rota-filha "".
 */
export function trilhaDe(raiz: ActivatedRouteSnapshot): Passo[] {
  const trilha: Passo[] = [];
  const segmentos: string[] = [];
  for (let r: ActivatedRouteSnapshot | null = raiz; r; r = r.firstChild) {
    segmentos.push(...r.url.map((s) => s.path));
    const titulo = r.routeConfig?.data?.["titulo"];
    if (typeof titulo === "string") trilha.push({ titulo, caminho: "/" + segmentos.join("/") });
  }
  return trilha;
}

/** document.title = "<último da trilha> · Dayspot". */
@Injectable()
export class TituloDayspot extends TitleStrategy {
  private readonly title = inject(Title);

  override updateTitle(estado: RouterStateSnapshot) {
    const titulo = trilhaDe(estado.root).at(-1)?.titulo;
    this.title.setTitle(titulo ? `${titulo} · Dayspot` : "Dayspot");
  }
}
