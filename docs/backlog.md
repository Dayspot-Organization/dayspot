# Backlog

## Organizado por fatias verticais — v2.6 · 18/11/2026

Complementa `docs/requisitos.md` e `docs/guia-banco-de-dados.md`.

> **Mudança desde a v1.0:** o backlog era fatiado horizontalmente (banco → auth → API → frontend → bot). Agora é fatiado **verticalmente**: cada fatia atravessa todas as camadas e termina em algo demonstrável. As issues são quase as mesmas; mudou o agrupamento, a ordem e o critério de conclusão de cada bloco.

> **Mudanças da v2.1:** voltaram cinco itens das revisões de arquitetura de 16 e 17/09 que a v2.0 não trazia — Postgres no CI (0.17), teste de exclusão de tenant (0.18), migration `sessoes` (2.14), normalização de e-mail (2.15) e `secret_token` no webhook (3.15). A decisão de sessão (cookie `HttpOnly`) já estava tomada e deixou de aparecer como pendente. Cronograma recalculado.

> **Mudanças da v2.2:** decisões de 02/10/2026 sobre papéis, agenda e financeiro (`requisitos.md` v0.8). Papéis administrador e prestador com escopo por prestador (2.7, teste 2.16); convites na Fatia 2 (2.17–2.20); agendamento nunca é apagado — a 5.1 perde o `DELETE` e ganha concluir; remoção de membro (5.8); tela de clientes na 3.8; preço nos serviços (6.1, 6.5a); fatia nova **7 Acompanhar** (calendário semanal, financeiro e `.ics`), e Entregar passa a ser a Fatia 8. Corte de escopo e cronograma recalculados (~474h).

> **Mudanças da v2.3:** revisão da Fatia 2 (23/10/2026). A 2.7 aplica o `Escopo` também na criação de agendamento e a 2.16 testa isso; a escolha de empresa no login ficou decidida e é da 2.3 (`requisitos.md` §12); itens novos 2.21 (`GET /membros`, para a 2.20) e 2.22 (criar conta pelo convite, para a 2.19). Cronograma recalculado (~481h).

