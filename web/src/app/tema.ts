import { DestroyRef, Injectable, computed, inject, signal } from "@angular/core";

export type Tema = "claro" | "escuro";

// Sem escolha salva, vale o sistema. localStorage pode falhar (aba anônima): idem.
function lerTema(): Tema | null {
  try {
    const t = localStorage.getItem("tema");
    if (t === "claro" || t === "escuro") return t;
  } catch {
    /* segue o sistema */
  }
  return null;
}

/** Tema claro/escuro: escolha salva ou, sem ela, o do sistema (acompanhando a troca ao vivo). */
@Injectable({ providedIn: "root" })
export class TemaService {
  private readonly escolhido = signal(lerTema());
  private readonly sistemaEscuro = signal(false);

  readonly tema = computed<Tema>(() => this.escolhido() ?? (this.sistemaEscuro() ? "escuro" : "claro"));

  constructor() {
    const escuroNoSistema = window.matchMedia("(prefers-color-scheme: dark)");
    this.sistemaEscuro.set(escuroNoSistema.matches);
    const avisar = () => this.sistemaEscuro.set(escuroNoSistema.matches);
    escuroNoSistema.addEventListener("change", avisar);
    inject(DestroyRef).onDestroy(() => escuroNoSistema.removeEventListener("change", avisar));
  }

  trocar() {
    const novo: Tema = this.tema() === "escuro" ? "claro" : "escuro";
    this.escolhido.set(novo);
    document.documentElement.dataset["tema"] = novo; // o index.html faz o mesmo ao carregar
    try {
      localStorage.setItem("tema", novo);
    } catch {
      /* sem localStorage, a escolha vale só até recarregar */
    }
  }
}
