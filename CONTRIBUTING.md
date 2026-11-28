# Como contribuir — Dayspot

Regras de trabalho do time. Valem para os três integrantes e para o Claude Code.

---

## Fluxo de um card, do início ao fim

1. Pegue um card em **Ready** no GitHub Projects, atribua a você e mova para **In progress**.
2. Crie a branch a partir da `dev` atualizada:

   ```bash
   git switch dev
   git pull
   git switch -c feat/t0-migration-clientes
   ```

3. Faça commits pequenos, no padrão abaixo.
4. Suba a branch e abra o PR **para a `dev`** (a branch padrão do GitHub):

   ```bash
   git push -u origin feat/t0-migration-clientes
   ```

5. Mova o card para **In review** e peça revisão a outro integrante.
6. Com aprovação e CI verde, faça o merge (**merge commit**) e apague a branch.
7. Mova o card para **Done** (o `Closes #N` fecha a issue sozinho).

> Todo trabalho sai da `dev` e volta para a `dev` por PR. Cada merge na `dev` publica
> na homologação (`homolog.dayspot.app.br`). Ninguém faz push direto na `dev` nem na `main`.

### Da `dev` para a produção

1. Com a homologação conferida, abra um PR da `dev` para a `main`, com o título
   `Publicação <data>` e a lista dos PRs que entram.
2. Com aprovação e CI verde, faça o merge (**merge commit**).
3. Em **Actions › CI › Run workflow**, escolha a `main`: os testes rodam de novo e a
   produção é publicada. Roteiro, volta de versão e regra das migrations em
   [`docs/deploy.md`](docs/deploy.md).

---

## Commits

```
<tipo> - <descrição breve>
<tipo>(<escopo>) - <descrição breve>
```

- Descrição em minúsculas, no imperativo ou curta e direta, sem ponto final.
- Escopo é opcional: use quando ajuda a localizar (`web`, `api`, `db`, `ci`).
- Um commit = uma mudança. "ajustes" não é descrição.

| Tipo | Quando usar |
|---|---|
| `feat` | funcionalidade nova |
| `fix` | correção de bug |
| `docs` | só documentação |
| `test` | só testes |
| `refactor` | muda código sem mudar comportamento |
| `build` | dependências, Docker, versão de .NET/Node |
| `ci` | GitHub Actions |
| `chore` | configuração e tarefas que não entram nas outras |

Exemplos:

```
feat - migration de clientes
fix - .env.example sem espaço antes do valor
docs - padrão de commit e branch
build(web) - atualiza dependências do frontend
ci - roda dotnet test em todo PR
```

---

## Branches

```
<tipo>/t<fatia>-<descricao-com-hifens>
```

- `<tipo>`: o mesmo da tabela de commits.
- `<fatia>`: número da fatia do card (`[T0]` → `t0`, `[T1]` → `t1`).
- Branches antigas com `f<fase>` (anteriores à troca para fatias) ficam como estão.
- Descrição curta, minúsculas, **sem espaços e sem acento** (o Git não aceita espaço).

Exemplos:

```
feat/t0-migration-clientes
feat/t1-config-tenant-fixo
build/t1-setup-roteamento-web
fix/t2-sessao-expirada
```

---

## Pull requests

- **Um PR por card.**
- Título: o mesmo da issue — `[T0] 0.10 Migration: clientes`.
- Descrição com `Closes #N` (o template já traz o campo).
- Precisa de **1 aprovação de outro integrante** e **CI verde** para mergear.
- Merge por **merge commit** (o botão "Create a merge commit" do GitHub), não squash: os commits da branch, já no padrão, entram na `main` como estão.
- Apague a branch depois do merge.

Quem revisa confere: critérios de aceite da issue, testes, e se o card foi movido no board.

---

## Board — obrigatório

O status de card mora **só** no board do GitHub Projects (**Dayspot Devops**).
Nenhum arquivo do repositório repete status — espelho em texto envelhece e dá
conflito em todo PR.

| Momento | No board |
|---|---|
| Começou o card | mover para **In progress** |
| Abriu o PR | mover para **In review** |
| Mergeou | mover para **Done** |

**PR que não move o card não é aprovado.**

`docs/cards.md` lista os cards que existem, sem status. Muda só quando cards são
criados ou apagados (uma fatia por vez), num PR `docs`.

---

## Comandos de desenvolvimento

Com o Postgres do `compose.yml` de pé, migrado e com seed (passo a passo no
[`README.md`](README.md#subindo-o-ambiente)):

```bash
dotnet run --project src/Api      # API em :8080, lendo o .env da raiz
set -a; . ./.env; set +a          # DATABASE_URL e MIGRATIONS_DATABASE_URL para os testes de banco
dotnet test                       # sem as duas variáveis, os testes de banco são pulados
dotnet format                     # o CI falha se sobrar diferença (--verify-no-changes)
```

- Schema muda só por EF Core Migration em `src/Api/Migrations/`
  (`dotnet ef migrations add Nome --project src/Api`), com o SQL do `Up` e do `Down`
  escrito à mão em `migrationBuilder.Sql`. A classe em `src/Api/Dados/Modelos.cs` e o
  snapshot acompanham no mesmo PR: o CI roda `dotnet ef migrations has-pending-model-changes`.
- Banco de dev migrado pela ferramenta antiga (sem a tabela `__EFMigrationsHistory`):
  recrie com `docker compose down -v` e `docker compose up -d`, depois migrations e seed
  ([README](README.md#subindo-o-ambiente)). Apaga os dados locais. No `.env`, troque as
  variáveis antigas de migration pela `MIGRATIONS_DATABASE_URL` do `.env.example`.
- No `web/`: `npm run lint` e `npm run build`.

---

## Definição de pronto

Está em [`docs/backlog.md`](docs/backlog.md#definição-de-pronto). Resumo: mergeado na
`dev`, PR aprovado por outro integrante, CI verde, critérios de aceite verificados e,
se mexeu no banco, migration versionada.