> **Mudanças da v2.4:** o backend passou de Go para .NET com EF Core (#144) e o frontend de React para Angular (#147); o projeto se chama Dayspot (#143). Itens e decisões não mudam: só as referências técnicas (0.5, 0.7, 1.2, 1.8, 3.2, acesso a dados da Fatia 1 e período da Fatia 7).

> **Mudanças da v2.5:** o cliente final agenda sem conta pela página pública da empresa (#162, `requisitos.md` v0.14). A 6.3 e a 6.6 passam a citar a página pública.

> **Mudanças da v2.6:** a primeira publicação é um piloto com uma empresa. Fatias novas **2E Equipe** (convites, depois do piloto) e **P Publicar**, com a ordem até o piloto. O canal de avisos passa a ser o WhatsApp: T3, T4 e os itens 5.5 a 5.7 foram reescritos, ficam congelados e estão a reestimar. Cancelar e concluir (parte de 5.1 e 5.4) passam para o #168; 6.1 e 6.5a ficam fora do piloto, com serviços cadastrados por SQL (#174). Registrados como entregues a 7.1 (#161) e a 8.1 para a API (PR #159).

---

## Por que fatia vertical

No arranjo anterior, nada funcionava ponta a ponta até a semana ~15. Se aparecesse uma demonstração na semana 8, o time mostraria migrations e requisições no Postman. Além disso, todos os erros de integração apareceriam juntos, no fim, sem prazo para corrigir.

Fatiando vertical, existe sistema funcionando na semana ~8 e ele cresce em capacidade, não em camadas.

**O que isso custa** — e não é zero:

- **Algumas issues são tocadas duas vezes.** O endpoint de criar agendamento nasce na Fatia 1, ganha autenticação na Fatia 2, ganha cancelamento na Fatia 5 e cálculo de slots na Fatia 6. Isso é retrabalho real: ~2 a 4% de esforço a mais no total.
- **Exige disciplina de Git.** Fatia que vive três semanas em branch longa deixa de ser iteração e vira o mesmo waterfall com nome diferente. Toda fatia termina mergeada na `main`.
- **Tentação de declarar fatia pronta com stub.** A definição de pronto (fim do documento) importa mais aqui do que importava antes.

**O que não muda:** a Fatia 0 continua horizontal, e isso é deliberado. Schema, RLS e constraint de sobreposição precisam estar corretos antes de qualquer fatia vertical — retrofit de `tenant_id` e RLS em schema pronto é caro e é justamente a decisão que o grupo já tomou ao manter multi-tenant.

---

## Como usar

- Cada item vira **uma issue**.
- No GitHub Projects: a fatia é a label `fatia-N` (`fatia-2e` na T2E e `fatia-p` na TP); o tamanho é o campo `Effort` (Low = P ≈ 2h, Medium = M ≈ 5h, High = G ≈ 10h); a sprint é o campo `Iteration` (2 semanas).
- Título no formato `[T2] 2.3 POST /login com emissão de sessão` — prefixo da fatia + número do item.
- Uma fatia pode ocupar mais de uma sprint; o status de cada card fica só no GitHub Projects (a lista dos cards criados está em `docs/cards.md`).
- **Crie as issues de uma fatia por vez.** Backlog completo no dia 1 vira paisagem.

---

## Ordem até o piloto

A primeira publicação é um piloto com uma empresa. Os cards de uma mesma etapa podem andar em paralelo, respeitando o "Depende de" de cada issue.

| Etapa | Cards | O que fecha |
|-------|-------|-------------|
| 1 | #165, #163 | Fluxo de merge protegido e página pública ligada à API |
| 2 | #169, #172, #149 (em paralelo) | Regras do agendamento público, saúde da API e banco de produção |
| 3 | #113, #115, #116, #114, #119, #117 | Login, sessão, tenant e papéis |
| 4 | #112, #120, #121, #118, #122, #126, #123 | Cadastro, telas, fim dos valores fixos e testes de isolamento |
| 5 | #168, #171, #170, #175, #176 | Operação no painel, contato, privacidade e acessibilidade |
| 6 | #173, #174 | Roteiro de deploy e empresa piloto em produção |
| 7 | #178 | Verificação em produção antes de divulgar |

O #177 entra só se sobrar tempo.

**Fora do piloto:** T2E (convites), T3 e T4 (congeladas até a reescrita para o WhatsApp ser fechada), o restante da T5, 6.1 e 6.5a (serviços por SQL no piloto).

---

## FATIA 0 — Fundação · ~63h · sem entrega demonstrável

Único bloco horizontal do projeto. Ao final, o banco está correto e o CI funciona; não há nada para mostrar a um usuário, e tudo bem.

| #    | Issue | Tam. |
|------|-------|------|
| 0.1  | `[T0] Criar repositório, README e estrutura de pastas` | P |
| 0.2  | `[T0] Branch protection na main (1 aprovação obrigatória)` | P |
| 0.3  | `[T0] docker-compose com postgres:16-alpine` | P |
| 0.4  | `[T0] .env.example e carregamento de config na aplicação` | P |
| 0.5  | `[T0] GitHub Actions: dotnet format, dotnet build, dotnet test` | M |
| 0.6  | `[T0] Padrão de commit e template de PR` | P |
| 0.7  | `[T0] Configurar EF Core Migrations + migration 001 (extensões e app.current_tenant_id)` | P |
| 0.8  | `[T0] Migration: tenants` | M |
| 0.9  | `[T0] Migration: usuarios e memberships` | M |
| 0.10 | `[T0] Migration: clientes` | M |
| 0.11 | `[T0] Migration: servicos e disponibilidades` | M |
| 0.12 | `[T0] Migration: agendamentos + constraint de sobreposição + trigger` | M |
| 0.13 | `[T0] Migration: notificacoes (outbox) com UNIQUE(agendamento_id, tipo, versao)` | M |
| 0.14 | `[T0] Migration: RLS com FORCE, USING e WITH CHECK em todas as tabelas` | G |
| 0.15 | `[T0] Script de init: app_user sem BYPASSRLS e grants` | P |
| 0.16 | `[T0] Seed com dois tenants fictícios e UUIDs fixos` | P |
| 0.17 | `[T0] Subir Postgres no CI para os testes de banco` | M |
| 0.18 | `[T0] Teste: exclusão de tenant não quebra em chave estrangeira` | P |

O carregamento de config da 0.4 passou para a 1.1.

**Critério de conclusão da fatia:** o checklist da Fatia 0 no `guia-banco-de-dados.md` passa inteiro — incluindo o item que quase todo time esquece, que é confirmar que agendamentos consecutivos (9-10h e 10-11h) **funcionam**.

> **0.17 destrava o resto.** Sem Postgres no CI, o teste de isolamento da 0.14 e os testes de repositório da 1.2 e 1.3 só rodam na máquina de quem escreveu. Pode entrar assim que a 0.7 for mergeada.

> **Ordem:** 0.7 → 0.8 → 0.9 → 0.10 → 0.11 → 0.12 → 0.13 → 0.15 → 0.16 → 0.14 → 0.18, com a 0.17 em paralelo assim que a 0.7 entrar. A 0.16 vem antes da 0.14 porque o teste de isolamento precisa dos dois tenants do seed; a 0.18 vem depois da 0.13 para cobrir `notificacoes` na cadeia de `ON DELETE`.

> **0.14 é a issue mais importante do projeto.** Não atribua a quem está aprendendo Postgres agora.

---

## FATIA 1 — Agendar · ~46h · 🎯 primeira demonstração

**Entrega:** abrir a aplicação no navegador, criar um agendamento e vê-lo na lista. Sem login: tenant e prestador vêm de constante no config.

| #    | Issue | Tam. |
|------|-------|------|
| 1.1  | `[T1] Config com TENANT_FIXO e PRESTADOR_FIXO (temporário, remover na T2)` | P |
| 1.2  | `[T1] Camada de repositório: toda consulta recebe tenantId logo depois do AppDb` | M |
| 1.3  | `[T1] Abrir transação por requisição com set_config('app.tenant_id', ...)` | M |
| 1.4  | `[T1] CRUD mínimo de clientes (criar e listar)` | M |
| 1.5  | `[T1] POST /agendamentos traduzindo SQLSTATE 23P01 para HTTP 409` | M |
| 1.6  | `[T1] GET /agendamentos com filtro por período` | M |
| 1.7  | `[T1] Padronizar formato de resposta de erro da API` | P |
| 1.8  | `[T1] Setup Angular + TypeScript + roteamento` | P |
| 1.9  | `[T1] Cliente HTTP com tratamento centralizado de erro` | M |
| 1.10 | `[T1] Tela de listagem de agendamentos` | M |
| 1.11 | `[T1] Formulário de novo agendamento (data e hora digitadas, sem slots)` | M |
| 1.12 | `[T1] Estados de carregamento e exibição de erro` | M |

**Por que sem login já na primeira fatia:** autenticação é ~55h. Esperá-la para ver a primeira tela empurraria a demonstração inicial para a semana ~14. O RLS já está ativo por baixo desde a Fatia 0 — o `tenant_id` só vem de constante em vez de vir da sessão, então não se cria dívida de isolamento, apenas uma linha a remover na Fatia 2.

> **Cuidado:** 1.3 precisa estar certo desde já. Se alguém usar `SET` em vez de `SET LOCAL` / `set_config(..., true)`, o bug fica dormindo até existir concorrência real e vira vazamento entre tenants.

**Acesso a dados:** EF Core + Npgsql, só mapeando o schema (decidido em 15/11/2026, no lugar de pgx v5 + sqlc). As consultas da 1.2 ficam em `src/Api/Dados/Consultas.cs`, como métodos de extensão do `AppDb`; a 1.3 usa o helper `AppDb.IniciarComTenant` do `guia-banco-de-dados.md` (§Acesso a dados) para abrir a transação da requisição.

**Paralelismo:** 1.7, 1.8 e 1.9 não dependem do banco e podem andar junto com o fim da Fatia 0.

**Desde a v2.2:** a 1.5 grava `valor_centavos` copiado do serviço e só aceita prestador com membership ativo; a 1.6 aceita `prestador_id` opcional, sem regra de sessão (restringir ao prestador da sessão é da 2.7); a lista da 1.10 foi substituída pela grade semanal da 7.1 (#161).

---

## FATIA 2 — Entrar · ~66h · 🎯 multi-tenant real

**Entrega:** dois usuários de tenants diferentes fazem login e cada um vê apenas a própria agenda; dentro da empresa, o prestador vê só os seus agendamentos.

| #    | Issue | Tam. |
|------|-------|------|
| 2.1  | `[T2] Hash de senha com Argon2id` | P |
| 2.2  | `[T2] POST /signup: cria usuário + tenant (com criado_por) + membership administrador na mesma transação` | M |
| 2.3  | `[T2] POST /login com emissão de sessão` | M |
| 2.4  | `[T2] POST /logout e invalidação` | P |
| 2.5  | `[T2] Middleware de autenticação` | M |
| 2.6  | `[T2] Middleware de tenant: valida membership e injeta app.tenant_id` | G |
| 2.7  | `[T2] Middleware de autorização: papéis administrador/prestador e Escopo por prestador` | M |
| 2.8  | `[T2] Rate limit no endpoint de login` | P |
| 2.9  | `[T2] GET /me com os tenants do usuário` | P |
| 2.10 | `[T2] Telas de cadastro e login` | M |
| 2.11 | `[T2] Layout autenticado com indicação do tenant ativo` | M |
| 2.12 | `[T2] Remover TENANT_FIXO e PRESTADOR_FIXO do config` | P |
| 2.13 | `[T2] Teste automatizado de isolamento entre tenants em todos os endpoints` | M |
| 2.14 | `[T2] Migration: sessoes` | P |
| 2.15 | `[T2] Normalizar e-mail (trim + minúsculas) no cadastro e no login` | P |
| 2.16 | `[T2] Teste: prestador não lê agendamento de outro prestador do mesmo tenant` | M |
| 2.17 | `[T2] Migration: convites (papel administrador ou prestador)` | P |

Os itens 2.18 a 2.22 passaram para a Fatia 2E. A 2.17 (#127) continua na T2 no board, mas só a T2E depende dela: não entra na ordem até o piloto. Os cards da fatia sem número de item (#138, #146, #148, #151, #161, #162, #163 e #165) estão em `docs/cards.md`.

**Sessão já decidida:** cookie `HttpOnly` + `Secure` + `SameSite=Lax` com a tabela `sessoes` (`requisitos.md` §9). Por isso a 2.14 vem antes da 2.3.

**Empresa no login (decidido em 23/10/2026, `requisitos.md` §12):** login só com e-mail e senha. Com uma membership ativa, a sessão nasce na empresa; com várias, nasce com `tenant_id` nulo e o usuário escolhe na tela da 2.10, que grava a empresa pelo slug (`POST /api/sessao/empresa`, 2.3). Até escolher, a sessão só acessa `/me`, logout e a escolha (2.6).

**Papéis decididos (02/10/2026):** `administrador` e `prestador`; o administrador também pode atender. O escopo por prestador é filtro na query, sem policy extra no RLS. A 2.7 monta o `Escopo` (`guia-banco-de-dados.md` §Acesso a dados): para o prestador, sempre o da sessão (`prestador_id` de outro → 403 `sem_permissao`); para o administrador, o do seletor ou nenhum. O handler nunca monta o filtro. Rota só de administrador responde 403 ao prestador. Vale também na criação (`POST /agendamentos`, 1.5): o prestador só agenda para si, e o administrador para qualquer membro ativo.

> **2.16 é a 2.13 dentro da empresa.** A fixture cria os usuários via `/signup` e insere a 2ª membership direto no banco, pela conexão do dono das tabelas — sem depender do seed nem dos convites. Cobre a listagem e a criação: o prestador recebe só os próprios agendamentos, `prestador_id` de outro dá 403 (inclusive no `POST`) e agendamento de outro tenant nunca aparece. PATCH nasce na 5.1 e cancelar e concluir no #168; os dois repetem o critério (403 no mesmo tenant, 404 em outro).

**Critério de conclusão:** 2.13 e 2.16 passam no CI. Sem esses testes verdes, o isolamento é suposição, não garantia — e as fatias seguintes vão construir por cima dele.

---

## FATIA P — Publicar · ~65h · 🎯 piloto com uma empresa

**Entrega:** a empresa piloto opera em produção: o cliente final agenda pela página pública e a empresa atende, cancela e conclui pelo painel.

| Card | Issue | Tam. |
|------|-------|------|
| #149 | `[TP] Banco de produção no Neon na stack nova (EF Migrations e .NET)` | M |
| #168 | `[TP] Cancelar e concluir agendamento no painel` | G |
| #169 | `[TP] Regras do agendamento público no backend` | M |
| #170 | `[TP] Contato da empresa na página pública` | M |
| #171 | `[TP] Dados do agendamento no painel: telefone, serviço e profissional` | M |
| #172 | `[TP] Health check e monitoramento mínimo` | M |
| #173 | `[TP] Roteiro de deploy e migrations` | M |
| #174 | `[TP] Provisionar a empresa piloto` | M |
| #175 | `[TP] Privacidade da página pública` | M |
| #176 | `[TP] Acessibilidade do fluxo público` | M |
| #177 | `[TP] Ajustes da agenda: achados menores do PR #157` (cortável) | M |
| #178 | `[TP] Verificação em produção antes de divulgar` | M |

As ~65h somam os 12 cards acima; sem o #177, ~60h. A lista completa, com cards criados depois, fica em `docs/cards.md`.

**Decisões do piloto:** uma empresa; serviços cadastrados por SQL (#174), com 6.1 e 6.5a fora do piloto; convites depois do piloto; cancelar não gera aviso ao cliente enquanto o canal do WhatsApp não existir (#168).

> **Decisão em aberto (#174).** A página pública lista como profissional todo membership ativo, então um administrador que não atende apareceria para o cliente. A janela pública é fixa, de segunda a sábado, das 08:00 às 19:00: falta confirmar se o horário real da empresa piloto cabe nela.

**Critério de conclusão:** roteiro do #178 executado em produção, com cada achado corrigido ou aceito pelo time.

---

## FATIA 2E — Equipe · ~24h · depois do piloto

**Entrega:** o administrador convida novos membros, e o convidado entra na empresa pelo link do convite.

| #    | Issue | Tam. |
|------|-------|------|
| 2.18 | `[T2E] POST e GET /convites: administrador convida administrador ou prestador` | M |
| 2.19 | `[T2E] Aceite do convite (endpoint e tela), reativando membership removido` | M |
| 2.20 | `[T2E] Tela de gestão de prestadores: listar membros e convidar` | M |
| 2.21 | `[T2E] GET /membros: lista os membros do tenant (só administrador)` | P |
| 2.22 | `[T2E] Criar conta pelo convite: grava só usuarios + membership do convite` | M |
| #150 | `[T2E] Teste: handler fora de src/Api/Dados/ não acessa DbSet do AppDb` | P |

Os itens mantêm a numeração 2.x. A migration de convites (2.17, #127) é pré-requisito de 2.18, 2.19 e 2.22.

> **Convites (2.17–2.22).** Administrador convida administrador ou prestador. Convite para e-mail que já tem membership ativo, expirado ou já aceito → 409 `convite_invalido`; token inexistente → 404. O aceite exige sessão com o e-mail do convite e reativa membership removido (`guia-banco-de-dados.md` §011). Convidado sem conta cria a conta pelo convite (2.22): grava só `usuarios` e a membership do convite, sem empresa nova. A 2.20 lista os membros pelo `GET /membros` (2.21). A remoção de membro fica na 5.8, porque depende do cancelamento da 5.2.

---

## FATIA 3 — Avisar · a reestimar · congelada

**Entrega:** criar um agendamento e o cliente receber a confirmação no WhatsApp. Envio imediato, sem agendador — o que valida o canal inteiro sem depender do worker temporizado.

> **Congelada até a reescrita para o WhatsApp ser fechada.** O canal de avisos é o WhatsApp; o Telegram saiu. Os itens abaixo são a primeira versão da reescrita: nenhum card desta fatia ou da T4 deve ser criado antes das decisões pendentes. Tamanhos, custo e prazo ficam a reestimar.

**Decisões pendentes:**

- Fornecedor de acesso ao WhatsApp.
- Como o cliente dá o consentimento para receber avisos.
- Modelos de mensagem, regras de envio, limites e custo, que dependem do fornecedor.
- Quais colunas do Telegram saem do schema (`tenants.telegram_bot_token`, `clientes.telegram_chat_id`, `clientes.vinculo_token`, `clientes.vinculado_em` e `notificacoes.telegram_message_id`).

| #    | Issue | Tam. |
|------|-------|------|
| 3.1  | `[T3] Configurar a conta do fornecedor do WhatsApp e documentar as credenciais no .env.example` | a reestimar |
| 3.2  | `[T3] Camada de acesso à API do fornecedor do WhatsApp em C#` | a reestimar |
| 3.3  | `[T3] Modo de desenvolvimento local sem envio real pelo WhatsApp` | a reestimar |
| 3.4  | `[T3] Endpoint de webhook para receber eventos do WhatsApp` | a reestimar |
| 3.5  | `[T3] Migration: trocar as colunas do Telegram pelos dados do canal WhatsApp` | a reestimar |
| 3.6  | `[T3] Registrar o consentimento do cliente para receber avisos no WhatsApp` | a reestimar |
| 3.7  | `[T3] Tratar telefone inválido ou sem WhatsApp antes do envio` | a reestimar |
| 3.8  | `[T3] Tela de clientes (listar e criar) com a situação do WhatsApp` | a reestimar |
| 3.9  | `[T3] Indicador visual de cliente sem consentimento ou inalcançável no WhatsApp` | a reestimar |
| 3.10 | `[T3] Definir interface Notificador (sem mencionar o fornecedor)` | a reestimar |
| 3.11 | `[T3] Implementar o notificador do WhatsApp sobre a interface` | a reestimar |
| 3.12 | `[T3] Estrutura do worker: loop periódico, encerramento gracioso e Dockerfile multi-stage` | a reestimar |
| 3.13 | `[T3] Enfileirar notificação de confirmação na transação de criação` | a reestimar |
| 3.14 | `[T3] Montar texto das mensagens nos modelos aceitos pelo WhatsApp` | a reestimar |
| 3.15 | `[T3] Autenticar o webhook do fornecedor do WhatsApp (RNF10)` | a reestimar |

**Se o prazo apertar,** a fatia pode quebrar em duas: canal (3.1–3.9 e 3.15) e envio (3.10–3.14).

> **3.12 inclui a imagem do worker.** A 8.1 entregou só o Dockerfile da API (PR #159), porque o worker ainda não existe.

> **3.15 entra junto com a 3.4.** O endpoint é público: sem autenticar a origem, aceita eventos forjados por qualquer um. O mecanismo depende do fornecedor.

> **3.8 cria a tela de clientes.** A 1.4 é só backend; sem tela, a situação do WhatsApp não tem onde aparecer.

> **3.9 não é cosmético.** Sem ele, o prestador acha que o lembrete foi enviado quando o cliente sequer é alcançável. Precisa distinguir "sem consentimento" de "número inalcançável" — são situações diferentes.

---

## FATIA 4 — Lembrar · a reestimar · congelada

**Entrega:** lembrete chega sozinho pelo WhatsApp X horas antes do agendamento, sem ninguém apertar nada.

Congelada junto com a T3: depende do canal e das regras de envio do fornecedor.

| #   | Issue | Tam. |
|-----|-------|------|
| 4.1 | `[T4] Enfileirar lembrete com agendar_para = inicio - antecedencia` | a reestimar |
| 4.2 | `[T4] Consumir fila outbox com FOR UPDATE SKIP LOCKED` | a reestimar |
| 4.3 | `[T4] Retry com backoff usando proxima_tentativa e tentativas` | a reestimar |
| 4.4 | `[T4] Respeitar os limites de envio do fornecedor do WhatsApp` | a reestimar |
| 4.5 | `[T4] Tratar número inalcançável ou recusa do cliente marcando notificavel = false` | a reestimar |
| 4.6 | `[T4] Antecedência do lembrete configurável por tenant` | a reestimar |
| 4.7 | `[T4] Teste: duas instâncias do worker não enviam a mesma notificação` | a reestimar |

**Critério de conclusão:** 4.7 verde. Rodar dois workers em paralelo contra o mesmo banco produz exatamente um envio por agendamento.

**Truque para testar sem esperar horas:** deixe a antecedência configurável (4.6) e coloque em 1 minuto no ambiente de desenvolvimento. Sem isso, cada teste manual custa uma tarde.

---

## FATIA 5 — Cancelar · a reestimar · 🎯 ciclo de vida completo

**Entrega:** reagendar pelo sistema, cancelar pelo botão da mensagem do WhatsApp e remover um membro, com as notificações certas em cada caso.

| #   | Issue | Tam. |
|-----|-------|------|
| 5.1 | `[T5] PATCH /agendamentos/{id} (remarcar, trocar serviço), sem DELETE` | a reestimar |
| 5.2 | `[T5] Enfileirar cancelamento e descartar lembrete e confirmação pendentes na mesma transação` | M |
| 5.3 | `[T5] Reagendamento: descartar lembrete antigo e enfileirar versao + 1` | M |
| 5.4 | `[T5] Ação de reagendar no frontend` | a reestimar |
| 5.5 | `[T5] Botões de Confirmar e Cancelar na mensagem do WhatsApp` | a reestimar |
| 5.6 | `[T5] Receber a resposta do botão pelo webhook e atualizar o status` | a reestimar |
| 5.7 | `[T5] Responder ao cliente no WhatsApp confirmando a ação registrada` | a reestimar |
| 5.8 | `[T5] Remover membro: cancelar em lote os agendamentos futuros e avisar os clientes` | G |

> **Recorte para o piloto (#168).** `POST .../cancelar` e `POST .../concluir` saíram da 5.1, e as ações de cancelar e concluir no painel saíram da 5.4. Remarcar e trocar serviço continuam aqui. O aviso de cancelamento continua na 5.2, que depende da T3.

> **5.2 é a issue que evita o bug mais constrangedor possível:** o cliente recebe "seu agendamento foi cancelado" e, horas depois, "lembrete do seu agendamento".

> **5.1 e #168, regras de cada operação.** Remarcar e trocar serviço só em `confirmado`; concluir só com `inicio <= now()` (relógio do banco); cancelar enquanto `confirmado`; `cancelado` e `concluido` são finais. Remarcar mantém valor e duração (`fim = novo inicio + (fim − inicio)`); trocar o serviço (só para serviço ativo) recalcula o `fim` e recopia o valor. Cada operação trava a linha e responde 404 (outro tenant), 403 `sem_permissao` (agendamento de outro prestador, para quem não é administrador), 409 `status_invalido` (transição inválida) e 409 `conflito_horario` (o PATCH dispara a constraint de novo, SQLSTATE `23P01`). Não existe `DELETE`: o `app_user` nem tem o privilégio (`guia-banco-de-dados.md` §007, §009 e §Acesso a dados).

> **Concluir está no #168.** Sem ele, nenhum agendamento chega a `concluido` e o financeiro (Fatia 7) fica sempre zerado.

> **Botão Confirmar (5.5–5.7).** O agendamento já nasce `confirmado`: o Confirmar só registra a confirmação de presença e responde ao cliente (5.7), sem mudar status. Só o Cancelar muda o status, pelo caso de uso da 5.2. Decidido em 02/10/2026. Os itens dependem da T3 e dos recursos de mensagem do fornecedor do WhatsApp, ainda não decidido.

> **5.8, remoção de membro.** Vale para qualquer membership, inclusive administrador que atende. Na mesma transação: grava `removido_em` e cancela cada agendamento futuro do removido (`inicio > now()`) pelo caso de uso da 5.2 — descarta lembrete e confirmação pendentes e enfileira o aviso de cancelamento. Agendamentos passados ainda `confirmado` ficam para o administrador concluir pela agenda. Ninguém remove o criador da empresa; só o criador remove outro administrador; administrador que não é o criador remove só prestadores — o resto dá 403 `sem_permissao`. O botão de remover fica na tela de gestão (2.20).

---

## FATIA 6 — Configurar · ~50h · 🎯 autonomia do prestador

**Entrega:** o administrador cadastra os serviços, com preço, e os horários de cada prestador (o prestador cadastra os próprios); o formulário passa a oferecer slots calculados em vez de campo de hora livre.

| #    | Issue | Tam. |
|------|-------|------|
| 6.1  | `[T6] CRUD de servicos com preço (só administrador)` | M |
| 6.2  | `[T6] CRUD de disponibilidades` | M |
| 6.3  | `[T6] GET /horarios-livres com cálculo de slots` | G |
| 6.4  | `[T6] Fuso horário por tenant e conversão na borda` | M |
| 6.5a | `[T6] Telas de cadastro de serviços e preços` | M |
| 6.5b | `[T6] Telas de cadastro de disponibilidades` | M |
| 6.6  | `[T6] Substituir campo de hora livre por seleção de slots` | M |
| 6.7  | `[T6] Consultar os próprios agendamentos pelo WhatsApp` | a reestimar |
| 6.8  | `[T6] Log de mensagens enviadas visível no frontend` | M |

> **Antecipado em parte na página pública (#162 e #163).** 6.3: `GET /horarios-livres` continua previsto para o painel; a página pública lê `GET /api/publico/{slug}/ocupados`, que não calcula slots, e o `POST` público aceita a janela fixa de segunda a sábado, das 08:00 às 19:00, até existir o cadastro de disponibilidade. 6.4: a página pública já mostra e envia dia e hora no fuso da empresa (`tenants.fuso_horario`); o painel continua no relógio do navegador. 6.6: a página pública já oferece horários em grade e grava o serviço e o profissional escolhidos; a seleção de slots ainda troca o campo de hora livre do painel, e `POST /api/agendamentos` segue com `SERVICO_FIXO` até lá.

> **Fora do piloto: 6.1 e 6.5a.** Os serviços da empresa piloto são cadastrados por SQL (#174). Os dois itens continuam nesta fatia.

**Fatia mais cortável do projeto.** Até aqui o sistema já agenda, autentica, isola tenants, avisa, lembra e cancela. Disponibilidades podem viver no seed até o fim do semestre sem prejudicar a demonstração.

> **6.4 é a exceção — não corte.** Fuso errado significa lembrete na hora errada, e isso invalida a Fatia 4 inteira.

> **6.1 e 6.5a também ficam.** Sem preço, o financeiro (Fatia 7) não tem valor. Só o administrador gerencia serviços e preços, e a API exige o preço (a coluna não tem `DEFAULT`). A 6.5 foi dividida em 6.5a (serviços e preços, protegida) e 6.5b (disponibilidades, cortável). Decidido em 02/10/2026.

> **Escopo e membership ativo.** O administrador edita a disponibilidade de qualquer prestador, escolhendo-o como no seletor da agenda; o prestador, só a própria (decidido em 02/10/2026). A 6.3 só oferece slots de membership ativo (`JOIN memberships ... removido_em IS NULL`); as disponibilidades de quem foi removido ficam no banco. 6.3 e 6.8 seguem o `Escopo` da 2.7: prestador que pede `prestador_id` de outro recebe 403.

---

## FATIA 7 — Acompanhar · ~37h · 🎯 agenda e financeiro

**Entrega:** a agenda vira calendário semanal; administrador e prestador veem o resumo financeiro do mês e exportam o período para o calendário do celular.

Depende do #168 (concluir) e da 6.1 (preço): sem os dois, o financeiro sai zerado.

| #   | Issue | Tam. |
|-----|-------|------|
| 7.1 | `[T7] Calendário semanal: eixo de horas, navegação entre semanas e cor por status` | G |
| 7.2 | `[T7] Seletor de prestador no calendário (administrador), com removidos marcados` | M |
| 7.3 | `[T7] GET /financeiro: resumo do mês no fuso do tenant (agendamentos concluídos)` | M |
| 7.4 | `[T7] Financeiro por prestador para o administrador, com removidos marcados` | M |
| 7.5 | `[T7] Tela do resumo financeiro mensal` | M |
| 7.6 | `[T7] Exportar .ics do período do calendário (download)` | M |
| 7.7 | `[T7] Botão de exportar .ics no calendário` | P |

**7.1 entregue pelo #161 (PR #157):** a grade semanal substituiu a lista da 1.10 e consome a 1.6 com o período da semana.

> **Período no fuso do tenant.** Semana e mês contam pelo `inicio`, no fuso do tenant (RNF01): limites calculados no C# e passados como `timestamptz` (`guia-banco-de-dados.md` §Acesso a dados). O resumo soma `valor_centavos` dos `concluido`: para o prestador, os próprios; para o administrador, o total da empresa e, na 7.4, por prestador.

> **`.ics` (7.6).** Download, não feed: é uma fotografia do período e não sincroniza. Mesmo escopo e mesmo período do calendário; só `confirmado` e `concluido` (cancelados ficam fora). `METHOD:PUBLISH`; por evento, `UID:<id>@dayspot`, `DTSTAMP` = agora, `SEQUENCE` = epoch de `atualizado_em` (reimportar substitui o evento remarcado) e `DTSTART`/`DTEND` em UTC (`Z`). Rota na mesma origem, com o cookie da sessão; sem token na URL.

**Se faltar tempo, corte nesta ordem:** `.ics` (7.6, 7.7) → financeiro por prestador (7.4) → seletor de prestador (7.2). Decidido em 02/10/2026; a 7.1 saiu da lista porque já foi entregue.

---

## FATIA 8 — Entregar · ~32h

| #   | Issue | Tam. |
|-----|-------|------|
| 8.1 | `[T8] Dockerfile multi-stage da API` | M |
| 8.2 | `[T8] docker-compose de produção` | M |
| 8.3 | `[T8] Executar migrations no deploy` | P |
| 8.4 | `[T8] HTTPS e webhook público do WhatsApp` | a reestimar |
| 8.5 | `[T8] README com instruções de setup local` | M |
| 8.6 | `[T8] Documentação da API` | M |
| 8.7 | `[T8] Roteiro de apresentação e demonstração` | M |

Era a Fatia 7 até a v2.1; nenhum card dela tinha sido criado.

**8.1 entregue para a API pelo PR #159.** O Dockerfile do worker passou para a 3.12.

> **8.2 e 8.3 a rever.** O piloto publica no Render, com o Postgres no Neon e as migrations aplicadas fora do deploy (#149 e #173). Os dois itens ainda descrevem o plano anterior.

---

## Cronograma

A 15h/semana nominais, com ~70% de eficiência real (~10,5h/semana):

| Fatia        | Esforço  | Nominal     | Realista    |
|--------------|----------|-------------|-------------|
| 0 Fundação   | 63h      | sem. 1–5    | sem. 1–6    |
| 1 Agendar    | 46h      | sem. 6–8    | sem. 7–11   |
| 2 Entrar     | 66h      | a reestimar | a reestimar |
| P Publicar   | ~65h     | a reestimar | a reestimar |
| 2E Equipe    | 24h      | a reestimar | a reestimar |
| 3 Avisar     | a reestimar | a reestimar | a reestimar |
| 4 Lembrar    | a reestimar | a reestimar | a reestimar |
| 5 Cancelar   | a reestimar | a reestimar | a reestimar |
| 6 Configurar | 50h      | a reestimar | a reestimar |
| 7 Acompanhar | 37h      | a reestimar | a reestimar |
| 8 Entregar   | 32h      | a reestimar | a reestimar |
| **Total**    | **a reestimar** | **a reestimar** | **a reestimar** |

Na v2.6, a T2 perde 22h para a T2E (2.18 a 2.22), que soma o #150 (+2h). As horas de T6, T7 e T8 são as da v2.5: não descontam o que já foi antecipado ou entregue (6.3, 6.4, 6.6, 7.1 e 8.1) nem reestimam 6.7 e 8.4, que dependem do WhatsApp. O total de 481h e as semanas das versões anteriores deixam de valer até o fornecedor do WhatsApp ser decidido.

O total subiu de 380h para 384h com o retrabalho de fatiar vertical, e para 397h com os itens reincorporados na v2.1. Em troca, a primeira demonstração sai na semana ~8 em vez da semana ~15.

Na v2.2, para 474h: escopo por prestador e convites na Fatia 2 (2.16–2.20, +22h), tela de clientes na 3.8 (P → M, +3h), concluir e trocar serviço na 5.1 (M → G, +5h), remoção de membro na 5.8 (+10h) e a Fatia 7 (+37h). A 6.5 dividida (G → 6.5a M + 6.5b M) não muda o total.

Na v2.3, para 481h: `GET /membros` (2.21, +2h) e criar conta pelo convite (2.22, +5h).

**Paralelismo:** as Fatias 0 e 1 quase não paralelizam — trabalho serial com três pessoas em cima. A exceção é o frontend base (1.7–1.9), que não depende do banco. A partir da Fatia 3 abrem duas ou três frentes (canal / worker / frontend).

---

## Corte de escopo, em ordem

1. Fatia 6 (deixar disponibilidades no seed) — exceto 6.1, 6.4 e 6.5a
2. 5.5, 5.6, 5.7 (botões na mensagem do WhatsApp; cancelar só pelo sistema)
3. 6.8 (log de mensagens)
4. 7.6 e 7.7 (`.ics`)
5. 7.4 (financeiro por prestador)
6. 7.2 (seletor de prestador no calendário) — último recurso: RF14 é protegido e só sai depois de todo o resto

**Fora do corte (decidido em 02/10/2026):** RF12 (convites e remoção: 2.17–2.22 e 5.8), 5.3 (reagendar), 6.1 e 6.5a (serviços e preços). Ficar fora do piloto não é corte: convites, 6.1 e 6.5a entram depois da primeira publicação.

**Nunca corte:** 0.12, 0.14, 0.17, 1.3, 2.6, 2.13, 2.16, 3.15, 4.2, 4.7, 6.1 e 6.4. São as issues que separam um sistema funcional de um que vaza dados entre clientes ou entre prestadores, aceita comando forjado, manda o mesmo lembrete três vezes, avisa na hora errada ou fecha o mês sem preço.

---

## Definição de pronto

**Issue:**

- [ ] Mergeada na `main`
- [ ] PR aprovado por outro integrante
- [ ] CI verde
- [ ] Critérios de aceite verificados
- [ ] Alteração de banco só via migration versionada

**Fatia:**

- [ ] Todas as issues fechadas
- [ ] O cenário de entrega funciona ponta a ponta em ambiente limpo (`docker compose up` a partir do zero)
- [ ] Alguém que não escreveu o código conseguiu executar o cenário

O último item é o que impede fatia declarada pronta com stub. Se só quem escreveu consegue rodar, a fatia não acabou.
