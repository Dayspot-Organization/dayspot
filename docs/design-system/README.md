# Sistema de design · Dayspot

Versão 2.1 · 17/11/2026. Regras normalizadas pelo novo design (proposta v2 + boas práticas da
identidade), com as correções da auditoria aplicadas. Leia o [`DESIGN.md`](../../DESIGN.md) antes.

| O quê | Onde |
|---|---|
| Brief (produto, voz, marca, decisões) | [`DESIGN.md`](../../DESIGN.md) |
| Tokens (fonte única) | [`tokens.css`](tokens.css) (também em `web/src/tokens.css`: seção 8) |
| Logos usados nas telas | [`assets/`](assets/) |
| Telas de referência v2.1 | [`telas/`](telas/) (abra o `.dc.html` no navegador) |
| Auditoria e correções | [`auditoria.md`](auditoria.md) |

**Como usar numa tela nova:** (1) escolha o padrão de tela da seção 5; (2) monte só com os
componentes da seção 4 e os tokens; (3) liste os estados com o checklist da seção 6;
(4) peça nova, cor nova ou tamanho novo entram aqui primeiro (seção 7), nunca direto na tela.

---

## 1. Regras que valem em toda tela

1. **Telas de trabalho são neutras.** Cor só em ação (ameixa), seleção (ameixa) e status
   (paleta de status). Degradê só em Entrar e Cadastro, sem texto em cima.
2. **Ameixa é ação e seleção, nunca status.** Pêssego só como preenchimento (o marcador do
   horário escolhido), nunca texto ou traço.
3. **Status sempre com ícone + palavra**, com os rótulos oficiais: Aguardando, Confirmado,
   Feito, Cancelado, Faltou, Erro. Nunca "pendente", "concluído", "no-show".
4. **Um nível de cartão** (raio 24, borda `--ds-divisoria`). Dentro dele, separe com espaço,
   divisória ou fundo `--ds-fundo`; nunca cartão dentro de cartão.
5. **Uma ação principal por tela** (ou por painel). O botão diz o que acontece, com dia e
   hora quando houver: "Agendar quarta, 10h". Nada de "OK", "Enviar", "Salvar" solto quando
   existe algo mais específico.
6. **Nunito só em título e horário;** Hanken Grotesk no resto. Números que mudam ou ficam
   em coluna com `font-variant-numeric: tabular-nums`.
7. **Menor texto 13 px.** Sem caixa alta, itálico, emoji ou inglês.
8. **O papel decide a tela.** Prestador não vê seletor de pessoa, Serviços, Prestadores nem
   Configurações. A API também bloqueia.
9. **Escuro troca tokens, não regras.** Sem sombra, sem brilho: as superfícies sobem de tom.
10. **Nenhum hex na tela ou no componente.** Tudo por `var(--ds-*)`.

---

## 2. Fundações

### 2.1 Cor (papéis)

| Token | Claro | Escuro | Uso | Nunca |
|---|---|---|---|---|
| `ds-fundo` | `#FBF7F5` | `#1A1417` | fundo da tela | — |
| `ds-superficie` | `#FFFFFF` | `#251D21` | cartão, painel, menu lateral, campo | — |
| `ds-hover` | `#F4ECEA` | `#30272B` | hover, botão secundário, trilho do segmento, esqueleto, item ativo do menu, avatar neutro | como borda |
| `ds-divisoria` | `#EEE3E0` | `#463A40` | divisória, borda de cartão, linha da grade | algo que precise ser visto (1,18:1) |
| `ds-borda` | `#8E807F` | `#857880` | campo, opção, horário livre, interruptor desligado | única pista de um campo |
| `ds-texto` | `#2B2227` | `#F5ECEF` | texto principal | — |
| `ds-texto-2` | `#5B4D53` | `#C0B0B7` | secundário, legenda, cabeçalho de tabela | — |
| `ds-texto-3` | `#8E807F` | `#857880` | placeholder, desabilitado | texto que precisa ser lido (3,56:1) |
| `ds-acao` | `#86315C` | `#EE9DBF` | botão principal, link, seleção, item ativo, ponto dos ícones | status; fundo escuro em ameixa |
| `ds-acao-pressionada` | `#6E2A55` | `#F4B4CF` | hover/toque do principal, hover de link | — |
| `ds-acao-suave` | `#FBF4F7` | `#33222B` | fundo de opção e linha selecionada | — |
| `ds-acao-texto` | `#FFFFFF` | `#1A1417` | texto sobre `ds-acao` | — |
| `ds-acento` | `#F4A08A` | `#F4B49A` | marcador do horário escolhido | texto, traço |
| `ds-foco` | `#86315C` | `#EE9DBF` | anel de foco, sólido e igual a `ds-acao` (pior fundo: 6,40:1 no claro, 5,26:1 no escuro) | cor translúcida (fica abaixo de 3:1) |

Todos os pares de texto conferidos (AA) nos dois temas em 17/11/2026.

**Status** (cada um com o token sem sufixo para barra e ponto, `-texto` e `-chip`):

