<div align="center">

<img src="web/public/favicon.svg" alt="Símbolo do Dayspot" width="96">

<h1>Dayspot 📅</h1>

<p>
  <img alt=".NET 10" src="https://img.shields.io/badge/.NET-10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white">
  <img alt="EF Core 10" src="https://img.shields.io/badge/EF_Core-10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white">
  <img alt="PostgreSQL 16" src="https://img.shields.io/badge/PostgreSQL-16-4169E1?style=for-the-badge&logo=postgresql&logoColor=white">
  <img alt="Angular 22" src="https://img.shields.io/badge/Angular-22-DD0031?style=for-the-badge&logo=angular&logoColor=white">
  <img alt="TypeScript 6" src="https://img.shields.io/badge/TypeScript-6-3178C6?style=for-the-badge&logo=typescript&logoColor=white">
</p>
<p>
  <img alt="Node.js 22" src="https://img.shields.io/badge/Node.js-22-5FA04E?style=for-the-badge&logo=nodedotjs&logoColor=white">
  <img alt="xUnit.v3 4" src="https://img.shields.io/badge/xUnit.v3-4-512BD4?style=for-the-badge&logo=dotnet&logoColor=white">
  <img alt="Docker Compose" src="https://img.shields.io/badge/Docker_Compose-2496ED?style=for-the-badge&logo=docker&logoColor=white">
  <a href="https://github.com/Dayspot-Organization/dayspot/actions"><img alt="CI" src="https://img.shields.io/github/actions/workflow/status/Dayspot-Organization/dayspot/ci.yml?branch=main&style=for-the-badge&label=CI&logo=githubactions&logoColor=white"></a>
  <img alt="Status: em desenvolvimento" src="https://img.shields.io/badge/status-em_desenvolvimento-yellow?style=for-the-badge">
</p>

<p><strong>Plataforma multi-tenant de agendamentos: o cliente final agenda sem criar conta e recebe confirmação e lembrete pelo Telegram.</strong></p>

</div>

---

Cada organização (barbearia, clínica, estúdio) tem seus próprios usuários, serviços, horários e clientes, isolados das demais.

> 🚧 Em desenvolvimento. Andamento no GitHub Projects (Dayspot Devops).

---

**Índice**

