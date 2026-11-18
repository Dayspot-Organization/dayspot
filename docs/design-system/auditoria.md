# Auditoria do design · telas de referência e `web/src`

Relatório de 17/11/2026, com o status depois das correções (v2.1): as regras foram normalizadas
pelo novo design e todos os achados foram tratados.
Conferido de novo depois: nenhum hex nem rgba solto nas 13 telas, nenhum texto abaixo de 13 px,
raios só 12 / 16 / 24 / pílula (4 nas marcas do gráfico e 50 % no avatar).

| # | Gravidade | Achado | Status | Onde mudou |
|---|---|---|---|---|
| 1 | Alta | Iniciais do avatar com 11 e 12 px | **Corrigido**: 13 px | Agenda, Financeiro, Disponibilidade |
| 2 | Alta | Tokens do código na v1 (borda de campo 1,4:1, menu ameixa, Nunito em tudo) | **Corrigido**: tokens em `web/src/tokens.css`, menu em superfície, Hanken + Nunito, logo em SVG | `web/src/tokens.css`, `styles.css`, `app/layout.ts`, `index.html`, `public/logo-horizontal*.svg` |
| 3 | Alta | Foco e desabilitado sem desenho na maioria dos controles | **Corrigido**: regras de hover, foco (só teclado), desabilitado e carregando no CSS das 13 telas; nova seção "Estados de interação" em `telas/Main.dc.html` | 13 telas |
| 4 | Alta | Carregando = desabilitado com o texto trocado ("Salvando…") | **Corrigido**: mantém a largura, indicador por cima, `aria-busy`; desabilitado é outro estado | `Main`; CSS das 13 telas |
| 5 | Média | Hex soltos em todas as telas | **Corrigido**: 100 % `var(--ds-*)`, inclusive SVG e gráfico; a seção escura de `Main` usa `data-tema="escuro"` com os mesmos tokens | 13 telas |
| 6 | Média | Seleção em grafite no filtro de pessoa e nas abas | **Corrigido**: ameixa (`--ds-acao` + `--ds-acao-texto`), avatar dentro em superfície | Agenda, Financeiro, Disponibilidade |
| 7 | Média | Item ativo do menu trocava o peso (600 → 700) e hover = ativo | **Corrigido**: mesmo peso; hover em `--ds-fundo`, ativo em `--ds-hover` + ameixa | Telas com menu; código |
| 8 | Média | Quase-duplicados `#FDFAF9` e `#FBF4F7` | **Corrigido**: viraram `--ds-fundo` e o token novo `--ds-acao-suave` | Agenda, Clientes, Novo agendamento |
| 9 | Média | Diálogo destrutivo da página do cliente em ameixa | **Corrigido**: botão `b5` em `--ds-erro-texto`, igual ao de Prestadores; `role="alertdialog"` | Meus horários, Prestadores |
| 10 | Média | Raios próximos demais (14, 16, 18, 20) | **Corrigido**: três passos (12, 16, 24) e a pílula | 13 telas |
| 11 | Média | Paddings fora da escala (22, 26, 18) | **Corrigido**: 24, 24 e 16 | 13 telas |
| 12 | Baixa | Sombra leve no segmento e no interruptor | **Mantido de propósito**: é o relevo do item escolhido; registrado como `--ds-sombra`, some no escuro | — |
| 13 | Baixa | Esqueleto com raio 10 | **Corrigido**: 12 | Estados |
| 14 | Baixa | Prévia de horários (36 px, 15) não listada | **Documentado** como variante pequena do Horário | [`README.md`](README.md) §4.3 |
| 15 | Baixa | `--ds-grafico-marcado` com 2,56:1 no claro | **Mantido**, sempre com legenda, tooltip e "Ver como tabela"; escuro recalculado para 3,48:1 | Financeiro |
| 16 | Baixa | Valores do escuro sem cálculo | **Corrigido**: calculados e fixados (`#F4B4CF` 10,6:1, `#33222B` 12,9:1, `#A85A80` 3,48:1, véu) | `web/src/tokens.css` |

### Achados da revisão das telas renderizadas, corrigidos

| Achado | Correção |
|---|---|
| Blocos de 50 min na grade cortavam o nome em cima do serviço; blocos de 20 min cortavam a hora | O bloco mostra linhas conforme a altura (< 41 px: hora e ícone; 41–63: + nome; ≥ 64: + serviço) e, abaixo de 30 px, padding 1 10 |
| No painel de detalhe, "Remarcar" e "Marcar como falta" passavam da largura do painel | Secundárias empilhadas em largura total |

### Ainda em aberto

- Descrição e `og:description` de `web/src/index.html` ainda citam Telegram.
- Na tabela de Clientes (1440 px com o painel aberto), nome e WhatsApp quebram em duas linhas.
  Proposta: telefone sem quebra e a coluna "Última visita" escondida quando o painel abre.