| Status | Ícone | Claro: barra / texto / fundo | Forma no bloco | Origem no banco |
|---|---|---|---|---|
| Aguardando | relógio | `#A87400` / `#7A5200` / `#FCF0CF` | barra tracejada 4 px | `confirmado` sem `presenca_confirmada_em` |
| Confirmado | ✓ | `#2E7D32` / `#22622A` / `#E4F2E4` | barra cheia 4 px | `confirmado` + "Vou sim" |
| Feito | ✓✓ | `#1F5F8B` / `#1B527A` / `#E3EDF5` | sem barra | `concluido` |
| Cancelado | ✕ | `#8C8279` / `#5F5750` / `#EEEAE5` | borda tracejada, texto riscado; oculto por padrão | `cancelado` |
| Faltou | pessoa riscada | `#7350D6` / `#5A3BB8` / `#EEE9FC` | sem barra | novo (precisa de coluna) |
| Erro | ! | `#D1242F` / `#A8161F` / `#FDE6E6` | sempre com texto | falha ao salvar, mensagem não entregue |

`ds-erro` também serve a campo inválido e ação destrutiva. `ds-confirmado` serve a "Entregue"
(mensagem). Um aviso neutro usa `ds-hover` + `ds-texto`.

**Proporção numa tela de trabalho:** neutros 70 % · grafite 16 % · ameixa até 9 % ·
pêssego até 3 % · status até 2 % · degradê 0 %.

**Pares aprovados:** só os da tabela 3.3 de `boas-praticas-identidade.md`. Cor nova
precisa passar 4,5:1 (texto) ou 3:1 (ícone, borda, gráfico) antes de virar token.

### 2.2 Tipografia

| Token | Fonte | Uso | Exemplo |
|---|---|---|---|
| `ds-tx-h1` | Nunito 800 40/44, −1,5 % | título de Entrar e Cadastro | Entrar |
| `ds-tx-h2` | Nunito 800 28/34, −1 % | título de tela (um por tela) | Terça, 17 de novembro |
| `ds-tx-h3` | Nunito 700 20/26 | título de cartão, painel, passo, estado vazio | Próximos horários |
| `ds-tx-h4` | Hanken 700 16/22 | rótulo de grupo dentro do cartão | Mensagens no WhatsApp |
| `ds-tx-corpo` | Hanken 400 16/24 | texto, resumo da tela, campo | — |
| `ds-tx-controle` | Hanken 600 15/22 | rótulo de campo, item de menu, segmento, botão pequeno, aviso (400) | — |
| `ds-tx-tabela` | Hanken 400 15/22 | célula de tabela, lista de pares | — |
| `ds-tx-botao` | Hanken 600 16/20 | botão | Agendar quarta, 10h |
| `ds-tx-legenda` | Hanken 500 13/18 | legenda, ajuda, cabeçalho de coluna, meta | 3 visitas |
| `ds-tx-horario-*` | Nunito 800, tabular | destaque 32 · lista 20 · botão 16 · bloco 14 | 09:30 |

Número grande do financeiro: Nunito 800 32/40. Dia no seletor de dia: Nunito 800 22; dia no
cabeçalho da grade: 24/28. Importar só Hanken 400–700 e Nunito 700–800.

### 2.3 Espaço

Base 4: `ds-e-1` 4 · `ds-e-2` 8 · `ds-e-3` 12 · `ds-e-4` 16 · `ds-e-5` 20 · `ds-e-6` 24 · `ds-e-8` 32 · `ds-e-10` 40 ·
`ds-e-12` 48 · `ds-e-14` 56. Exceções nomeadas: `ds-e-rotulo` 6 (rótulo → campo), `ds-e-aviso` 10,
`ds-e-celula` 14.

Ritmo comum: entre cartões 20; entre campos 16; dentro de cartão e painel 24;
cabeçalho da tela → conteúdo 24; título → frase de resumo 4.

### 2.4 Forma, borda e elevação

- Raio: três passos e a pílula. `ds-raio-bloco` 12 (bloco da agenda, esqueleto) · `ds-raio-medio` 16
  (campo, aviso, opção, dia, quadro interno) · `ds-raio-cartao` 24 (cartão, painel, diálogo) ·
  `ds-raio-pilula` 999 (botão, horário, chip, segmento, faixa). Marca de gráfico: raio 4.
- Borda: 1 px `ds-divisoria` em cartão; 1,5 px `ds-borda` em campo e controle; 2 px `ds-acao` em
  opção selecionada; anel 2,5 px `ds-acao` em bloco selecionado.
- Sombra só no que flutua: `ds-sombra` (opção ativa do segmento, botão do interruptor) e
  `ds-sombra-forte` (diálogo, menu suspenso, aviso flutuante). Escuro: sem sombra.

### 2.5 Layout

| Peça | Medida |
|---|---|
| Quadro de referência | 1440 px |
| Menu lateral | 248 px, `ds-superficie`, borda direita `ds-divisoria`, padding 24 × 14 |
| Top-bar | 64 px, padding lateral 32, linha embaixo |
| Conteúdo | padding 28 32 48, até 1240 px |
| Painel lateral (detalhe, edição) | 340 px; resumo e prévia 360 px; `position: sticky; top: 24px` |
| Coluna principal + painel | `grid-template-columns: minmax(0,1fr) 340px; gap: 20px` |
| Página do cliente | até 1040 px, coluna 320 à direita, padding 48 × 40 |
| Grade da agenda | coluna de horas 64 px, 1 h = 72 px, 08–19 h |

