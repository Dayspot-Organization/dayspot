// A lista de senhas comuns (#211), a mesma que a API embute: src/senhas-comuns.txt.
// O angular.json carrega .txt como texto.
import texto from "../../senhas-comuns.txt";
import { lerLista } from "./senha";

export const SENHAS_COMUNS = lerLista(texto);
