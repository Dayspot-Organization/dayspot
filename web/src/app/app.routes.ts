import { Injectable, inject } from "@angular/core";
import { Title } from "@angular/platform-browser";
import { type RouterStateSnapshot, type Routes, TitleStrategy } from "@angular/router";
import { Agendamentos } from "./agendamentos/agendamentos";
import { Agendar } from "./agendar/agendar";
import { Cadastro } from "./cadastro/cadastro";
import { Entrar } from "./entrar/entrar";
import { Painel, trilhaDe } from "./layout";
import { NovoAgendamento } from "./novo-agendamento/novo-agendamento";

// data.titulo vira a trilha da top-bar (layout.ts): "Agenda › Novo agendamento".
export const routes: Routes = [
  // Página do cliente, aberta pelo link da empresa: fora da casca do painel, sem menu nem login.
  // Sem slug válido, a própria página avisa que a empresa não foi encontrada.
  {
    path: "agendar",
    data: { titulo: "Agendar" },
    children: [
      { path: "", component: Agendar },
      { path: ":slug", component: Agendar },
      { path: "**", component: Agendar },
    ],
  },
  // Entrar e Cadastro (#120): fora da casca do painel. Por enquanto só as telas; o guard do
  // painel e o 401 que leva a /entrar entram com os endpoints de sessão (#113, #119).
  { path: "entrar", component: Entrar, data: { titulo: "Entrar" } },
  { path: "cadastro", component: Cadastro, data: { titulo: "Cadastro" } },
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