### 2.6 Movimento

Hover e cor em `ds-t-rapido` (150 ms). Painel e diálogo abrem em `ds-t-medio` (220 ms) e fecham em
`ds-t-saida` (160 ms), com `ds-curva`. Esqueleto pulsa devagar (1,6 s). Nada de animação em tela
de trabalho além disso. `prefers-reduced-motion` zera as durações.

### 2.7 Ícones e imagens

- Ícones da marca: grade 24, traço 2,25 `currentColor`, pontas redondas, um ponto cheio em
  `ds-acao`. No menu, 22 px. Ícones de utilidade (fechar, setas, busca, tema, mais, check):
  mesmo traço, 16–20 px, sem ponto. Abaixo de 16 px: traço 2,5 e ponto na cor do traço.
- Ícone de status: traço 2,6, 15 px no chip, 14 no bloco, 18 no aviso.
- Logo: `logo-horizontal.svg` 26 px de altura no menu, 24 em Entrar, 20 no rodapé da
  página do cliente ("agendado com" + logo). Mínimo 96 × 20 px.
- Ilustrações de estado vazio: 176 × 132, tokens `--ilu-*`. Sem personagem, sem mascote.

### 2.8 Como escrevemos

- Frase curta, verbo primeiro, "você". O que aconteceu → o que fazer.
- Título de tela diz onde estou ("Clientes") ou o dia ("Terça, 17 de novembro"). Embaixo,
  uma frase de resumo com números reais: "5 horários hoje: 1 feito, 2 confirmados e 2
  aguardando resposta no WhatsApp."
- Erro explica o conserto: "Falta o domínio, como @gmail.com." / "O fim precisa ser depois
  do início. Era 13:00 às 20:00?" Nunca "Campo inválido".
- Erro nosso: assume, diz o estado real e o próximo passo. "Sem conexão. Tente de novo
  quando a internet voltar. Nada do que estava salvo foi perdido."
- Pergunta de confirmação traz o objeto: "Cancelar quarta, 10h?" "Remover Ana Rocha?"
- Pares de botão fixos: "Vou sim" / "Preciso cancelar" · "Cancelar horário" / "Voltar" ·
  "Marcar mesmo assim" · "Marcar como falta".
- Opcional se marca no rótulo: "Observação (opcional)". Removido aparece como "(removido)";
  a própria pessoa como "(você)".
- Guardar textos num arquivo de textos do produto, não espalhados no código.

---

## 3. Regras de interação (definidas uma vez)

| Estado | Peça com borda (campo, opção, horário livre, dia) | Peça sem borda (secundário, ícone, item de menu, terciário, linha clicável) | Ação principal |
|---|---|---|---|
| Normal | `ds-superficie` + borda 1,5 `ds-borda` | secundário/ícone: `ds-hover`; terciário e menu: transparente | `ds-acao` + `ds-acao-texto` |
| Hover | borda escurece para `ds-texto-2`; fundo não muda | transparente → `ds-hover` (item do menu → `ds-fundo`); `ds-hover` → mistura 6 % de `ds-texto` sobre `ds-hover` | `ds-acao-pressionada` |
| Pressionado | igual ao hover + fundo `ds-hover` | mistura 10 % de `ds-texto` | `ds-acao-pressionada` |
| Foco (só teclado, `:focus-visible`) | anel 3 px `ds-foco`, afastado 1 px, + borda `ds-acao` | anel 3 px `ds-foco` afastado 2 px | anel 3 px `ds-foco` afastado 2 px |
| Desabilitado | fundo `ds-hover`, borda `ds-hover`, texto `ds-texto-3`; horário ocupado também riscado | fundo `ds-hover`, texto `ds-texto-3` | fundo `ds-hover`, texto `ds-texto-3` |
| Carregando | — | mantém tamanho | **mantém a largura**: rótulo invisível, indicador centralizado, `aria-busy` |
| Selecionado | opção: borda 2 `ds-acao` + fundo `ds-acao-suave`; horário/dia: fundo `ds-acao` + `ds-acao-texto` (+ marcador `ds-acento` no horário) | menu: `ds-hover` + texto/ícone `ds-acao` (mesmo peso); filtro de pessoa e abas: `ds-acao` + `ds-acao-texto`; segmento: `ds-superficie` + `ds-sombra` | n/a |
| Erro | borda `ds-erro` + texto de ajuda em `ds-erro-texto` | — | — |

Desenhados em [`telas/Main.dc.html`](telas/Main.dc.html), seção "Estados de interação".

- Hover é sempre mais discreto que selecionado.
- Selecionar não muda o peso da fonte.
- Seleção é sempre ameixa (`ds-acao`); grafite não marca seleção.
- Placeholder em `ds-texto-3`, mais claro que `ds-texto-2`.
- Itens em lista de seleção têm folga de 8–10 px entre si; nunca encostados.
- Toda seleção também vai para o leitor de tela: `aria-pressed`, `aria-checked`,
  `aria-selected` ou `aria-current`.