- [🎯 Sobre o projeto](#-sobre-o-projeto)
- [🛠️ Stack](#️-stack)
- [🚀 Começando](#-começando)
  - [Pré-requisitos](#pré-requisitos)
  - [Clonando](#clonando)
  - [Variáveis de ambiente](#variáveis-de-ambiente)
  - [Subindo o ambiente](#subindo-o-ambiente)
  - [Frontend](#frontend)
- [🗄️ Banco de dados](#️-banco-de-dados)
  - [Migrations](#migrations)
  - [Seed e verificação de isolamento](#seed-e-verificação-de-isolamento)
- [📁 Estrutura](#-estrutura)
- [📍 API](#-api)
- [📐 Convenções](#-convenções)
- [🗺️ Roteiro](#️-roteiro)
- [📚 Documentação](#-documentação)
- [🔐 Dados pessoais](#-dados-pessoais)
- [🤝 Equipe](#-equipe)
- [📫 Contribuindo](#-contribuindo)

---

## 🎯 Sobre o projeto

São dois papéis dentro de cada organização:

| Papel | O que faz |
| --- | --- |
| **Administrador** | Gerencia a empresa — convida membros, remove prestadores, define serviços e preços, vê o financeiro de todos. Também pode atender. |
| **Prestador** | Vê e opera apenas a própria agenda. |

O cliente final não tem conta. Ele se vincula ao bot por um link `t.me/<bot>?start=<token>` e passa a receber confirmação, lembrete e aviso de cancelamento no Telegram.

O isolamento entre organizações é garantido no banco, com Row Level Security, e não apenas na aplicação.

---

## 🛠️ Stack

| Camada | Tecnologia |
| --- | --- |
| API e worker | C# — ASP.NET Core (.NET 10), minimal APIs |
| Acesso a dados | EF Core + Npgsql |
| Migrations | EF Core Migrations, com o SQL escrito à mão (`migrationBuilder.Sql`) |
| Banco | PostgreSQL 16, com Row Level Security |
| Frontend | Angular 22 + TypeScript (Angular CLI, componentes standalone, signals, `@angular/router`) |
| Notificações | Telegram Bot API |
| Ambiente | Docker Compose |

---

## 🚀 Começando

### Pré-requisitos

- [Docker](https://docs.docker.com/get-docker/) com Compose
- [.NET SDK](https://dotnet.microsoft.com/download) 10 (`10.0.100` ou feature mais nova, conforme o `global.json`)
- [Node.js](https://nodejs.org/) 22 — para o `web/` (a versão do CI)
- `psql` (cliente do PostgreSQL) — para o seed e a verificação de isolamento

O `dotnet-ef` (migrations) não se instala à parte: é ferramenta local, com versão fixa em
`.config/dotnet-tools.json` (a mesma do EF Core). `dotnet tool restore` baixa.

### Clonando

```bash
git clone git@github.com:Dayspot-Organization/dayspot.git
cd dayspot
```

### Variáveis de ambiente

Use o `.env.example` como referência:

```bash
cp .env.example .env
```

| Variável | Para quê |
| --- | --- |
| `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB` | Criam o banco e o **dono das tabelas** no container do `compose.yml` |
| `APP_USER_PASSWORD` | Senha do `app_user`, criado por `db/init/app_user.sh`; a mesma da `DATABASE_URL` |
| `MIGRATIONS_DATABASE_URL` | Conexão do **dono das tabelas** — só migrations (`dotnet ef`) e seed. Ignora o RLS. |
| `DATABASE_URL` | Conexão do **`app_user`** — a única que a API usa. Sujeita ao RLS. |
| `TENANT_FIXO`, `PRESTADOR_FIXO`, `SERVICO_FIXO` | Sessão e serviço da Fatia 1, sem login (UUIDs do seed, tabela abaixo). Temporárias: saem na 2.12 e na 6.6 |
| `PORTA_HTTP` | Porta da API; opcional, padrão `8080` (a do proxy do `ng serve`) |

> São duas conexões de propósito. Se a API conectar como dono das tabelas, todas as policies de RLS viram decoração e o isolamento entre organizações deixa de existir.

### Subindo o ambiente

```bash
docker compose up -d      # PostgreSQL em localhost:5467, Adminer em localhost:8088
dotnet tool restore       # dotnet-ef na versão do .config/dotnet-tools.json
dotnet ef database update --project src/Api   # aplica as migrations (MIGRATIONS_DATABASE_URL do .env)
set -a; . ./.env; set +a  # exporta o .env para o psql e o dotnet test
psql "$MIGRATIONS_DATABASE_URL" -f db/seed.sql   # os testes de banco leem o seed

dotnet run --project src/Api   # sobe em :8080 lendo o .env; sem variável obrigatória, não sobe
dotnet test                    # sem DATABASE_URL e MIGRATIONS_DATABASE_URL exportadas, os testes de banco são pulados
dotnet format                  # o CI roda com --verify-no-changes
```

Os testes de banco rodam contra o Postgres do `compose.yml`, com as migrations
e o seed aplicados: gravam e apagam os próprios dados (UUIDs `f…`) e desfazem o resto
por `ROLLBACK`.

> ⚠️ O `db/init/app_user.sh` só roda com o volume do banco vazio. Se o seu volume é anterior ao `db/init`, rode `docker compose down -v` uma vez antes do `up` — isso **apaga os dados locais** do banco.

> ⚠️ **Banco de dev migrado pela ferramenta antiga.** Quem migrou o banco antes das EF Core Migrations não tem a tabela `__EFMigrationsHistory`: o `dotnet ef database update` tentaria criar tudo de novo e falharia. Não há transição: recrie o banco com `docker compose down -v` e `docker compose up -d` (**apaga os dados locais**), depois aplique as migrations e o seed. No `.env`, troque as variáveis antigas de migration pela `MIGRATIONS_DATABASE_URL` do `.env.example`.

### Frontend

```bash
cd web
npm install
npm run dev     # ng serve em http://localhost:4200 (abre em /agendamentos)
npm run lint    # o CI roda lint e build em todo PR
npm run build   # ng build (TypeScript strict e templates strict)
```

Componentes standalone com signals. As rotas ficam em `web/src/app/app.routes.ts` (`@angular/router`); `app.ts`/`app.html` é o layout com a navegação e o `<router-outlet />` onde cada tela aparece. Chamadas à API passam pelo serviço `Api` (`web/src/app/api/`), com o `HttpClient` e um interceptor que converte as respostas de erro (`docs/erros-api.md`).

Em dev, o `ng serve` repassa (`web/proxy.conf.json`) tudo que começa com `/api` para a API em `localhost:8080`, sem reescrever o caminho: a SPA chama `/api/agendamentos` na mesma origem e não precisa de CORS.

---

## 🗄️ Banco de dados

### Migrations

EF Core Migrations em `src/Api/Migrations/`: uma classe por migration, com o SQL escrito à mão em `migrationBuilder.Sql(...)` no `Up` e no `Down`. O `dotnet ef` conecta como dono das tabelas pela `MIGRATIONS_DATABASE_URL`, lida do ambiente ou do `.env`, então os comandos abaixo rodam contra o Postgres do `compose.yml`, com ele de pé:

| Comando | O que faz |
| --- | --- |
| `dotnet ef migrations list --project src/Api` | O que já rodou e o que falta |
| `dotnet ef database update --project src/Api` | Aplica todas as pendentes |
| `dotnet ef database update <migration> --project src/Api` | Volta até a migration dada, desfazendo as posteriores |
| `dotnet ef database update 0 --project src/Api` | Desfaz todas |
| `dotnet ef migrations add NomeDaMigration --project src/Api` | Nova migration e snapshot atualizado; troque o corpo gerado pelo SQL em `migrationBuilder.Sql` |
| `dotnet ef migrations has-pending-model-changes --project src/Api` | Falha se o modelo do `AppDb` mudou sem migration (o CI roda) |

Toda migration tem `Up` e `Down`, e os dois são testados: `database update`, `database update 0`, `database update`.

### Seed e verificação de isolamento

```bash
set -a; source .env; set +a   # exporta as variáveis para o psql
dotnet ef database update --project src/Api   # o seed exige o banco migrado
psql "$MIGRATIONS_DATABASE_URL" -f db/seed.sql   # como dono das tabelas: ignora o RLS

# sem psql instalado, pelo container do compose:
docker compose exec -T db sh -c 'psql -U "$POSTGRES_USER" -d "$POSTGRES_DB"' < db/seed.sql
```

O seed roda numa transação só e para no primeiro erro. Pode rodar de novo: o que já
existe (mesmo `id`) é pulado. No CI, com o Postgres do workflow (0.17), o passo é o
mesmo, depois das migrations: `psql "$MIGRATIONS_DATABASE_URL" -f db/seed.sql`.

O seed cria dois tenants fictícios (`alfa` e `beta`). São dois de propósito: com um só não é possível testar isolamento entre tenants. UUIDs fixos usados no config da T1 (1.1):

| Variável | UUID | O que é |
|---|---|---|
| `TENANT_FIXO` | `11111111-1111-1111-1111-111111111111` | tenant Barbearia Alfa |
| `PRESTADOR_FIXO` | `cccccccc-cccc-cccc-cccc-cccccccccccc` | Alfa Prestador (papel `prestador`) |
| `SERVICO_FIXO` | `11111111-0005-0000-0000-000000000001` | serviço Corte do Alfa |

O administrador do Alfa é `aaaaaaaa-…` e o da Beta, `bbbbbbbb-…`; o padrão dos demais
UUIDs está no cabeçalho de `db/seed.sql`. Os usuários do seed não fazem login: o
`senha_hash` é falso.

Conectado como `app_user` (`psql "$DATABASE_URL"`, **nunca** como o dono das tabelas):

```sql
BEGIN;
SELECT set_config('app.tenant_id', '11111111-1111-1111-1111-111111111111', true);

SELECT count(*) FROM clientes;   -- só clientes do tenant Alfa

SELECT count(*) FROM clientes    -- deve retornar 0
WHERE tenant_id = '22222222-2222-2222-2222-222222222222';

ROLLBACK;
```

> ⚠️ Se o segundo `SELECT` retornar alguma linha, o Row Level Security não está ativo. Ver [`docs/guia-banco-de-dados.md`](docs/guia-banco-de-dados.md).

---

## 📁 Estrutura

Hoje:

```
dayspot/
├── .github/
│   ├── workflows/ci.yml        # em push e PR para a main: .NET (dotnet format, build; Postgres 16 com migrations up, down e up de novo; testes, inclusive os de banco) e web (lint, build)
│   └── pull_request_template.md
├── src/Api/                    # a API (ASP.NET Core): Program.cs, rotas e handlers, Erros.cs (docs/erros-api.md), Transacao.cs
├── src/Api/Config.cs           # leitura das variáveis de ambiente e do .env — só ele lê o ambiente
├── src/Api/Auth/               # hash de senha (Argon2id) e normalização de e-mail
├── src/Api/Dados/              # EF Core: AppDb (mapeia o schema das migrations), Modelos e Consultas — toda consulta recebe tenantId
├── src/Api/Migrations/         # EF Core Migrations: SQL em migrationBuilder.Sql (Up e Down) e o snapshot do modelo
├── tests/Api.Tests/            # xUnit: API, config, auth e banco (isolamento por RLS, exclusão de tenant, consultas)
├── db/seed.sql                 # dois tenants fictícios (alfa e beta)
├── db/init/                    # app_user.sh: cria o app_user no primeiro up do compose
├── web/                        # frontend Angular
├── docs/
├── compose.yml                 # PostgreSQL 16 + Adminer
├── Dayspot.slnx                # solução .NET (API + testes)
├── global.json                 # versão do SDK e runner de testes
├── .config/dotnet-tools.json   # dotnet-ef em versão fixa, a do EF Core
├── .env.example
├── CLAUDE.md                   # regras para o Claude Code
├── CONTRIBUTING.md
└── DESIGN.md                   # referência visual do web/
```

Estrutura-alvo — cada pasta nasce no card que a usa:

```
src/Worker/            # consumidor da fila de notificações (T3)
src/Api/Notificador/   # interface Notificador + implementação Telegram (T3)
```

---

## 📍 API

Os endpoints nascem na fatia T1 e são documentados conforme entram. O que já está definido é o **formato de erro**, usado por todos os handlers:

```json
{
  "erro": {
    "codigo": "conflito_horario",
    "mensagem": "Já existe um agendamento nesse horário.",
    "detalhes": { "prestador_id": "5f0c2a9e-3b7d-4c1e-9a8f-2d6b1e7c4a90" }
  }
}
```

| Código | HTTP | Quando |
| --- | --- | --- |
| `requisicao_invalida` | 400 | JSON malformado, campo obrigatório ausente, valor inválido |
| `nao_encontrado` | 404 | ID inexistente — ou de outro tenant |
| `conflito_horario` | 409 | Agendamento sobrepõe outro (SQLSTATE `23P01`) |
| `erro_interno` | 500 | Qualquer coisa não prevista |

Os códigos previstos para fatias seguintes (401, 403 e outros 409) estão listados em [`docs/erros-api.md`](docs/erros-api.md).

Nenhum erro de banco vaza para a resposta: nome de constraint, coluna e tabela ficam no log. Registro de outro tenant retorna 404 e não 403, para não confirmar que o ID existe.

Especificação completa em [`docs/erros-api.md`](docs/erros-api.md).

---

## 📐 Convenções

🗄️ **Banco.** Toda alteração de schema entra como migration versionada. Nenhum `ALTER TABLE` rodado à mão, em nenhuma circunstância.

🔑 **Contexto de tenant.** Toda requisição abre transação e define `app.tenant_id` via `set_config(..., true)`. Nunca `SET` sem escopo local — com pool de conexões, a variável vaza para a requisição seguinte e vira vazamento de dados entre organizações.

📦 **Acesso a dados.** EF Core com Npgsql. O schema nasce das EF Core Migrations, com SQL escrito à mão; o `AppDb` só o mapeia. As consultas ficam em `src/Api/Dados/Consultas.cs`, e toda uma recebe `tenantId` e filtra por ele. O RLS é a rede de segurança; o filtro na consulta é a primeira barreira.

🕐 **Fuso horário.** Tudo em UTC no banco (`timestamptz`). Conversão apenas na borda, usando `tenants.fuso_horario`.

🌿 **Commits, branches e PR.** Commit `<tipo> - <descrição>`, branch `<tipo>/t<fatia>-<descrição>` a partir da `main`, um PR por card com `Closes #N`, aprovação de outro integrante e CI verde. Todo PR move o card no GitHub Projects. Detalhes em [`CONTRIBUTING.md`](CONTRIBUTING.md).

---

## 🗺️ Roteiro

O backlog é dividido em fatias verticais: cada uma atravessa banco, API e frontend e termina em algo demonstrável. Uma fatia pode levar mais de uma sprint.

| Fatia | Entrega |
| --- | --- |
| **T0** — Fundação | Repositório, Docker, CI, migrations, RLS, seed com dois tenants |
| **T1** — Agendar | Criar e listar agendamentos no navegador (tenant fixo, sem login) |
| **T2** — Entrar | Signup, login, sessão, isolamento real entre tenants, papéis e convites |
| **T3** — Avisar | Vincular cliente ao Telegram e enviar confirmação |
| **T4** — Lembrar | Lembrete automático X horas antes |
| **T5** — Cancelar | Cancelar, reagendar e concluir, inclusive pelo bot; remover membro |
| **T6** — Configurar | Serviços com preço, disponibilidades, slots livres e fuso por tenant |
| **T7** — Acompanhar | Calendário semanal, resumo financeiro do mês e exportação `.ics` |
| **T8** — Entregar | Deploy e documentação final |

Backlog detalhado em [`docs/backlog.md`](docs/backlog.md); cards já criados em [`docs/cards.md`](docs/cards.md). Status de card, só no GitHub Projects.

---

## 📚 Documentação

| Arquivo | Conteúdo |
| --- | --- |
| [`docs/requisitos.md`](docs/requisitos.md) | Requisitos, arquitetura e decisões técnicas tomadas (§12) |
| [`docs/especificacao-fluxo.md`](docs/especificacao-fluxo.md) | Papéis e permissões, ciclo de vida do agendamento, telas e modelo de dados |
| [`docs/backlog.md`](docs/backlog.md) | Backlog por fatias verticais |
| [`docs/guia-banco-de-dados.md`](docs/guia-banco-de-dados.md) | Migrations comentadas, armadilhas do RLS e acesso a dados |
| [`docs/erros-api.md`](docs/erros-api.md) | Formato de erro da API |
| [`docs/cards.md`](docs/cards.md) | Cards do GitHub Projects por fatia (sem status) |
| [`CONTRIBUTING.md`](CONTRIBUTING.md) | Padrão de commit, branch, PR e board |
| [`CLAUDE.md`](CLAUDE.md) | Regras para o Claude Code |

---

## 🔐 Dados pessoais

Projeto acadêmico. O sistema armazena nome, telefone e identificador de Telegram de clientes, com a finalidade única de operar agendamentos e enviar as notificações correspondentes.

A exportação `.ics` é um download feito pelo prestador: o arquivo leva nome do cliente e horário de cada agendamento para o calendário do aparelho dele (e para o iCloud, se o calendário sincroniza). A finalidade é a mesma: o prestador consultar a própria agenda. O arquivo é uma fotografia; depois de baixado, o sistema não o atualiza nem o apaga.

O `seed.sql` usa exclusivamente dados fictícios. Não versione nem carregue dados reais de terceiros neste repositório. Para demonstrações, use contatos do próprio grupo, com ciência dos envolvidos.

---

## 🤝 Equipe

Projeto acadêmico

<table>
  <tr>
    <td align="center">
      <a href="https://github.com/igorhsalgado">
        <img src="https://github.com/igorhsalgado.png" width="100px" alt="Igor Salgado"/><br>
        <sub><b>Igor Salgado</b></sub>
      </a>
    </td>
    <td align="center">
      <a href="https://github.com/thisdev-davi">
        <img src="https://github.com/thisdev-davi.png" width="100px" alt="Davi Oliveira"/><br>
        <sub><b>Davi Oliveira</b></sub>
      </a>
    </td>
    <td align="center">
      <a href="https://github.com/J0aoCunha">
        <img src="https://github.com/J0aoCunha.png" width="100px" alt="João Victor da Silva Cunha"/><br>
        <sub><b>João Victor Cunha</b></sub>
      </a>
    </td>
  </tr>
</table>

---

## 📫 Contribuindo

O fluxo completo está em [`CONTRIBUTING.md`](CONTRIBUTING.md). Em resumo:

1. Pegue um card em **Ready** no GitHub Projects, atribua a você e mova para **In progress**
2. `git switch -c <tipo>/t<fatia>-<descrição>` a partir da `main`
3. Commits no padrão `<tipo> - <descrição>`
4. Abra o PR com `Closes #N` na descrição e mova o card para **In review**
5. Com aprovação de outro integrante e CI verde, faça **squash merge** e mova o card para **Done**

Nenhum item entra em **Done** sem estar mergeado na `main`, com PR aprovado, CI verde e critérios de aceite verificados.

