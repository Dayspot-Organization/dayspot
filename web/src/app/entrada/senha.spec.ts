// Teste da regra da senha (#211), com os casos de tests/Api.Tests/ForcaSenhaTests.cs.
// Roda com `npm test` (node --test, sem dependência nova).
import { test } from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { forcaDaSenha, lerLista } from "./senha.ts";

const comuns = lerLista(readFileSync(new URL("../../senhas-comuns.txt", import.meta.url), "utf8"));
const medir = (senha: string) => forcaDaSenha(senha, "Ana Souza", "ana.rocha@salao.example", comuns);

const COMUM = "Essa senha é muito usada. Escolha outra.";
const PESSOAL = "A senha não pode ter seu nome ou seu e-mail.";

test("recusa curta, comum e com nome ou e-mail", () => {
  for (const [senha, texto] of [
    ["123456789", "Falta 1 caractere."],
    ["😀😀😀😀😀😀😀😀😀", "Falta 1 caractere."], // 9 caracteres, 18 unidades UTF-16
    ["1234567890", COMUM],
    ["QwertyUIOP", COMUM],
    ["xx-ana.rocha-xx", PESSOAL],
    ["SOUZA-2024!", PESSOAL],
    ["eu-sou-souzá-sim", PESSOAL],
    ["çççççççççç", COMUM], // sem acento, vira cccccccccc
    ["x".repeat(1025), "Use até 1024 caracteres."],
  ]) {
    assert.equal(medir(senha).texto, texto, senha);
    assert.notEqual(medir(senha).estado, "ok", senha);
  }
});

test("aceita senha boa", () => {
  for (const senha of ["cafe-com-pao-42", "banana-madura", "çãéíóúçãéí"]) {
    assert.deepEqual(medir(senha), { nivel: 4, estado: "ok", texto: "Senha boa." }, senha);
  }
});

test("o medidor enche até os 10 caracteres", () => {
  assert.deepEqual(
    ["", "abc", "abcde", "abcdefgh"].map((s) => [medir(s).nivel, medir(s).texto]),
    [
      [0, "Faltam 10 caracteres."],
      [1, "Faltam 7 caracteres."],
      [2, "Faltam 5 caracteres."],
      [3, "Faltam 2 caracteres."],
    ],
  );
});

