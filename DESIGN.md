# DESIGN.md · Dayspot

> Brief de design do Dayspot. Toda tela nova começa lendo este arquivo e o
> [`docs/design-system/README.md`](docs/design-system/README.md). Se este arquivo e o código
> discordarem, o código vale até o dono do produto decidir; a divergência entra em Perguntas abertas.
>
> Versão 2.1 · 17/11/2026 · fontes: guia de boas práticas da identidade (D aberto, paleta Fim de
> tarde), proposta de design v2 do novo front-end, [`docs/especificacao-fluxo.md`](docs/especificacao-fluxo.md)
> v1.1, [`docs/requisitos.md`](docs/requisitos.md) v0.10 e as 13 telas de referência em
> [`docs/design-system/telas/`](docs/design-system/telas/).

## Produto

Plataforma multi-tenant de agendamento para estabelecimentos pequenos (barbearia,
salão, manicure, clínica, estúdio). Cada empresa tem equipe, serviços, horários e
clientes isolados.

- **Painel (web, 1440 px primeiro):** administrador e prestador marcam, remarcam,
  cancelam e acompanham a semana numa grade; cadastram clientes, serviços,
  disponibilidade, equipe; veem o financeiro do mês.
- **Página do cliente (sem conta):** aberta pelo link da mensagem do WhatsApp; mostra os
  próprios horários com "Vou sim" / "Preciso cancelar". A grade é só do estabelecimento.
- **Avisos:** confirmação, lembrete e cancelamento vão pelo **WhatsApp**, por um job que
  chama a API. O Telegram saiu.
- **Trabalho principal da tela mais usada (Agenda):** ver o dia/semana de relance e marcar
  um horário livre em menos de 30 segundos.

## Público e voz

Donos e prestadores de salão de bairro, pouco técnicos, usando entre um atendimento e
outro, às vezes sob luz forte e com a mão molhada. Clientes finais de todas as idades.

Voz: a recepcionista boa de um salão de bairro. Clara, calorosa, confiável, sem pose.
Frase curta, verbo primeiro, "você". Diz o que aconteceu e depois o que fazer.
Português do Brasil, sem inglês na tela (horário, lembrete, falta; nunca slot, booking,
no-show). Sem emoji, sem exclamação, sem caixa alta, sem itálico.
Formatos: "quarta, 10h" em frase; `10:00` em grade e lista; datas "18 de novembro" /
"18 nov"; dinheiro `R$ 70,00` (tabela pode usar `R$ 70`). Canal sempre nomeado:
"pelo WhatsApp".

## Clima

