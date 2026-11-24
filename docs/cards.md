# Cards — Dayspot

Cards que existem no board **Dayspot Devops** (GitHub Projects da organização
[Dayspot-Organization](https://github.com/Dayspot-Organization/dayspot)), por fatia.
Só a lista: status, responsável e sprint ficam no board. Atualize este arquivo
quando cards forem criados ou apagados (uma fatia por vez), num PR `docs`.

Dependências copiadas do "Depende de" de cada issue — o corpo da issue é o contrato.

Conferido em 24/11/2026.

## Mudanças de 24/11/2026 (review)

Cards novos da Fatia P, com os achados do review do código, fora do `docs/backlog.md`:

| Card | Item | O que mudou |
|---|---|---|
| #198 a #200 | — | Cards novos: achados do backend, achados do front e docs alinhados ao código |

## Mudanças de 18/11/2026 (piloto)

A primeira publicação é um piloto com uma empresa (`docs/backlog.md` v2.6).

| Card | Item | O que mudou |
|---|---|---|
| #128, #129, #130, #135, #136 | 2.18–2.22 | Passam para a Fatia 2E, depois do piloto; os itens mantêm a numeração |
| #127 | 2.17 | Passa para a Fatia 2E (20/11/2026): só a 2E depende da migration de convites |
| #150 | — | Passa para a Fatia 2E |
| #149 | — | Passa para a Fatia P. Decidido: o Neon não será recriado |
| #168 a #178 | — | Cards novos da Fatia P |
| #179 a #182 | — | Cards novos da Fatia P: design system, documentação, concorrência no agendamento público e este alinhamento |

## Mudanças de 18/11/2026 (agendamento público)

Cards novos da Fatia 2, fora do `docs/backlog.md`:

| Card | Item | O que mudou |
|---|---|---|
| #161 | — | Card novo: agenda semanal no design v2.1 (PR #157) |
| #162 | — | Card novo: rotas públicas por slug e criação atômica de cliente e agendamento |
| #163 | — | Card novo: página pública `/agendar/:slug` e novo agendamento no design v2.1 (PR #158) |
| #165 | — | Card novo: CI em PR para qualquer base, build Docker e 1 aprovação para a `main` |

## Mudanças de 17/11/2026 (migração de stack)

Projeto renomeado para dayspot (#143); repositório em `Dayspot-Organization/dayspot`.

| Card | Item | O que mudou |
|---|---|---|
| — | — | Backend migrado de Go (pgx + sqlc, goose) para .NET + EF Core no PR #144, sem card: API em `src/Api/`, migrations em `src/Api/Migrations/`, acesso a dados em `src/Api/Dados/`, erros em `src/Api/Erros.cs`, testes em `tests/Api.Tests/`. Saíram `cmd/`, `internal/`, `db/migrations/` e `db/queries/` |
| #146 | — | Card novo: frontend `web/` migrado de React + Vite para Angular (`web/src/app/`), fechado pelo PR #147 |
| #148 | — | Card novo, retroativo: backend migrado de Go para .NET + EF Core, fechado pelo PR #144 |
| #149 | — | Card novo: banco de produção no Neon na stack nova (continuação do #138): `MIGRATIONS_DATABASE_URL`, `channel_binding` na URL, `.env.example` e RNF09. Decisão em aberto: recriar o banco com `dotnet ef database update` ou registrar as 10 migrations em `__EFMigrationsHistory` |
| #150 | — | Card novo: teste que falha se um handler fora de `src/Api/Dados/` acessar um `DbSet` do `AppDb` |

Os títulos de #63 (goose) e #55 (Vite + React) continuam como no board; a stack
atual é a da tabela acima.

## Mudanças de 23/10/2026 (backlog v2.3)

Revisão da Fatia 2. Corpo editado no board (rodapé "Atualizado em 23/10/2026"):

| Card | Item | O que mudou |
|---|---|---|
| #112 | 2.2 | E-mail ou slug já usado: 409 `email_em_uso` e `slug_em_uso` |
| #113 | 2.3 | Dona da decisão da escolha de empresa (§8.2): `POST /api/sessao/empresa`; credencial inválida: 401 `credencial_invalida` |
| #116 | 2.6 | Sessão sem empresa escolhida só acessa `/me`, logout, a escolha e o aceite do convite |
| #117 | 2.7 | `Escopo` também no `POST /agendamentos`; passa a depender da 1.5 |
| #118 | 2.8 | Rate limit: 429 `muitas_tentativas`, 5 falhas por e-mail e 20 por IP em 15 minutos |
| #119 | 2.9 | Sem decisão em aberto; passa a depender da 2.3 |
| #120 | 2.10 | Tela de escolha de empresa |
| #123 | 2.13 | Passa a depender da 2.7 e da 2.12 |
| #126 | 2.16 | Testa também o `POST /agendamentos` |
| #128 | 2.18 | Link do convite copiado pelo administrador, sem e-mail; devolve o token |
| #129 | 2.19 | Convidado sem conta vai para a 2.22; passa a depender dela; rota `POST /api/convites/{token}/aceite` |
| #130 | 2.20 | Lista membros pela 2.21; passa a depender dela; "copiar link" do convite |
| #136 | 2.22 | Rotas `GET /api/convites/{token}` e `POST /api/convites/{token}/conta`; e-mail com conta: 409 `email_em_uso` |

Cards novos: #135 (2.21) e #136 (2.22).

## Mudanças de 02/10/2026 (backlog v2.2)

Decisões de papéis, agenda e financeiro (`requisitos.md` v0.8). Títulos e
dependências não mudaram; mudou o corpo destes cards, já editado no board
(rodapé "Atualizado em 02/10/2026"):

| Card | Item | O que mudou |
|---|---|---|
| #65 | 0.9 | `CHECK` de papel só com administrador e prestador; `ALTER TABLE tenants ADD COLUMN criado_por` (a 002 não muda) |
| #67 | 0.11 | `servicos.preco_centavos` sem `DEFAULT`, com `CHECK >= 0` |
| #68 | 0.12 | `valor_centavos` com `CHECK >= 0`; índice `idx_agendamentos_prestador_inicio`; administrador como prestador |
| #70 | 0.14 | `REVOKE DELETE ON agendamentos FROM app_user`; cinco tabelas fora do RLS; sem policy por prestador |
| #72 | 0.16 | Ordem `usuarios` → `tenants` → `memberships`; no Alfa, administrador criador que atende + 1 prestador; preços |
| #52 | 1.5 | Grava `valor_centavos` copiado do serviço; só prestador com membership ativo |
| #53 | 1.6 | `prestador_id` opcional, sem regra de sessão (fica na 2.7) |
| #57 | 1.10 | Continua lista simples; o calendário semanal é a 7.1 |

Itens novos do backlog, sem card ainda: 5.8, 6.5a e 6.5b (a 6.5 foi dividida) e 7.1 a 7.7 (Fatia 7 Acompanhar). Entregar passou a ser a Fatia 8 (8.1
a 8.7).

## Fatia 0 — Fundação

| Card | Item | Título | Tam. | Área | Depende de |
|---|---|---|---|---|---|
| #63 | 0.7 | Configurar goose + migration 001 (extensões e app.current_tenant_id) | P | banco | — |
| #64 | 0.8 | Migration: tenants | M | banco | 0.7 |
| #65 | 0.9 | Migration: usuarios e memberships | M | banco | 0.8 |
| #66 | 0.10 | Migration: clientes | M | banco | 0.8 |
| #67 | 0.11 | Migration: servicos e disponibilidades | M | banco | 0.9 |
| #68 | 0.12 | Migration: agendamentos + constraint de sobreposição + trigger | M | banco | 0.10, 0.11 |
| #69 | 0.13 | Migration: notificacoes (outbox) com UNIQUE(agendamento_id, tipo, versao) | M | banco | 0.12 |
| #70 | 0.14 | Migration: RLS com FORCE, USING e WITH CHECK em todas as tabelas | G | banco, **caminho crítico** | 0.13, 0.15, 0.16, 0.17 |
| #71 | 0.15 | Script de init: app_user sem BYPASSRLS e grants | P | banco, infra | 0.7 |
| #72 | 0.16 | Seed com dois tenants fictícios e UUIDs fixos | P | banco | 0.13 |
| #73 | 0.17 | Subir Postgres no CI para os testes de banco | M | infra | 0.7 |
| #74 | 0.18 | Teste: exclusão de tenant não quebra em chave estrangeira | P | banco | 0.13, 0.17 |

**Prioridade:** 0.7 -> 0.8 -> 0.9 -> 0.10 -> 0.11 -> 0.12 -> 0.13 -> 0.15 -> 0.16 -> 0.14 -> 0.18

0.17 (em paralelo, depois de 0.7). A 0.16 vem antes da 0.14 porque o teste de isolamento precisa dos dois tenants do seed.

Itens 0.1 a 0.6 não têm card: as issues da numeração antiga foram apagadas.

## Fatia 1 — Agendar

| Card | Item | Título | Tam. | Área | Depende de |
|---|---|---|---|---|---|
| #48 | 1.1 | Config com TENANT_FIXO e PRESTADOR_FIXO (temporário, remover na T2) | P | backend | — |
| #49 | 1.2 | Camada de repositório: toda função recebe tenantID como 1º parâmetro | M | backend, banco | migrations e seed da Fatia 0 |
| #50 | 1.3 | Abrir transação por requisição com set_config('app.tenant_id', ...) | M | backend, banco, **caminho crítico** | 1.1, 1.2 |
| #51 | 1.4 | CRUD mínimo de clientes (criar e listar) | M | backend | 1.2, 1.3, 1.7 |
| #52 | 1.5 | POST /agendamentos traduzindo SQLSTATE 23P01 para HTTP 409 | M | backend, banco | 1.2, 1.3, 1.4, 1.7 |
| #53 | 1.6 | GET /agendamentos com filtro por período | M | backend | 1.2, 1.3, 1.7 |
| #54 | 1.7 | Padronizar formato de resposta de erro da API | P | backend | — |
| #55 | 1.8 | Setup Vite + React + TypeScript + roteamento | P | frontend | — |
| #56 | 1.9 | Cliente HTTP com tratamento centralizado de erro | M | frontend | 1.7, 1.8 |
| #57 | 1.10 | Tela de listagem de agendamentos | M | frontend | 1.6, 1.9 |
| #58 | 1.11 | Formulário de novo agendamento (data e hora digitadas, sem slots) | M | frontend | 1.4, 1.5, 1.9 |
| #59 | 1.12 | Estados de carregamento e exibição de erro | M | frontend | 1.9, 1.10, 1.11 |
| #107 | 1.13 | 404 e 405 de rota inexistente no formato da API, sem abrir transação | P | backend | 1.3, 1.7 |
| #108 | 1.14 | Timeouts do servidor HTTP e verificação do banco na subida | P | backend | 1.3 |

**Prioridade:** 1.1 -> 1.2 -> 1.3 -> 1.4 -> 1.6 -> 1.5 -> 1.10 -> 1.11 -> 1.12

1.7 -> 1.8 -> 1.9 (em paralelo, junto com o fim da Fatia 0). A 1.6 vem antes da 1.5 porque destrava a 1.10.

1.13 e 1.14 são dívidas do servidor HTTP da 1.3, fora do backlog.

## Fatia 2 — Entrar

| Card | Item | Título | Tam. | Área | Depende de |
|---|---|---|---|---|---|
| #111 | 2.1 | Hash de senha com Argon2id | P | backend | — |
| #112 | 2.2 | POST /signup: cria usuário + tenant (com criado_por) + membership administrador na mesma transação | M | backend, banco | 2.1, 2.15 |
| #113 | 2.3 | POST /login com emissão de sessão | M | backend | 2.1, 2.14, 2.15 |
| #114 | 2.4 | POST /logout e invalidação | P | backend | 2.3, 2.5 |
| #115 | 2.5 | Middleware de autenticação | M | backend | 2.14, 2.3 |
| #116 | 2.6 | Middleware de tenant: valida membership e injeta app.tenant_id | G | backend, banco, **caminho crítico** | 2.5, 1.3 |
| #117 | 2.7 | Middleware de autorização: papéis administrador/prestador e Escopo por prestador | M | backend | 2.6, 1.5, 1.6 |
| #118 | 2.8 | Rate limit no endpoint de login | P | backend | 2.3 |
| #119 | 2.9 | GET /me com os tenants do usuário | P | backend | 2.3, 2.5 |
| #120 | 2.10 | Telas de cadastro e login | M | frontend | 2.2, 2.3, 1.9 |
| #121 | 2.11 | Layout autenticado com indicação do tenant ativo | M | frontend | 2.9, 2.10 |
| #122 | 2.12 | Remover TENANT_FIXO e PRESTADOR_FIXO do config | P | backend | 1.1, 2.6, 2.7 |
| #123 | 2.13 | Teste automatizado de isolamento entre tenants em todos os endpoints | M | backend | 2.2, 2.3, 2.6, 2.7, 2.12, 0.17 |
| #124 | 2.14 | Migration: sessoes | P | banco | 0.9 |
| #125 | 2.15 | Normalizar e-mail (trim + minúsculas) no cadastro e no login | P | backend | — |
| #126 | 2.16 | Teste: prestador não lê agendamento de outro prestador do mesmo tenant | M | backend | 2.2, 2.3, 2.7, 0.17 |

### Cards da Fatia 2 sem item no backlog

| Card | Item | Título | Tam. | Área | Depende de |
|---|---|---|---|---|---|
| #138 | — | Banco de produção no Neon | M | infra, banco | Fatia 0, 0.17 |
| #146 | — | Migrar o frontend de React para Angular | — | frontend | 1.8–1.12 |
| #148 | — | Migrar o backend de Go para .NET (EF Core) | — | backend | 1.1–1.7, 1.13, 1.14, 2.14 |
| #151 | — | Atualizar documentação para a stack .NET/Angular | — | docs | PRs #144 e #147 |
| #161 | — | Agenda semanal no design v2.1 | M | frontend | 1.6, #146 |
| #162 | — | Agendamento público: rotas por slug e criação atômica de cliente e agendamento | G | backend, banco, docs | 1.3, 1.5, 1.7, #148 |
| #163 | — | Página pública /agendar/:slug e novo agendamento no design v2.1 | G | frontend | #161, #162 |
| #165 | — | Proteger o fluxo de merge na main: CI em todo PR, build Docker e 1 aprovação | P | infra | — |

A 2.14 vem antes da 2.3: a sessão mora na tabela `sessoes`. A fatia fecha quando 2.13 e 2.16 passam no CI.

Sem decisão em aberto no corpo das issues das Fatias 2 e 2E: as de 2.2, 2.3, 2.8, 2.18, 2.19 e 2.22 foram decididas em 23/10/2026 (`requisitos.md` §12).

## Fatia P — Publicar

| Card | Item | Título | Tam. | Área | Depende de |
|---|---|---|---|---|---|
| #149 | — | Banco de produção no Neon na stack nova (EF Migrations e .NET) | M | infra, banco | #138, #148 |
| #168 | — | Cancelar e concluir agendamento no painel | G | backend, frontend | 2.6 (#116), 2.7 (#117) |
| #169 | — | Regras do agendamento público no backend | M | backend | #162 |
| #170 | — | Contato da empresa na página pública | M | banco, backend, frontend | #163 |
| #171 | — | Dados do agendamento no painel: telefone, serviço e profissional | M | backend, frontend | 2.6 (#116), 2.7 (#117) |
| #172 | — | Health check e monitoramento mínimo | M | infra, backend | — |
| #173 | — | Roteiro de deploy e migrations | M | infra, banco, docs | #149, #172 |
| #174 | — | Provisionar a empresa piloto | M | infra, banco | #149, #170, #173 |
| #175 | — | Privacidade da página pública | M | frontend, docs | #163, #170 |
| #176 | — | Acessibilidade do fluxo público | M | frontend | #163 |
| #177 | — | Ajustes da agenda: achados menores do PR #157 | M | frontend | #161 |
| #178 | — | Verificação em produção antes de divulgar | M | infra | #118, #120, #123, #126, #149, #163, #168 a #176 |
| #179 | — | Alinhar o design system às decisões do piloto | M | docs | — |
| #180 | — | Documentação de produto e API alinhada às decisões do piloto | M | docs | #162 |
| #181 | — | Agendamentos simultâneos no mesmo horário: deadlock responde 500 em vez de 409 | P | backend | #162 |
| #182 | — | Alinhar backlog e cards ao planejamento do piloto | M | docs | — |
| #198 | — | Achados do review do backend: erros 500, rate limit do cadastro, banco e testes | M | backend, banco | — |
| #199 | — | Achados do review do front: navegação, envio e acessibilidade | M | frontend | — |
| #200 | — | Docs alinhados ao código depois da migração e da Fatia 2 | P | docs | — |

Decisão em aberto no #174: administrador que não atende aparece como profissional na página pública, e falta confirmar se o horário da empresa piloto cabe na janela fixa.

## Fatia 2E — Equipe

Depois do piloto.

| Card | Item | Título | Tam. | Área | Depende de |
|---|---|---|---|---|---|
| #127 | 2.17 | Migration: convites (papel administrador ou prestador) | P | banco | 0.9 |
| #128 | 2.18 | POST e GET /convites: administrador convida administrador ou prestador | M | backend | 2.17 (#127), 2.7 (#117) |
| #129 | 2.19 | Aceite do convite (endpoint e tela), reativando membership removido | M | backend, frontend | 2.17 (#127), 2.5 (#115), 2.10 (#120), 2.22 (#136) |
| #130 | 2.20 | Tela de gestão de prestadores: listar membros e convidar | M | frontend | 2.18 (#128), 2.21 (#135), 2.11 (#121) |
| #135 | 2.21 | GET /membros: lista os membros do tenant (só administrador) | P | backend | 2.7 (#117) |
| #136 | 2.22 | Criar conta pelo convite: grava só usuarios + membership do convite | M | backend, frontend | 2.1 (#111), 2.3 (#113), 2.10 (#120), 2.15 (#125), 2.17 (#127) |
| #150 | — | Teste: handler fora de src/Api/Dados/ não acessa DbSet do AppDb | P | backend | #148 |

A 2.22 vem antes da 2.19: o aceite oferece "criar conta" a quem não tem conta.

**Sequência:** 2.17 (#127) e #150 -> 2.18 (#128), 2.21 (#135) e 2.22 (#136) -> 2.19 (#129) e 2.20 (#130).

## Fatias 3 e 4

Sem cards: congeladas até a reescrita para o WhatsApp ser fechada (`docs/backlog.md`).

## Ordem até o piloto

Os cards de uma mesma etapa podem andar em paralelo, respeitando o "Depende de".
O #163 depende de #161 e #162, que vêm antes da etapa 1.

| Etapa | Cards | O que fecha |
|---|---|---|
| 1 | #165, #163 | Fluxo de merge protegido e página pública ligada à API |
| 2 | #169, #172, #149 (em paralelo) | Regras do agendamento público, saúde da API e banco de produção |
| 3 | 2.3 (#113), 2.5 (#115), 2.6 (#116), 2.4 (#114), 2.9 (#119), 2.7 (#117) | Login, sessão, tenant e papéis |
| 4 | 2.2 (#112), 2.10 (#120), 2.11 (#121), 2.8 (#118), 2.12 (#122), 2.16 (#126), 2.13 (#123) | Cadastro, telas, fim dos valores fixos e testes de isolamento |
| 5 | #168, #171, #170, #175, #176 | Operação no painel, contato, privacidade e acessibilidade |
| 6 | #173, #174 | Roteiro de deploy e empresa piloto em produção |
| 7 | #178 | Verificação em produção antes de divulgar |

O #177 entra só se sobrar tempo. #179, #180, #181 e #182 foram criados depois da ordem aprovada e não têm etapa definida.

**Caminho crítico:** 2.3 -> 2.5 -> 2.6 -> 2.7 -> 2.12 -> 2.13 -> #178. Atraso na
2.6 ou na 2.7 atrasa o piloto, porque #168 e #171 também dependem delas.
