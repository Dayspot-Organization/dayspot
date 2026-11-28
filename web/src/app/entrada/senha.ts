// Regra da senha nova (#211): a mesma de src/Api/Auth/ForcaSenha.cs, com os mesmos textos.
// Sem Angular nem a lista embutida, para o teste rodar no `node --test` (senha.spec.ts).

export const MIN_SENHA = 10;
export const MAX_SENHA = 1024;
/** Parte do nome ou do e-mail com menos que isso não barra: "Ana" não barra "banana". */
const MIN_PARTE = 4;

export interface Forca {
  /** Barras cheias do medidor, de 0 a 4: enchem até os 10 caracteres. */
  nivel: number;
  /** falta: ainda curta; ruim: comum ou com nome/e-mail; ok: pode usar. */
  estado: "falta" | "ruim" | "ok";
  /** O que aparece embaixo do campo e, ao continuar, vira o erro. */
  texto: string;
}

/** Minúsculas e sem acento, como a API: "JOÃO" e "joao" são iguais. */
const normalizar = (t: string) => t.normalize("NFD").replace(/\p{Mn}/gu, "").toLowerCase();

/** As linhas do senhas-comuns.txt, sem os comentários. */
export function lerLista(texto: string): ReadonlySet<string> {
  return new Set(texto.split("\n").filter((l) => l && !l.startsWith("#")));
}

export function forcaDaSenha(senha: string, nome: string, email: string, comuns: ReadonlySet<string>): Forca {
  // Em caracteres, como a API (runas), não em unidades UTF-16.
  const tamanho = [...senha].length;
  if (tamanho < MIN_SENHA) {
    const falta = MIN_SENHA - tamanho;
    return {
      nivel: Math.floor(tamanho / 2.5),
      estado: "falta",
      texto: falta === 1 ? "Falta 1 caractere." : `Faltam ${falta} caracteres.`,
    };
  }
  if (tamanho > MAX_SENHA) return { nivel: 4, estado: "ruim", texto: "Use até 1024 caracteres." };
  const s = normalizar(senha);
  if (comuns.has(s)) return { nivel: 4, estado: "ruim", texto: "Essa senha é muito usada. Escolha outra." };
  const partes = [...normalizar(nome).split(/\s+/), normalizar(email.split("@")[0])];
  if (partes.some((p) => p.length >= MIN_PARTE && s.includes(p))) {
    return { nivel: 4, estado: "ruim", texto: "A senha não pode ter seu nome ou seu e-mail." };
  }
  return { nivel: 4, estado: "ok", texto: "Senha boa." };
}