Palavras: calma, precisa, acolhedora. Como uma grade de horários bem cuidada.
Nunca como: um dashboard SaaS genérico, um app "fofo" ou infantil, um salão feminino
cor-de-rosa, tela com degradê e brilho, mensagem de marketing ("nunca mais perca um
cliente"). Telas de trabalho são neutras; a cor aparece em ação, seleção e status.

## Marca

- **Símbolo:** D aberto com ponto pêssego no vão ("o horário marcado"). Sempre pelo SVG
  (`logo-horizontal.svg`, `simbolo*.svg`), nunca redigitado nem redesenhado. Abaixo de
  24 px, `simbolo-16px.svg`.
- **Cores:** ameixa `#86315C` (ação, link, seleção), pêssego `#F4A08A` (só preenchimento),
  grafite `#2B2227` (texto), papel `#FBF7F5`. Escuro: fundo `#1A1417`, ação `#EE9DBF`.
  Status com paleta própria, fora da marca. Valores em [`docs/design-system/tokens.css`](docs/design-system/tokens.css) (nomes `--ds-*`).
- **Fontes:** Nunito 700–800 só em título, wordmark e horário; Hanken Grotesk 400–700 em
  todo o resto.
- **Degradê do dia** (`#FFD9B0 → #F4A08A → #C9587E → #6E2A55`): só em peças de marca
  (Entrar, Cadastro, divulgação), nunca atrás de texto, nunca em tela de trabalho.
- **Ícones:** traço 2,25, pontas redondas, grade 24, um ponto cheio por ícone em ameixa.
- **Ilustrações de estado:** traço e preenchimento em cinza quente, ponto em ameixa.
- Sistema de design: [`docs/design-system/README.md`](docs/design-system/README.md). Tokens:
  [`docs/design-system/tokens.css`](docs/design-system/tokens.css). O código usa os mesmos tokens
  (`web/src/tokens.css`, carregado antes de `web/src/styles.css`); a migração está na seção 8 do README do sistema.

## Referências

As referências usadas até aqui são internas; nenhum produto externo foi escolhido como
referência. Pendente (ver Perguntas abertas).

- Página "Identidade Dayspot · D aberto" — tirar: regras de cor, contraste e uso do
  símbolo; evitar: degradê fora das peças de marca.
- Telas de referência v2.1 ([`docs/design-system/telas/`](docs/design-system/telas/), 13 telas,
  17/11/2026) — tirar: estrutura, medidas e componentes; os desvios da auditoria já foram
  corrigidos ([`docs/design-system/auditoria.md`](docs/design-system/auditoria.md)).
- Recepção de um salão de bairro (persona de voz) — tirar: o jeito de falar; evitar:
  cerimônia e jargão.

## Restrições

- **Web primeiro:** telas fechadas em 1440 px, conteúdo até 1240 px. Celular (390 px)
  vem depois, a partir das telas web.
- **Acessibilidade:** WCAG AA; só pares de cor da tabela 3.3 das boas práticas. Texto
  mínimo 13 px, corpo 16 px. Alvo mínimo 36 px no painel, 44 px em ação principal.
  Status sempre com ícone + palavra. Foco visível. Considerar luz forte (pares "com
  reflexo").
- **Temas:** claro e escuro, mesmos tokens; escuro sem sombra nem brilho.
- **Papéis:** o prestador não vê seletor de pessoa, Serviços, Prestadores nem
  Configurações. Esconder na tela não é autorização (RNF11).
- **Um conjunto de ícones** (os da marca). Faltou ícone, desenha-se seguindo a regra.
- **Movimento:** discreto; respeitar `prefers-reduced-motion`.
- **Stack:** Angular (standalone + signals), CSS com variáveis e `light-dark()`.

## Registro de decisões

- 2026-11-16 — Identidade D aberto, paleta Fim de tarde, Nunito + Hanken Grotesk — vale a
  página do D aberto quando os documentos discordarem.
- 2026-11-17 — Nova identidade substitui a direção "Grade" do `DESIGN.md` antigo do repo —
  decisão do dono do projeto.
- 2026-11-17 — Telegram sai; avisos pelo WhatsApp via job — decisão do dono do projeto.
- 2026-11-17 — Grade de agenda só para o estabelecimento; cliente vê lista dos próprios
  horários — o cliente não precisa de conta.
- 2026-11-17 — Fluxo de novo agendamento: cliente → serviço → (com quem) → dia → horário —
  aprovado pelo dono.
- 2026-11-17 — Web primeiro; versões de celular retiradas por enquanto — fechar o fluxo
  antes de adaptar.
- 2026-11-17 — Status na tela: Aguardando, Confirmado, Feito, Cancelado, Faltou, Erro;
  ameixa nunca é status — confunde ação com estado e falha em daltonismo.
- 2026-11-17 — Documentação do sistema (`DESIGN.md` + `docs/design-system/`) — para telas
  futuras saírem das mesmas peças.
- 2026-11-17 — Regras normalizadas pelo novo design (v2.1) — seguir o que está no novo
  front-end:
  - tokens com o prefixo `--ds-*` da `DESIGN-proposta` (status com `-texto` e `-chip`);
  - seleção é sempre ameixa, inclusive no filtro de pessoa e nas abas (antes grafite) — regra 3 do
    novo design;
  - destrutivo cheio, só no diálogo de confirmação, em `--ds-erro-texto` (como em Prestadores);
  - raios em três passos (12, 16, 24) e a pílula; campo, opção e dia vão para 16;
  - paddings na escala (22, 26 e 18 viram 24, 24 e 16);
  - botão carregando mantém a largura, com indicador; desabilitado é outro estado;
  - hover mais discreto que o selecionado; item ativo do menu sem trocar o peso;
  - iniciais do avatar com no mínimo 13 px;
  - bloco da agenda mostra linhas conforme a altura; ações secundárias do painel empilhadas.
- 2026-11-17 — Valores do escuro calculados e fixados (`#F4B4CF`, `#33222B`, `#A85A80`, véu) —
  todos passam AA.
- 2026-11-17 — Tokens v2.1 com fonte única num arquivo (`docs/design-system/tokens.css`, depois
  `web/src/tokens.css`); o menu deixa de ser ameixa sólido e o logo passa a ser sempre o SVG da
  marca — regra da identidade.
- 2026-11-17 — Código migrado para a v2.1 junto com a adaptação da Agenda: `web/src/tokens.css`,
  casca nova (menu em superfície, top-bar 64 px), Hanken Grotesk + Nunito, logo com os traços do SVG
  da marca. Status na grade: `confirmado` aparece como Aguardando até existir `presenca_confirmada_em`.

## Perguntas abertas

- **Referências externas:** há produtos para seguir (ou evitar) nas próximas telas?
- **Telas sem desenho:** escolha de empresa no login, aceite de convite, versões de celular.
- **Faltou e Aguardando × Confirmado** dependem de mudança no banco
  (`presenca_confirmada_em`, status de falta).
- **Telegram no `web/src/index.html`:** a descrição e o `og:description` ainda falam em Telegram.