---

## 4. Componentes

Cada componente: regra (quando usar e por quê), anatomia e matriz de estados. Nomes de
classe sugeridos entre parênteses; as telas de referência usam os nomes curtos (`.b1`, `.in`…).

### 4.1 Botão (`.botao`)

Uma ação principal por tela, à direita do cabeçalho ou no fim do painel. O texto diz o que
acontece. Ícone opcional à esquerda (18 px).

| Variante | Visual | Quando |
|---|---|---|
| Principal | `ds-acao`, `ds-acao-texto`, 44 px, padding 0 20, pílula | a próxima coisa a fazer |
| Principal de entrada | igual, 52 px | Entrar, Cadastro, Resumo do agendamento |
| Secundário | `ds-hover` + `ds-texto` | ação de apoio (Remarcar, Baixar agenda) |
| Terciário | transparente + `ds-acao`, padding 0 8 | ação leve dentro de linha ou painel (Trocar, Reenviar, Ver como tabela) |
| Destrutivo | transparente + `ds-erro-texto` | Cancelar horário, Remover (fora de diálogo) |
| Destrutivo cheio (`.b5`) | `ds-erro-texto` + `ds-acao-texto` | só dentro do diálogo de confirmação |
| Pequeno | 36 px, `ds-tx-controle`, padding 0 14 | barra de filtros, linha de tabela |
| Só ícone | círculo 40 px `ds-hover` (36 dentro de painel; transparente em linha de tabela) | buscar, tema, fechar, setas, mais; sempre com `aria-label` |
| Adicionar faixa | 36 px, transparente, borda 1,5 tracejada `ds-borda`, texto `ds-acao` | "+ Faixa" |

| Variante | Normal | Hover | Pressionado | Foco | Desabilitado | Carregando | Selecionado |
|---|---|---|---|---|---|---|---|
| Principal | ameixa | ameixa pressionado | ameixa pressionado | anel `ds-foco` | `ds-hover` + `ds-texto-3` | largura fixa + indicador | n/a |
| Secundário | `ds-hover` | +6 % texto | +10 % texto | anel | `ds-hover` + `ds-texto-3` | largura fixa + indicador | quando é filtro: ver 4.8 |
| Terciário | transparente | fundo `ds-hover` | +10 % | anel | `ds-texto-3` | indicador no lugar do texto | n/a |
| Destrutivo | `ds-erro-texto` | fundo `ds-erro-chip` | fundo `ds-erro-chip` | anel | `ds-texto-3` | indicador | n/a |
| Só ícone | `ds-hover` | +6 % texto | +10 % | anel | `ds-texto-3` | indicador | tema: ícone do tema atual |

### 4.2 Campo (`.campo`, `.entrada`)

Rótulo sempre visível acima (`ds-tx-controle`, 6 px), ajuda abaixo (`ds-tx-legenda`, `ds-texto-2`).
48 px, raio 16, borda 1,5 `ds-borda`, padding 0 16, `ds-tx-corpo`. Área de texto: altura livre,
padding 12 16. Busca: pílula, ícone 18 à esquerda (padding-left 44), rótulo escondido
visualmente. Seleção (`select`): mesmo visual do campo.

| Variante | Normal | Hover | Foco | Desabilitado | Erro | Preenchido |
|---|---|---|---|---|---|---|
| Texto / seleção / área | borda `ds-borda` | borda `ds-texto-2` | borda `ds-acao` + anel `ds-foco` | `ds-hover`, texto `ds-texto-3` | borda `ds-erro`, ajuda em `ds-erro-texto` explicando o conserto | texto `ds-texto` |
| Busca | idem, pílula | idem | idem | idem | n/a | idem |

Telefone, preço e duração com `tabular-nums`. Erro aparece ao sair do campo ou ao enviar,
nunca enquanto digita.

### 4.3 Horário (`.horario`)

Pílula 44 px, Nunito 800 16, tabular, em grade de 4–6 colunas com 8 px de folga, agrupada em
"Manhã" / "Tarde" (`ds-tx-h4`). Na prévia, 36 px e 15.

| Normal (livre) | Hover | Foco | Ocupado (desabilitado) | Selecionado |
|---|---|---|---|---|
| `ds-superficie` + borda 1,5 `ds-borda` | borda `ds-texto-2` | anel `ds-foco` | `ds-hover`, texto `ds-texto-3` riscado, `disabled`, `aria-label` "09:00, ocupado" | `ds-acao` + `ds-acao-texto` + marcador `ds-acento` 12 px no canto (contorno 2 px `ds-superficie`), `aria-pressed="true"` |

### 4.4 Dia (`.dia`)

64 × 76, raio 16, borda 1,5 `ds-borda`: dia da semana (`ds-tx-legenda` 600), número (Nunito 800
22), e "hoje" em `ds-acao` 700 ou "fechado". Fila de 8 dias com setas. Estados como o
horário: selecionado `ds-acao` + `ds-acao-texto`; fechado = desabilitado sem riscado.

### 4.5 Opção em cartão (`.opcao`)

