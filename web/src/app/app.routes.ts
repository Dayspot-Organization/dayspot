import { Injectable, inject } from "@angular/core";
import { Title } from "@angular/platform-browser";
import { type RouterStateSnapshot, type Routes, TitleStrategy } from "@angular/router";
import { Agendamentos } from "./agendamentos/agendamentos";
import { Painel, trilhaDe } from "./layout";
import { NovoAgendamento } from "./novo-agendamento/novo-agendamento";

// data.titulo vira a trilha da top-bar (layout.ts): "Agenda › Novo agendamento".
export const routes: Routes = [
  // Página do cliente, aberta pelo link da empresa: fora da casca do painel, sem menu nem login.
  { path: "agendar", component: NovoAgendamento, data: { titulo: "Agendar", publica: true } },
  {
    path: "",
    component: Painel,
    children: [
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
    ],
  },
];

/** document.title = "<último da trilha> · Dayspot". */
@Injectable()
export class TituloDayspot extends TitleStrategy {
  private readonly title = inject(Title);

  override updateTitle(estado: RouterStateSnapshot) {
    const titulo = trilhaDe(estado.root).at(-1)?.titulo;
    this.title.setTitle(titulo ? `${titulo} · Dayspot` : "Dayspot");
  }
}