Para escolher um entre poucos itens com dados (serviço com duração e preço; profissional).
Raio 16, padding 12 14, borda 1,5 `ds-borda`, rádio 20 px à esquerda, valor à direita em 700
tabular. `role="radio"` dentro de `role="radiogroup"`.

| Normal | Hover | Foco | Desabilitado | Selecionado |
|---|---|---|---|---|
| `ds-superficie` + borda | borda `ds-texto-2` | anel `ds-foco` | `ds-hover`, `ds-texto-3` | borda 2 `ds-acao`, fundo `ds-acao-suave`, rádio preenchido `ds-acao` |

Item escolhido com "Trocar" (cliente no passo 1): mesma aparência do selecionado, com
avatar, nome, meta e botão terciário "Trocar".

### 4.6 Segmento (`.segmento`)

Duas a quatro opções que trocam a visão (Dia/Semana; Todos, Com horário, Sem avisos).
Trilho `ds-hover`, pílula, padding 4, gap 2; opção 36 px, `ds-tx-controle`, `ds-texto-2`.
Contagem no rótulo: "Todos, 48".

| Normal | Hover | Foco | Selecionado |
|---|---|---|---|
| transparente, `ds-texto-2` | texto `ds-texto` | anel `ds-foco` | `ds-superficie` + `ds-texto` + `ds-sombra`, `aria-pressed="true"` |

### 4.7 Interruptor (`.interruptor`)

44 × 26, botão branco 20 com `ds-sombra`. Ligado `ds-acao`, desligado `ds-borda`.
`role="switch"` + `aria-checked`. Sempre com rótulo de texto ao lado ("Mostrar cancelados
(1)", "Segunda"). Desabilitado: trilho `ds-hover`. Foco: anel.

### 4.8 Filtro de pessoa / abas (`.filtro-pessoa`)

Grupo de botões pequenos (36 px; 44 nas abas de Disponibilidade) com avatar 24/32.
Ordem: Todos, a própria pessoa "(você)", outros, removidos "(removido)" em `ds-texto-2`.
Abre na própria agenda. Só administrador vê.

| Normal | Hover | Foco | Selecionado |
|---|---|---|---|
| secundário (`ds-hover`) | +6 % texto | anel | `ds-acao` + `ds-acao-texto`; o avatar dentro fica `ds-superficie` + `ds-acao` |

### 4.9 Chip de status (`.chip`)

28 px, pílula, padding 0 12 0 10, ícone 15 + palavra, Hanken 600 13. Fundo `ds-*-chip`,
texto e ícone `ds-*-texto`. Neutro (`ds-hover` + `ds-texto-2`) para informação sem gravidade
("Sem WhatsApp"). Não é clicável. **Estados de dado:** Aguardando, Confirmado, Feito,
Cancelado, Faltou, Erro, Neutro; para mensagens: Entregue (confirmado), Não entregue (erro),
Enviando (aguardando).

O normal não precisa de chip: na tabela de clientes "Recebe" é texto `ds-texto-2`; só a exceção
ganha cor.

### 4.10 Bloco da agenda (`.bloco`)

`<button>` posicionado na grade: topo = início, altura = duração (72 px por hora, −1 px de
folga), 4 px das bordas da coluna. Raio 12, padding 6 10, fundo `ds-*-chip` do status, barra
esquerda conforme o status. Linha 1: hora (`ds-tx-horario-bloco`) + ícone de status 14 à direita;
linha 2: nome 13/600 com reticências; linha 3: serviço 13 `ds-texto-2`. O que entra depende da altura:
< 41 px só hora e ícone (abaixo de 30 px, padding 1 10); 41–63 px hora e nome; 64 px ou mais, as três linhas.
`aria-label` "09:30, Marina Albuquerque, Corte + barba, Confirmado".

| Status | Normal | Hover | Foco | Selecionado |
|---|---|---|---|---|
| todos | fundo e barra do status | fundo escurece 3 % | anel `ds-foco` | anel 2,5 `ds-acao` (`box-shadow`) e o painel de detalhe aberto |
| Cancelado | `ds-superficie`, borda 1,5 tracejada `ds-borda`, texto riscado 60 % | idem | idem | idem; só aparece com "Mostrar cancelados" |

A **linha "agora"** é 2 px `ds-texto` com ponto
de 10 px na esquerda, só na coluna de hoje, com `aria-label` "Agora, 10:40". O dia de hoje
tem cabeçalho `ds-hover` com rótulo e número em `ds-acao`.

### 4.11 Aviso (`.aviso`)

Faixa no fluxo da página: raio 16, padding 12 16, ícone 18 + frase "o que aconteceu, o que
fazer", `ds-tx-controle` 400. Pode ter um botão pequeno à direita ("Ver os 3") ou horários
alternativos como botões.

| Dado | Fundo | Texto/ícone |
|---|---|---|
| Erro | `ds-erro-chip` | `ds-erro-texto` (título em negrito, resto em `ds-texto`) |
| Atenção | `ds-aguardando-chip` | `ds-aguardando-texto` |
| Sucesso | `ds-confirmado-chip` | `ds-confirmado-texto` |
| Neutro | `ds-hover` | `ds-texto` |

Erro de envio usa `role="alert"`. Aviso não some sozinho.

### 4.12 Diálogo de confirmação (`.dialogo`)

Para ação que não se desfaz (cancelar horário, remover pessoa). `role="alertdialog"`,
cartão raio 24, padding 24, `ds-sombra-forte`, sobre `ds-veu`. Título `ds-tx-h3` com a pergunta e o
objeto; uma frase com a consequência; ação nomeada e cheia em `ds-erro-texto` ("Cancelar horário",
"Remover e cancelar 7 horários") e "Voltar". Foco
vai para "Voltar"; `Esc` fecha e devolve o foco.

### 4.13 Cartão (`.cartao`) e painel lateral (`.painel`)

Cartão: `ds-superficie`, borda 1 `ds-divisoria`, raio 24, padding 24–28 × 24–32. Cabeçalho com
`ds-tx-h3`. Tabela dentro do cartão encosta nas bordas (`overflow: hidden`).

Painel lateral: cartão de 340 px, `sticky`, padding 24, gap 16. Topo: título `ds-tx-h3` (ou
chip de status) + botão fechar 36. Depois: destaque (horário grande ou avatar), lista de
pares, grupos com `ds-tx-h4`, e as ações no fim (principal em largura total; secundárias empilhadas em largura total, lado a lado só
quando as duas cabem em 292 px; destrutiva por último). `Esc` fecha e devolve o foco ao item de origem.

Cartão de número (financeiro): padding 20 24, rótulo 15/600 `ds-texto-2`, número Nunito 800
32/40, legenda. Valor provável (ainda pode mudar) em `ds-texto-2`.

Quadro interno: fundo `ds-fundo` ou `ds-hover`, raio 16, padding 12–16 (histórico, bloco de
horário do resumo). Não é um segundo cartão: sem borda.

### 4.14 Tabela (`.tabela`)

`ds-tx-tabela`. Cabeçalho `ds-tx-legenda` 600 `ds-texto-2`, padding 14 16 8. Célula padding 14 16,
divisória `ds-divisoria` em cima; primeira e última coluna com 24 de recuo. Número alinhado à
direita, tabular. Primeira coluna: avatar 40 + nome 700 + meta em legenda. Última: botão só
ícone "mais" transparente com `aria-label` "Editar <nome>". Linha selecionada: fundo
`ds-acao-suave`. Linha de removido: texto `ds-texto-2` + legenda "removido em 6 nov".
Barra de participação: 8 px, trilho `ds-hover`, preenchimento `ds-acao`.

### 4.15 Lista de pares (`.pares`)

Rótulo `ds-texto-2` à esquerda, valor 600 à direita, padding 10 0, divisória `ds-divisoria` em cima.
Valor pode ter um terciário pequeno ("Trocar"). Usada no detalhe e no resumo.

### 4.16 Avatar (`.avatar`)

Círculo com iniciais (2 letras, 700). Tamanhos: 24, 32, 36, 40, 44, 56. Neutro: `ds-hover`
+ `ds-texto`. A própria pessoa logada: `ds-acao` + `ds-acao-texto`. Fonte: 13 px nos de 24–36,
14 nos de 40–44, 18 no de 56. Nunca abaixo de 13.

### 4.17 Passo numerado (`.passo`)

Seção do formulário longo, separada por divisória, padding 24. Cabeçalho: círculo 28
(`ds-texto` + branco, 14/700) + título `ds-tx-h3` + legenda opcional na mesma linha. Passos
concluídos continuam editáveis; não há "Próximo": o resumo ao lado atualiza.

### 4.18 Faixa de horário (`.faixa`)

Pílula 40 px `ds-hover`, "09:00 – 12:00" em Nunito 800 16, botão remover 32 com ✕ e
`aria-label`. Erro: fundo `ds-erro-chip`, texto `ds-erro-texto`, contorno 1,5 `ds-erro`, e a frase do
conserto embaixo.

### 4.19 Estado vazio (`.vazio`)

Centralizado no cartão: ilustração 176 × 132, título `ds-tx-h3` que diz o fato, frase de 30ch
no máximo, uma ação principal (e no máximo uma secundária). Exemplos em
[`telas/Estados.dc.html`](telas/Estados.dc.html).

### 4.20 Esqueleto (`.esqueleto`)

Formas `ds-hover` raio 12 nas medidas finais do conteúdo, pulsando devagar.
`aria-busy="true"` no contêiner. Nunca "Carregando…" solto. Atualização com dados já na
tela não mostra esqueleto.

### 4.21 Gráfico de barras (`.grafico`)

SVG `role="img"` com descrição. Série 1 `ds-grafico-recebido`, série 2 `ds-grafico-marcado`, barras com raio 4 no
topo. Eixo `ds-borda`, grade `ds-divisoria`, rótulos `ds-tx-legenda` `ds-texto-2`; "hoje" em 700 `ds-texto` com
linha tracejada. Legenda com quadradinho 14 + nome. Tooltip com valor por barra e botão
"Ver como tabela" com a tabela equivalente.

### 4.22 Casca do painel (`.app`)

- **Menu lateral:** logo (26 px) · seletor de empresa (quadro `ds-fundo`, borda `ds-divisoria`, raio 16,
  "Empresa" em legenda + nome 15/700 + seta) · itens (44 px, pílula, padding 0 14, ícone 22 +
  `ds-tx-controle`) · grupo "Só administrador" (legenda) · rodapé com avatar 36 + nome + papel,
  divisória em cima. Hover em `ds-fundo`; item ativo em `ds-hover` + `ds-acao`, sem trocar o peso,
  `aria-current="page"`.
- **Top-bar:** página atual 15/600 `ds-texto-2` (ou trilha "Agenda › **Novo agendamento**", com
  a última parte em `ds-texto`) à esquerda; busca e tema (só ícone 40) à direita.
- **Cabeçalho da tela:** `ds-tx-h2` + frase de resumo `ds-texto-2` à esquerda; ações à direita
  (secundária antes da principal), alinhadas por baixo. 24 px até o conteúdo.

### 4.23 Painel de marca (Entrar e Cadastro)

Duas colunas (1 : 0,9). Esquerda: logo 24 no topo, formulário de até 420 px centralizado,
`ds-tx-h1` + frase, campos, principal 52 px, rodapé com divisória e links. Direita:
`ds-degrade`, padrão de arcos branco a 10 %, símbolo claro 168 px no centro, `aria-hidden`,
sem texto.

### Índice de componentes

| Peça | Onde os estados estão desenhados (`telas/`) | Código | Situação |
|---|---|---|---|
| Botão | §4.1 · `Main` | `web/src/styles.css` (`.botao`, `.botao-secundario`, `.botao-terciario`, `.botao-pequeno`, `.botao-icone`) | no código |
| Campo | §4.2 · `Main` | `web/src/styles.css` (`.campo`, `input`, `select`, `.busca`) | no código |
| Horário | §4.3 · `Novo-agendamento-web` | `web/src/styles.css` (`.horario`) | no código |
| Dia | §4.4 · `Novo-agendamento-web` | `web/src/styles.css` (`.dia`) | no código, sem "fechado" (ainda não há disponibilidade) |
| Opção em cartão | §4.5 · `Novo-agendamento-web` | `web/src/styles.css` (`.opcao`) | no código |
| Segmento | §4.6 · `Main`, `Agenda-semana` | `web/src/styles.css` (`.segmento`) | no código |
| Interruptor | §4.7 · `Main`, `Disponibilidade` | `web/src/styles.css` (`.interruptor`) | no código |
| Filtro de pessoa / abas | §4.8 · `Agenda-semana`, `Financeiro`, `Disponibilidade` | — | só design |
| Chip de status | §4.9 · `Main` | `web/src/styles.css` (`.chip`, `.st-*`) | no código |
| Bloco da agenda | §4.10 · `Main`, `Agenda-semana` | `app/agendamentos` (`.bloco`) | no código |
| Aviso | §4.11 · `Main`, `Clientes` | `app-mensagem-erro` (`.aviso`) | no código (erro) |
| Diálogo de confirmação | §4.12 · `Meus-horarios-cliente`, `Prestadores` | — | só design |
| Cartão / painel lateral | §4.13 · todas | `web/src/styles.css` (`.cartao`, `.painel`, `.com-painel`) | no código |
| Tabela | §4.14 · `Clientes`, `Servicos`, `Financeiro` | — | só design |
| Lista de pares | §4.15 · `Agenda-semana` | `web/src/styles.css` (`.pares`) | no código |
| Avatar | §4.16 · todas | `web/src/styles.css` (`.avatar`) | no código |
| Passo numerado | §4.17 · `Novo-agendamento-web` | `web/src/styles.css` (`.passo`) | no código |
| Faixa de horário | §4.18 · `Disponibilidade` | — | só design |
| Estado vazio | §4.19 · `Estados` | `app-vazio` | no código |
| Esqueleto | §4.20 · `Estados` | `web/src/styles.css` (`.esqueleto`), desenhado em cada tela | no código |
| Gráfico de barras | §4.21 · `Financeiro` | — | só design |
| Casca (menu, top-bar, cabeçalho) | §4.22 · todas | `web/src/app/layout.ts`, `painel.html` (`.cabecalho-tela`) | no código, sem seletor de empresa nem rodapé de usuário (ainda não há login) |
| Painel de marca | §4.23 · `Entrar`, `Cadastro` | — | só design |

---

## 5. Padrões de tela

Toda tela nova usa um destes. Se nenhum serve, é decisão de sistema: proponha antes.

| Padrão | Estrutura | Exemplos |
|---|---|---|
| **A. Lista + painel** | casca · cabeçalho (título, resumo, principal "Novo …") · aviso opcional · grade `1fr 340`: cartão com busca + segmento + tabela; painel de edição sticky | Clientes, Serviços, Prestadores |
| **B. Grade de tempo + detalhe** | casca · cabeçalho · barra de filtros (pessoa, cancelados, setas + Hoje, Dia/Semana) · grade `1fr 340`: cartão com grade de horas; painel de detalhe | Agenda |
| **C. Formulário em passos + resumo** | casca com trilha · cabeçalho sem ação · grade `1fr 360`: cartão com passos numerados; resumo sticky com horário grande, pares, principal 52 px, frase sobre o WhatsApp e erro 409 | Novo agendamento |
| **D. Edição por linha + prévia** | casca · abas por pessoa · grade `1fr 360`: cartão com uma linha por item (interruptor + faixas + total) e barra de salvar no rodapé ("Alterações não salvas", Descartar, Salvar …); prévia sticky | Disponibilidade, Configurações |
| **E. Relatório** | casca · cabeçalho com setas de mês · filtro de pessoa · 4 cartões de número · cartão de gráfico · grade `1.5fr 1fr` com tabelas | Financeiro |
| **F. Página pública** | sem casca, até 1040 · cabeçalho do salão (avatar 56 + nome + endereço) · `ds-tx-h2` · cartão do próximo horário com 2 ações · lista · rodapé "agendado com" + logo | Meus horários |
| **G. Painel de marca** | duas colunas, formulário + degradê | Entrar, Cadastro |

---

## 6. Checklist de estados para cada tela

Escreva para cada item: aplica (e o que mostra), não aplica (por quê) ou pergunta aberta.

- **Dados:** padrão · vazio de primeira vez (estado vazio com ação) · vazio por filtro ("Nenhum
  cliente com 'Paulo'" + Limpar busca) · um item · muitos (rolagem, contagem no filtro) · texto
  longo (nome de 40 letras, R$ 12.345,00, observação de 300 caracteres) · campo opcional
  ausente ("—").
- **Sistema:** primeiro carregamento (esqueleto) · atualização (sem esqueleto) · falha ao
  carregar (aviso de erro + "Tentar de novo") · sem conexão · sessão expirada · conflito 409
  (horário ocupado → alternativas) · 404 / outra empresa.
- **Pessoas:** administrador × prestador (o que some) · removido "(removido)" · a própria
  pessoa "(você)" · cliente sem WhatsApp.
- **WhatsApp:** enviada · entregue · não entregue · cliente respondeu.
- **Atividade:** salvando (botão mantém largura) · salvo (aviso de sucesso ou o painel
  atualiza) · alterações não salvas · confirmação antes de ação sem volta.
- **Tema:** claro e escuro conferidos.

---

## 7. Mudando o sistema

1. Veja se um componente existente + uma variante resolve. Prefira reusar.
2. Desenhe a peça com regra, exemplo real e matriz de estados. Cor nova vira token com
   contraste conferido; ícone novo segue a regra de ícones.
3. Liste todas as telas e arquivos de código que usam a peça.
4. Leve a mudança e a lista para aprovação do dono do produto.
5. Aprovado: atualize as telas em `telas/`, o código, este README, o índice,
   `tokens.css` e o Registro de decisões do `DESIGN.md`, no mesmo PR.

---

## 8. Migração dos tokens do código (v1 → v2.1)

**Feita na adaptação da Agenda.** [`tokens.css`](tokens.css) foi copiado para `web/src/tokens.css`
(carregado antes de `styles.css` pelo `angular.json`), os nomes seguem o mapa abaixo, o menu foi para
superfície, Hanken Grotesk + Nunito 700–800 entram pelo `index.html` e o logo usa os traços de
`logo-horizontal.svg` direto no componente, com as cores pelos tokens (`--ds-acao` no símbolo,
`--ds-texto` no nome: os mesmos valores das versões clara e escura).

| v1 (código) | v2.1 | Mudança |
|---|---|---|
| `--fundo`, `--superficie` | `--ds-fundo`, `--ds-superficie` | só o nome |
| `--afundado` | `--ds-hover` | só o nome |
| `--linha` | `--ds-divisoria` | escuro `#3A3035` → `#463A40` |
| `--linha-forte` (borda de campo `#DCCFCB`) | `--ds-borda` | `#8E807F` / `#857880`, 1,5 px |
| `--texto` `#1A1417` / `#F3E9EC` | `--ds-texto` | `#2B2227` / `#F5ECEF` |
| `--texto-2` `#6B5F64` / `#B3A2A9` | `--ds-texto-2` | `#5B4D53` / `#C0B0B7` |
| `--texto-3` | `--ds-texto-3` | só o nome |
| `--destaque`, `--destaque-texto`, `--marca` | `--ds-acao` | unificados |
| `--hover` (hover de link) | `--ds-acao-pressionada` | — |
| `--sobre-cor` | `--ds-acao-texto` | — |
| `--ponto` `#F4A08A` fixo | `--ds-acento` | escuro `#F4B49A` |
| `--foco` 22 % | `--ds-foco` | sólido, igual a `--ds-acao`; anel de 3 px |
| `--nav` (menu ameixa sólido) | sai | menu em `--ds-superficie`, item ativo em `--ds-hover` + `--ds-acao` |
| `--agendado` | sai | bloco usa `--ds-<status>-chip` |
| `--sucesso*`, `--atencao*`, `--perigo*` | `--ds-confirmado*`, `--ds-aguardando*`, `--ds-erro*` | paleta de status |
| logo desenhado + "dayspot" digitado | `logo-horizontal.svg` e `-escuro.svg` | regra da marca: sempre o SVG |
| Nunito em tudo, texto 14/20 | Hanken Grotesk 16/24 + Nunito em título | botão pílula 44, campo 48 raio 16, cartão raio 24 |
