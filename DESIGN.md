# Dayspot · design

Referência de design do `web/`, na versão 3. A fonte é a **proposta Dayspot v3**, que fica fora do
repo (peça o link a quem cuida do design). Quando a proposta e este arquivo divergirem, vale a
proposta, e este arquivo é corrigido no mesmo PR. Os valores dos tokens ficam em
[`web/src/tokens.css`](web/src/tokens.css); as classes prontas, em `web/src/styles.css`, na seção
"Componentes v3".

## Como usar numa tela nova

1. **A tela está desenhada na proposta?** Hoje estão a Agenda da semana, Entrar e Cadastro. Se
   está, medidas, estados e textos são os dela.
2. **Se não está,** escolha um [padrão de tela](#padrões-de-tela) e monte só com as
   [fundações](#fundações) e os [componentes](#componentes) daqui.
3. **Liste os estados** com o [checklist](#checklist-de-estados).
4. **Peça, cor ou medida nova** entra primeiro na proposta e aqui, nunca direto na tela
   ([Mudando o sistema](#mudando-o-sistema)).
5. **Só `var(--ds-*)` e classes `.ds-*`.** Os apelidos v2.1 do `tokens.css` e as classes
   `.botao`, `.campo` e `.aviso` são das telas que ainda estão na v2.1 (Agenda, Novo agendamento
   e `/agendar`) e saem quando elas forem redesenhadas.

## Produto e público

O Dayspot é a agenda online de salões, barbearias, clínicas e estúdios de bairro.

- **Quem usa o painel:** donos e prestadores, pouco técnicos, entre um atendimento e outro, às
  vezes sob luz forte e com a mão molhada.
- **Quem agenda:** clientes de todas as idades, que chegam pelo link do WhatsApp sem ter conta.
- **Clima:** calma, precisa, acolhedora, como uma grade de horários bem cuidada. Nunca um
  dashboard SaaS genérico, um app "fofo", um salão cor-de-rosa ou uma tela com degradê e brilho.

## Como escrevemos

A recepcionista boa de um salão de bairro: clara, calorosa, confiável, sem pose.

- Frase curta, verbo primeiro, "você". Diz o que aconteceu e depois o que fazer: "E-mail ou
  senha não conferem. Confira e tente de novo."
- Português do Brasil, sem inglês na tela: horário, lembrete, falta; nunca slot, booking, no-show.
- Sem emoji, exclamação, caixa alta ou itálico.
- Formatos: "quarta, 10h" em frase e 10:00 em grade e lista; "18 de novembro" ou "18 nov";
  R$ 70,00.
- O canal sempre nomeado: "pelo WhatsApp".
- O botão diz a ação, com dia e hora quando houver: "Novo horário", "Agendar quarta, 10h",
  "Manter". Nunca "OK", "Sim" ou "Enviar".

## Regras que valem em toda tela

As cinco primeiras são da v3; as outras continuam da v2.1.

1. **Branco primeiro.** O conteúdo fica num painel `ds-superficie` com canto de 16 px sobre a
   casca `ds-fundo`, onde ficam o topo e o menu. Dentro, as áreas se separam por um fio de 1 px
   (`ds-divisoria`). Nada de cartão dentro de cartão.
2. **Espaço no lugar de caixa.** 32 px dentro do painel, 24 entre grupos, 48 entre seções. Linha
   de lista de 40 px e controle de 32. O texto continua compacto.
3. **Cinza na estrutura, rosa na interação.** Texto, fios e fundos em cinza neutro, de contraste
   alto. `ds-acao` preenche a ação principal, o hoje e o horário escolhido, sempre com
   `ds-acao-texto` em cima. Texto, ícone e linha da marca usam `ds-tinta`, só no que está ativo
   ou escolhido; o hover fica num rosa bem claro. O pêssego `ds-acento` fica nas peças de marca.
   O rosa nunca é status.
4. **Uma família, dois pesos.** Geist 400 e 500 em toda a interface, com corpo de 13 px no
   painel. A hierarquia vem do tamanho e da cor de apoio. Número sempre tabular.
5. **Ícone de traço fino.** Lucide, traço 1,75 (1,5 no menu), sem o ponto da marca, 14 px nos
   controles e 20 no menu.
6. **Status sempre com ícone e palavra**, com os rótulos oficiais: Aguardando, Confirmado, Feito,
   Cancelado, Faltou e Erro. Nunca "pendente", "concluído" ou "no-show".
7. **Uma ação principal por tela** (ou por painel).
8. **Menor texto 13 px.**
9. **O papel decide a tela.** O prestador não vê o seletor de pessoa ("Toda a equipe"), Serviços,
   Equipe nem Configurações, e só enxerga a própria agenda. A API também bloqueia: esconder na
   tela não é autorização.
10. **Escuro troca tokens, não regras.** Sem sombra nem brilho: as superfícies sobem de tom.
11. **Nenhum hex na tela nem no componente.** Tudo por `var(--ds-*)`.

## Fundações

### Cor

Os nomes são por função: uma tela pede `ds-texto-2`, nunca `#52525B`. Claro e escuro usam os
mesmos nomes, e os valores estão no `tokens.css`. Todos os pares de texto passam AA nos dois temas
(conferido em 19/11/2026).

| Token | Uso |
|---|---|
| **Neutros** | |
| `ds-fundo` | casca: atrás do painel, onde ficam o topo e o menu. Nunca atrás de texto longo |
| `ds-superficie` | painel, cartão, campo, menu suspenso |
| `ds-hover` | preenchimento cinza: etiqueta de status, trilho do segmento, pressionado |
| `ds-hover-leve` | hover de peça com e sem contorno |
| `ds-faixa` | aviso neutro, horário feito ou ocupado (`ds-texto-3` em cima: 4,6:1) |
| `ds-casca-hover` | hover do que fica na casca: a empresa e os botões do topo |
| **Linhas, da mais leve à mais forte** | |
| `ds-divisoria-suave` | entre linhas de lista e grupos de menu; contorno do botão desabilitado |
| `ds-divisoria` | o fio de 1 px entre áreas (barra, painel, detalhe) e o do botão secundário. Só decorativo |
| `ds-contorno` | bloco da agenda e barra das etapas por fazer |
| `ds-contorno-forte` | horário livre |
| `ds-contorno-hover` | hover de bloco e de horário |
| `ds-tracejado` | horário cancelado e espaço vazio da grade |
| `ds-borda` | campo e select em repouso: a única linha que precisa de 3:1 (3,4:1 e 4,4:1) |
| `ds-grade-hora`, `ds-grade-meia` | linhas da hora e da meia hora na grade; a meia também listra o intervalo |
| `ds-rolagem` | polegar da barra de rolagem fina |
| **Texto** | |
| `ds-texto` | texto principal (17,7:1) |
| `ds-texto-2` | apoio, legenda, rótulo secundário (7,7:1) |
| `ds-texto-3` | placeholder, hora da grade, desabilitado (4,8:1). Nunca texto que precisa ser lido para agir |
| `ds-icone` | ícone parado em botão, etiqueta e menu |
| **Marca** | |
| `ds-acao` | o rosa da marca, #EAA2C1 nos dois temas. Só preenchimento: botão principal, hoje, horário escolhido, hora de agora e a logo. Sobre o branco fica em 2:1, então nunca é texto nem linha |
| `ds-acao-pressionada` | hover e toque do botão principal |
| `ds-acao-texto` | texto e ícone sobre o rosa (8,8:1) |
| `ds-tinta` | o rosa legível: texto, ícone, linha e foco da marca, só no que está ativo ou escolhido (6,0:1) |
| `ds-foco` | anel de foco do teclado, com o mesmo valor da tinta |
| `ds-acao-suave` | linha, opção e bloco selecionados (tinta em cima: 5,5:1) |
| `ds-menu-hover` | hover do item do menu e das ações em tinta |
| `ds-menu-ativo` | item ativo do menu e aba escolhida (tinta em cima: 5,3:1) |
| `ds-hoje` | fundo da coluna de hoje na agenda |
| `ds-acento` | pêssego: só em peça de marca e ilustração, como preenchimento |
| **Peças de marca** | |
| `ds-degrade-1` a `-3` | degradê do rosa claro ao lilás, só em Entrar, Cadastro e divulgação, sem texto em cima |
| `ds-arco` | arcos de 1 px sobre o degradê |
| `ds-simbolo-degrade` | o símbolo D sobre o degradê: branco no claro, rosa no escuro |
| **Outros** | |
| `ds-veu` | atrás da gaveta do celular (casca da v2.1) |
| `--ilu-*` | ilustração de estado: um desenho serve aos dois temas |

**Status.** Seis, fora da marca, cada um com um ícone e três tokens: `ds-<status>` é a cor do
ícone (3,6:1 ou mais sobre `ds-hover`), `ds-<status>-texto` é a palavra quando ela precisa de cor
(aviso, mensagem de campo) e `ds-<status>-chip` é o fundo de um aviso tingido. Na etiqueta de
status o fundo é `ds-hover` e só o ícone leva a cor. `ds-erro-texto` também é o destrutivo.

| Status | Token | Ícone |
|---|---|---|
| Aguardando | `ds-aguardando` | `clock` |
| Confirmado | `ds-confirmado` | `circle-check` |
| Feito | `ds-feito` | `check-check` |
| Cancelado | `ds-cancelado` | `circle-x` |
| Faltou | `ds-faltou` | `user-x` |
| Erro | `ds-erro` | `circle-alert` |

### Tipo

Geist 400 e 500, pelo Google Fonts, com `system-ui` de reserva. A Nunito fica só dentro do SVG do
wordmark.

| Estilo | Medida | Uso |
|---|---|---|
| Título de entrada | 500 32/38, −3% | Entrar, Cadastro e peças de marca; um por tela |
| Título de tela | 500 18/26, −1,5% | título da tela |
| Título de painel | 500 18/26, −1% | título do painel lateral |
| Número | 500 20/28, −2%, tabular | horário e valor em destaque |
| Corpo | 400 13/20 | corpo do painel |
| Controle | 500 13/20 | botão, aba, segmento, rótulo, item de menu |
| Legenda | 400 13/18 | o mínimo da interface |
| Campo grande | 400 15/22 | campo de 44 px e frase sob o título de entrada |
| Botão grande | 500 15/20 | botão de 48 px |
| Corpo do cliente | 400 16/24 | texto para quem agenda, fora do painel |

Nada abaixo de 13 px. Horário, valor e contagem sempre com `font-variant-numeric: tabular-nums`.

### Espaço, raio e altura

| Espaço | px | Uso |
|---|---|---|
| `ds-e-1` | 4 | entre botões juntos e entre abas |
| `ds-e-rotulo` | 6 | rótulo até o campo; ícone até o texto no botão e na etiqueta |
| `ds-e-2` | 8 | ícone até o texto; entre itens do menu e entre controles da barra |
| `ds-e-aviso` | 10 | ícone até o texto no aviso; padding lateral da opção de menu |
| `ds-e-3` | 12 | padding lateral de botão, campo e linha |
| `ds-e-4` | 16 | entre campos de um formulário |
| `ds-e-6` | 24 | entre grupos dentro do painel |
| `ds-e-8` | 32 | padding do painel e do cartão |
| `ds-e-12` | 48 | entre seções de uma página |
| `ds-e-16` | 64 | entre colunas de página larga |

Nenhum padding fora da escala. As poucas exceções da proposta estão no componente que as usa.

| Raio | px | Uso |
|---|---|---|
| `ds-raio-bloco` | 8 | bloco da agenda, opção de menu, dica |
| `ds-raio-campo` | 10 | campo, select, linha selecionada, horário, item do menu, aviso |
| `ds-raio-menu` | 12 | menu suspenso |
| `ds-raio-painel` | 16 | painel de conteúdo, diálogo, peça de marca |
| `ds-raio-pilula` | 999 | botão, etiqueta, busca, segmento, avatar |

| Altura | px | Uso |
|---|---|---|
| `ds-a-etiqueta` | 24 | etiqueta de status, avatar pequeno |
| `ds-a-controle` | 32 | botão, aba, busca e segmento no painel |
| `ds-a-alvo` | 36 | área mínima de clique no painel; campo, select, horário, opção de menu |
| `ds-a-menu` | 38 | item do menu de ícones |
| `ds-a-linha` | 40 | linha de lista |
| `ds-a-campo-grande` | 44 | campo de Entrar e Cadastro; alvo mínimo de quem agenda |
| `ds-a-acao-grande` | 48 | ação principal fora do painel |

No painel, os controles têm 32 px desenhados e 36 de área de clique: um `::before` passa 2 px da
borda. Medidas de layout: topo de 48 (`ds-l-topo`), barra da tela de 64 (`ds-l-barra`), busca de
320, painel lateral de 360 (`ds-l-lateral`), formulário de 400 e hora da grade de 72. Borda de
1 px sempre (`ds-b-fio`); a seleção do bloco usa um anel de 1,5 (`ds-b-selecao`) e o foco, 2
(`ds-b-foco`).

### Elevação

Nenhuma sombra no escuro: lá os valores são `0 0 #0000`, e não `none`, para poderem ser somados a
um fio.

| Sombra | Uso |
|---|---|
| `ds-sombra` | opção ativa do segmento e bloco da agenda, que precisa se descolar da grade |
| `ds-sombra-flutuante` | o que flutua: menu suspenso e diálogo |
| sombra forte | bloco selecionado da agenda, junto do anel de 1,5 px: `0 6px 16px -4px rgba(24, 24, 27, 0.12)`. Vira `--ds-sombra-forte` quando o bloco entrar no código |

### Movimento

Discreto, e zerado com `prefers-reduced-motion`.

| Token | Valor | Uso |
|---|---|---|
| `--ds-t-rapido` | 120 ms | hover, cor, contorno |
| `--ds-t-medio` | 220 ms | painel lateral e diálogo abrindo |
| `--ds-t-saida` | 160 ms | fechar, sempre mais rápido que abrir |
| `--ds-curva` | `cubic-bezier(.2, .8, .2, 1)` | quase tudo |

O indicador de carregamento gira em 800 ms, linear. As peças da Agenda (menu de ícones, empresa no
topo, busca e bloco) usam 150 ms.

### Ícones

Lucide (licença ISC), pelo `@lucide/angular`: cada ícone é um `<svg lucideNome>`, e o traço de
1,75 vem do `provideLucideConfig`. Grade de 24, pontas redondas. Tamanhos: 13 px no bloco da
agenda e na ajuda do campo, 14 nos controles, na etiqueta e no aviso, e 20 no menu, com traço 1,5.
A cor vem de `ds-icone` parado, de `ds-tinta` quando ativo e de `ds-<status>` no status. Ícone que
faltar é desenhado na mesma grade e no mesmo traço. O ponto da marca não vai nos ícones: fica na
logo e nas ilustrações de estado.

| Função | Ícone |
|---|---|
| Agenda, Clientes, Serviços, Equipe, Financeiro, Configurações | `calendar`, `users`, `scissors`, `user-round`, `chart-column`, `settings` |
| Buscar, novo, navegar, mais ações | `search`, `plus`, `chevron-left`, `-right`, `-down`, `ellipsis` |
| WhatsApp, senha | `message-circle`, `eye` e `eye-off` |

### Logo

O símbolo D aberto, em `ds-acao` (#EAA2C1) nos dois temas. Sobre o degradê, `ds-simbolo-degrade`:
branco no claro e rosa no escuro. Tamanhos: 22 px no topo da casca, 26 em Entrar e Cadastro e 168
no painel de marca. Na interface vai só o símbolo; o wordmark (Nunito 800) espera o redesenho do
nome. Nunca redesenhar nem distorcer o símbolo.

Ícone do app (`favicon.svg`, `.ico`, PNG e manifesto): o símbolo em #EAA2C1 sobre um quadrado
#18181B (cerca de 9:1). O símbolo ocupa 56% da altura no ícone comum, 66% no favicon e 40% no
maskable, por causa da área segura. As imagens de compartilhamento (`og-image.png` e
`twitter-card.png`) ainda são da v2.1 e esperam um desenho novo.

## Regras de interação

Escritas uma vez: os componentes seguem estas regras em vez de cada um inventar as suas. O foco
aparece só pelo teclado (`:focus-visible`), menos no campo, que mostra a borda também no clique.

**Peças com contorno:** botão secundário, campo, select, busca, horário e bloco da agenda.

| Estado | Como fica |
|---|---|
| Hover | O contorno escurece: `ds-texto-3` no campo, select e busca; `ds-contorno-hover` no horário e no bloco. Botão secundário e horário também ganham fundo `ds-hover-leve`, porque o fio deles (1,2:1) não avisa sozinho que dá para clicar |
| Pressionado | Fundo `ds-hover` |
| Foco | Campo, select e busca: só a borda em `ds-tinta`, sem halo. Os demais: anel de 2 px em `ds-foco`, afastado 2 px |
| Desabilitado | Sem fundo, fio `ds-divisoria-suave`, texto e ícone `ds-texto-3`, cursor bloqueado. Horário ocupado: fundo `ds-faixa` e texto riscado |
| Carregando | Mesma largura; o texto fica invisível e um indicador de 14 px gira no centro. Os campos do formulário ficam só de leitura |
| Selecionado | Horário: fundo `ds-acao` com `ds-acao-texto`. Bloco: fundo `ds-acao-suave` e anel de 1,5 px em `ds-tinta` |
| Erro | Campo com borda `ds-erro` e, embaixo, a mensagem em `ds-erro-texto` com ícone |

**Peças sem contorno:** botão fantasma, aba, item do menu, opção e linha de lista.

| Estado | Como fica |
|---|---|
| Hover | Fundo `ds-hover-leve`; na casca, `ds-casca-hover`; no menu e nas ações em tinta, `ds-menu-hover`. A linha de lista arredonda (`ds-raio-campo`) e esconde o fio |
| Pressionado | Fundo `ds-hover` |
| Foco | Anel de 2 px em `ds-foco`, afastado 2 px |
| Desabilitado | Texto e ícone `ds-texto-3`, sem hover |
| Selecionado | Aba e item do menu: fundo `ds-menu-ativo`, texto e ícone `ds-tinta`. Linha: fundo `ds-acao-suave` e nome em `ds-tinta`. Opção de menu: check em `ds-tinta` à direita e texto em 500 |

**Ação principal:** no máximo uma por tela.

| Estado | Como fica |
|---|---|
| Normal | Fundo `ds-acao`, texto e ícone `ds-acao-texto`, sem contorno |
| Hover e pressionado | Fundo `ds-acao-pressionada` |
| Foco | Anel de 2 px em `ds-foco`, afastado 2 px |
| Desabilitado | Igual ao secundário desabilitado: o rosa nunca aparece apagado |
| Carregando | Mesma largura, com o indicador em `ds-acao-texto`; o rosa continua |

**Destrutivo.** Na tela, é só texto em `ds-erro-texto`, com hover `ds-erro-chip`. O botão cheio
(fundo `ds-erro-texto`, texto `ds-superficie`) aparece só na confirmação, ao lado de "Manter", e
mantém a cor no hover: `ds-erro` com texto branco fica em 3,97:1.

## Componentes

| Peça | Classe | No código |
|---|---|---|
| Botão | `.ds-botao`, com `--principal`, `--fantasma`, `--icone` e `--grande` | sim, menos o texto perigo e o perigo |
| Campo | `.ds-campo`, `.ds-entrada`, `.ds-prefixo`, `.ds-junto`, `.ds-ajuda`, `--grande` e `--erro` | sim |
| Aviso | `.ds-aviso`, com `--erro` e `--neutro` | sim |
| Etapas | `.ds-etapas` | sim |
| Painel de marca | `.painel-marca` | sim |
| Select | `.ds-selecao` | não |
| Menu suspenso | `.ds-menu`, `.ds-opcao` | não |
| Etiqueta de status | `.ds-etiqueta` | não |
| Segmento | `.ds-segmento` | não |
| Abas | `.ds-abas`, `.ds-aba` | não |
| Lista | `.ds-lista`, `.ds-linha` | não |
| Horário | `.ds-horario` | não |

O que ainda não está no código entra com a primeira tela que usar, com as medidas daqui, o desenho
da proposta e o nome de classe desta tabela.

### Botão

Dispara uma ação: no painel, uma pílula de 32 px com clique de 36; fora dele, 48.

- **Principal** (`--principal`): a próxima coisa que a pessoa deve fazer, no máximo um por tela.
  Ícone à esquerda quando ajuda ("+ Novo horário").
- **Secundário** (padrão): ações de apoio, ao lado do principal. Fundo `ds-superficie` e fio
  `ds-divisoria`.
- **Fantasma** (`--fantasma`): navegação e ações leves, como Voltar e as setas da semana. Sem
  fundo nem fio.
- **Só ícone** (`--icone`): 32 × 32, sempre com `aria-label`, e só com ícone conhecido (setas,
  mais ações).
- **Texto perigo:** ação destrutiva na tela, sempre por último ("Cancelar horário").
- **Perigo:** o vermelho cheio, só na confirmação.
- **Grande** (`--grande`): 48 px e texto de 15, para a ação principal fora do painel, na largura
  toda do formulário.

Texto: verbo e objeto, sem ponto ("Remarcar", "Criar conta e abrir a agenda"), e o rótulo não
quebra linha. Para escolher entre opções, use Segmento ou Abas, não botão. Carregando:
`aria-busy="true"`. Medidas: 32 px, padding 0 12 (0 14 0 10 no principal com ícone), ícone de 14 a
6 px do texto, texto 500 13. O grande tem 48 px, padding 0 22 e texto de 15. Sempre
`<button type="button">` nativo.

### Campo

Recebe texto digitado: rótulo sempre visível em cima, ajuda ou erro embaixo, e no foco só a borda
muda.

- **Campo** (`.ds-campo` com `.ds-entrada`): 36 px, no painel.
- **Grande** (`--grande`): 44 px e texto de 15, em Entrar, Cadastro e na página do cliente.
- **Com prefixo** (`.ds-prefixo`): a parte fixa do valor fica dentro da borda ("agendar/").
- **Com botão** (`.ds-junto`): um botão só ícone dentro da borda, como o "Mostrar senha", que é um
  alternador de nome fixo, com `aria-pressed`.

O rótulo é o nome do dado e leva "(opcional)" quando for o caso. A ajuda diz para que serve, em uma
frase. O erro diz como consertar ("Falta o domínio, como @gmail.com."), nunca "Campo inválido", e
aparece ao sair do campo ou ao enviar, nunca enquanto a pessoa digita, com `aria-invalid="true"` e
`aria-describedby`. O placeholder é só exemplo. Enquanto o formulário envia, os campos ficam só de
leitura e o indicador fica no botão. O valor rola dentro do campo, que nunca cresce.

Medidas: rótulo 500 13/18 a 6 px do campo; campo de 36, padding 0 12, borda de 1 px, raio 10 e
texto 400 13; ajuda 400 13/18 em `ds-texto-2`, com ícone de 13. Sempre `<label for>`.

### Aviso

A mensagem que não é de um campo.

- **Erro** (`--erro`): fundo `ds-erro-chip`, texto `ds-erro-texto` e `circle-alert`. Fica no topo
  do formulário, logo abaixo do título, com `role="alert"`: credencial errada, muitas tentativas,
  sem conexão.
- **Neutro** (`--neutro`): fundo `ds-faixa`, fio `ds-divisoria` e texto `ds-texto-2`, para o que
  informa sem pedir ação, como no detalhe da Agenda.

Medidas: raio 10, texto 13/18 e ícone de 14; o erro com padding 10 × 12 e 8 px até o texto, o
neutro com 12 × 14 e 10 px. Aviso de atenção e de sucesso ainda não estão desenhados.

### Etapas

"Etapa 1 de 2" acima do título, com uma barra por etapa: 20 × 4 px, em pílula, com 4 px entre
elas; a feita em `ds-tinta` e a por fazer em `ds-contorno`. Texto 13/18 em `ds-texto-2`, a 10 px
das barras. Ao trocar de etapa, o foco vai para o título. Hoje só aparece no Cadastro.

### Painel de marca

Só em Entrar e Cadastro: 576 px à direita, recuado 12 px da borda e com canto de 16. Degradê de
`ds-degrade-1` a `-3` a 160°, arcos de 1 px em `ds-arco` e o símbolo de 168 px em
`ds-simbolo-degrade`. Sem texto e escondido do leitor de tela. Abaixo de 1148 px de largura o
painel some, e o formulário sobe para o topo.

### Select

Escolhe um valor de uma lista fixa com o `<select>` nativo, com a borda e as medidas do Campo. A
lista aberta é a do sistema: funciona no celular, no teclado e no leitor de tela sem nada nosso.

- Use de 5 a umas 30 opções que a pessoa conhece (serviço, fuso, duração). Com 2 a 4 opções que
  trocam a visão, Segmento; para escolher pessoas, Abas ou Menu suspenso.
- Opção longa: rótulo curto e o detalhe depois de " · " ("Corte e escova · 45 min").

Medidas: 36 px, padding 0 36 0 12, e `chevron-down` de 14 a 12 px da direita, em `ds-icone`.

### Menu suspenso

Uma lista curta que abre de um botão e fecha ao escolher: o "Toda a equipe ▾" da Agenda e a troca
de empresa no topo.

- Para pessoa, empresa ou visão quando não cabe em Abas (mais de 5 pessoas) ou quando a escolha é
  rara. Para valor de formulário, use o Select.
- Botão secundário com `chevron-down`, `aria-haspopup="listbox"` e `aria-expanded`. Lista de 240
  px, padding 4, raio 12, fio `ds-divisoria` e `ds-sombra-flutuante`. Opções de 36 px, padding
  0 10 e raio 8, com avatar de 24 opcional e um separador `ds-divisoria-suave` entre grupos.
- Escolhida: check em `ds-tinta` à direita e texto em 500. Desabilitada: pessoa removida, em
  `ds-texto-3`, com "(removido)" no nome.
- Teclado: setas movem, Enter escolhe, Esc fecha e devolve o foco ao botão. A ordem é Toda a
  equipe, você, a equipe em ordem alfabética e os removidos por último. Nome longo fica numa linha
  com reticências, e o completo vai no `title`.

### Etiqueta de status

Mostra em que pé está um horário: fundo `ds-hover`, palavra em `ds-texto` e a cor do status só no
ícone. Sempre ícone e palavra; o ícone sozinho só vale no bloco da agenda, com a palavra no
`aria-label`. Não é clicável e não tem hover. A situação normal não leva etiqueta: só o que pede
atenção ganha uma. Medidas: 24 px, padding 0 10 0 8, ícone de 14 a 6 px, texto 500 13, pílula.

### Segmento

Troca a visão da mesma coisa, com 2 a 4 opções num trilho cinza: Dia ou Semana, Todos ou Com
horário. Uma opção está sempre ativa e muda a tela na hora, sem salvar nada. Texto de uma ou duas
palavras, com a contagem depois de vírgula ("Todos, 248"). Escolhida: fundo `ds-superficie`, texto
`ds-texto`, `ds-sombra` e `aria-pressed="true"`; as outras em `ds-texto-2`. Medidas: trilho
`ds-hover` com padding 3 e 2 px entre opções; opção de 26 px, com clique de 36, padding 0 12 e
texto 500 13. O 3, o 2 e o 26 ficam fora da escala de propósito: encaixam o trilho em 32.

### Abas

Escolhe de quem é a agenda, o financeiro ou a disponibilidade: pílulas lado a lado, com a escolhida
no tom claro da marca.

- Filtro de pessoa do administrador, com até 5 pessoas; com mais, Menu suspenso. O prestador não
  vê as abas.
- Primeiro nome, com "(você)" na pessoa logada. A ordem é Toda a equipe, você e a equipe em ordem
  alfabética. Pessoa removida não aparece.
- Escolhida: fundo `ds-menu-ativo`, texto `ds-tinta` e avatar em `ds-superficie` com as iniciais
  em tinta. O peso não muda.

Medidas: 32 px com clique de 36, padding 0 12, texto 500 13, avatar de 24 a 6 px do nome e 4 px
entre as abas.

### Lista

Uma coluna de registros parecidos (clientes, equipe, serviços), em linhas de 40 px que abrem o
detalhe. Serve para registros com um nome e dois ou três dados curtos.

- Avatar de 24 · nome 500 com reticências · até dois dados em `ds-texto-2` · `chevron-right` de 14
  em `ds-texto-3`. As colunas têm largura fixa, para os dados alinharem de uma linha para outra, e
  um fio `ds-divisoria-suave` separa as linhas.
- Selecionada: a linha cujo detalhe está aberto, com `aria-current="true"`, fundo `ds-acao-suave`
  e nome em `ds-tinta`. Pessoa removida: `ds-texto-3`, com "(removido)", e ainda abre o histórico.
- Com um item, a lista não estica. Com muitos, rola dentro do painel, e o cabeçalho da tela fica
  parado.

Medidas: 40 px, padding 0 12 e 14 px entre colunas.

### Horário

Um horário livre para escolher, agrupado em Manhã e Tarde, depois de escolhidos o serviço, a
pessoa e o dia (novo agendamento e remarcar). Mostre os ocupados só quando ajudam a entender o dia.

- Livre: fio `ds-contorno-forte`. Escolhido: `aria-pressed="true"`, fundo `ds-acao` com
  `ds-acao-texto`. Ocupado: fundo `ds-faixa`, sem fio, `ds-texto-3` riscado e
  `aria-label="10:30, ocupado"`.
- Se alguém ocupou o horário enquanto a pessoa olhava, o erro chega ao salvar: "Esse horário
  acabou de ser ocupado. Escolha outro."

Medidas: 36 px, no mínimo 72 de largura, padding 0 12, raio 10, texto 500 13 tabular e 12 px entre
horários.

### Peças da Agenda

Desenhadas só dentro da tela da Agenda, na proposta. Quando forem para o código, usam estas
medidas:

- **Casca:** `ds-fundo` atrás de tudo, topo de 48 px e menu de 60 na casca. O conteúdo fica num
  painel branco com fio de 1 px em cima e à esquerda e canto superior esquerdo de 16.
- **Menu de ícones:** itens de 38 × 38, raio 10 e 8 px entre eles, com ícone de 20 em traço 1,5 e
  `ds-icone`. Hover em `ds-menu-hover`; ativo em `ds-menu-ativo`, com o ícone em `ds-tinta`. O
  nome aparece numa dica escura ao lado, no hover e no foco. Configurações fica embaixo.
- **Topo:** a empresa à esquerda (logo de 22 px, nome 13/500 e seta, que abre o menu suspenso), a
  busca de 320 px no centro, só com a borda em tinta no foco, e a conta à direita, num avatar
  cinza.
- **Barra da tela:** 64 px, padding 32 à esquerda e fio embaixo de ponta a ponta. Título 18/26 e,
  embaixo, só o que pede ação: o total da semana e quantos ainda não responderam. Controles na
  ordem Hoje, anterior, próxima, Dia e Semana, equipe e Novo horário.
- **Cabeçalho dos dias:** 48 px, fixo ao rolar, com o dia em 13 px e o número em 14/500. Hoje: o
  dia em `ds-tinta`, o número num círculo `ds-acao` de 26 px e a coluna em `ds-hoje`. As linhas
  verticais continuam as da grade, sem quebra.
- **Grade:** uma hora tem 72 px e meia hora, 36, com as linhas contínuas entre as colunas. Horas em
  13 px `ds-texto-3`, centradas na linha, com 12 px de folga em cima e embaixo. Rola por dentro, de
  08:00 a 19:00, de segunda a sábado; o cabeçalho e a barra ficam parados.
- **Bloco:** raio 8, 4 px de folga de cada lado e 2 entre blocos seguidos, contorno `ds-contorno`
  com `ds-sombra`, padding 6 × 8, texto 13/18 e ícone de 13. Mostra só a hora de início e o nome. O
  de 30 min tem 34 px e uma linha, com o sobrenome abreviado; o de 45 min ou mais traz a hora em
  cima e o nome embaixo. O fundo é branco no claro e #252324 no escuro, e vira token quando o bloco
  entrar no código.
- **Bloco por status:** Aguardando e Confirmado em branco, com ícone; Feito em `ds-faixa`, sem
  ícone; Faltou em `ds-faixa`, com ícone; Cancelado tracejado e riscado. Hover: contorno
  `ds-contorno-hover`. Selecionado: anel de 1,5 px em `ds-tinta`, fundo `ds-acao-suave` e a sombra
  forte.
- **Agora e intervalo:** uma linha de 1,5 px em `ds-tinta` na coluna de hoje, com um ponto de 7 px
  no começo e a hora numa pílula `ds-acao` na coluna das horas. O intervalo é uma faixa listrada,
  sem clique, com a palavra só na primeira coluna.
- **Novo horário no vazio:** no hover aparece um bloco tracejado de 30 min com "+ 13:30", e o
  clique abre o novo horário com o dia e a hora preenchidos. Não aparece sobre horário, intervalo
  ou passado.
- **Detalhe:** fechado por padrão. Abre com 360 px ao clicar num horário, empurrando a grade, e
  fecha no X ou no Esc, com o foco voltando ao horário. De cima para baixo: status, nome, telefone
  com o atalho para o WhatsApp, quando, serviço com profissional e valor, e uma frase sobre a
  conversa ("Lembrete entregue hoje, 08:30. Ainda sem resposta."). As ações ficam no rodapé, com
  fio de ponta a ponta: horário futuro tem Remarcar e Cancelar; passado, Agendar de novo.
- **Confirmar exclusão:** o único lugar do vermelho cheio, com "Manter" ao lado.

### Peças que faltam desenhar

Antes de usar numa tela, desenhe na proposta. O visual da v2.1 não volta.

Diálogo (fora a confirmação), estado vazio, esqueleto, tabela, gráfico, interruptor, avatar como
peça própria, calendário de dias, opção em cartão, passo numerado, faixa de horário, aviso de
atenção e de sucesso, escolha de empresa e a casca no celular.

## Padrões de tela

Toda tela nova usa um destes. Se nenhum serve, é decisão de sistema: proponha antes. A estrutura
vem da v2.1; as medidas são as da v3.

| Padrão | Estrutura | Exemplos |
|---|---|---|
| A. Lista + painel | casca · cabeçalho (título, resumo e o principal "Novo …") · aviso opcional · lista com busca e segmento · painel de edição ao lado | Clientes, Serviços, Equipe |
| B. Grade de tempo + detalhe | casca · barra com os filtros (Hoje, setas, Dia e Semana, equipe) · grade de horas · painel de detalhe | Agenda |
| C. Formulário em passos + resumo | casca com trilha · cabeçalho sem ação · passos · resumo ao lado, com o horário em destaque, a ação principal, uma frase sobre o WhatsApp e o erro de horário ocupado | Novo agendamento |
| D. Edição por linha + prévia | casca · abas por pessoa · uma linha por item e barra de salvar no rodapé ("Alterações não salvas", Descartar, Salvar …) · prévia ao lado | Disponibilidade, Configurações |
| E. Relatório | casca · cabeçalho com as setas de mês · filtro de pessoa · números em destaque · gráfico · tabelas | Financeiro |
| F. Página pública | sem casca · cabeçalho do salão · próximo horário com duas ações · lista · rodapé com a logo | Meus horários |
| G. Painel de marca | formulário de 400 px + painel de marca | Entrar, Cadastro |

## Checklist de estados

Para cada tela, escreva em cada item: aplica (e o que mostra), não aplica (por quê) ou pergunta
aberta.

- **Dados:** padrão · vazio de primeira vez (estado vazio com ação) · vazio por filtro ("Nenhum
  cliente com 'Paulo'" e Limpar busca) · um item · muitos (rolagem, contagem no filtro) · texto
  longo (nome de 40 letras, R$ 12.345,00, observação de 300 caracteres) · campo opcional ausente
  ("—").
- **Sistema:** primeiro carregamento (esqueleto) · atualização (sem esqueleto) · falha ao carregar
  (aviso de erro e "Tentar de novo") · sem conexão · sessão expirada · conflito 409 (horário
  ocupado) · 404 ou outra empresa.
- **Pessoas:** administrador × prestador (o que some) · removido, com "(removido)" · a própria
  pessoa, com "(você)" · cliente sem WhatsApp.
- **WhatsApp:** enviada · entregue · não entregue · cliente respondeu.
- **Atividade:** salvando (o botão mantém a largura) · salvo (aviso de sucesso ou o painel
  atualiza) · alterações não salvas · confirmação antes de ação sem volta.
- **Tema e largura:** claro e escuro, e a tela estreita (390 px).

## Mudando o sistema

1. Veja se um componente existente, com uma variante, resolve. Prefira reusar.
2. Peça nova é desenhada na proposta, com regra, exemplo real e estados. Cor nova vira token com o
   contraste conferido; ícone novo segue a regra de ícones.
3. Liste as telas e os arquivos de código que usam a peça.
4. Leve a mudança e a lista para a aprovação de quem cuida do produto.
5. Aprovado: no mesmo PR vão o código, o `tokens.css` e este arquivo, com uma linha no
   [registro de decisões](#registro-de-decisões).

## Telas desenhadas na proposta

**Agenda da semana**, em 1440 × 900, de onde saem as medidas das [peças da Agenda](#peças-da-agenda).
A revisão de UX dela tem uma regra: cada coisa na tela ajuda a ver a semana de relance ou a marcar
em menos de 30 segundos, ou sai. Por isso a grade usa a largura toda e o detalhe começa fechado; a
barra mostra um número só, o de quem ainda não respondeu; o bloco mostra hora e nome, porque a
altura já diz a duração; o ícone aparece só quando diz algo (o feito não tem); o detalhe fala em
frases, e o histórico vai para a ficha do cliente; e o atalho do WhatsApp fica colado no telefone.

**Entrar e Cadastro.**

- **Entrar:** só e-mail e senha, sem escolher empresa. Com uma empresa, abre a Agenda.
- **Cadastro:** duas etapas, com "Etapa 1 de 2" no alto. Na 1, a conta (nome, e-mail e senha) e
  Continuar, que só confere os campos. Na 2, a empresa (nome, endereço da página e contato), a ação
  de criar e Voltar. Nada é gravado antes do fim: é um `POST /api/signup` só. Voltar mantém o que
  foi digitado. O endereço nasce do nome da empresa, sem acento e com hífen, e para de acompanhar
  quando a pessoa o edita.
- **Erros:** credencial errada vira o aviso no topo; o e-mail fica, a senha é apagada e o foco vai
  para ela. Muitas tentativas mostram o tempo do `Retry-After`. O e-mail já usado só aparece no
  fim: a tela volta para a etapa 1 com o erro no campo e o atalho "Entrar com este e-mail", sem
  perder a empresa. Endereço já usado: o erro fica no próprio campo, com o texto selecionado para
  trocar. Campo vazio, e-mail sem @ ou senha curta: erro no próprio campo, com o foco no primeiro.
- **Enviando:** a ação mantém a largura e mostra o indicador, e os campos ficam travados até a
  resposta.

## Registro de decisões

| Data | Decisão | Por quê |
|---|---|---|
| 17/11/2026 | v2.1: ameixa, Nunito e Hanken Grotesk, raio 24 | substituída pela v3 |
| 19/11/2026 | Proposta v3: cinza na estrutura e rosa na interação, Geist, Lucide e degradê claro nas peças de marca | a v2.1 ficava redonda e pesada, com caixa dentro de caixa |
| 20/11/2026 | Fundações e componentes da v3 aprovados | |
| 20/11/2026 | O ponto da marca sai dos ícones e fica na logo e nas ilustrações | em todo ícone, ele vira enfeite |
| 20/11/2026 | Contato da empresa obrigatório na etapa 2 do Cadastro, que a proposta não desenha | aparece na página pública: é por ele que o cliente cancela |
| 24/11/2026 | Logo em rosa claro (`ds-acao`) nos dois temas | o rosa claro é a cor principal da marca |
| 24/11/2026 | O painel de marca some abaixo de 1148 px | 560 da coluna, 576 do painel e 12 de margem não cabem lado a lado |
| 24/11/2026 | Este arquivo volta a ser a referência de design do repo, transcrito da proposta. Saem o `Design.html` e o guia e a auditoria da v2.1 (`docs/design-system/`), que ficam no histórico do Git | um texto só, que aparece no diff do PR |
| 25/11/2026 | Ícone do app: símbolo rosa sobre quadrado escuro | o símbolo segue rosa, como na logo, e o contraste fica em cerca de 9:1 |

## Em aberto

Da proposta, ainda sem decisão:

- **Wordmark:** redesenhar só o nome numa sans neutra, feito por quem cuida da marca. Até lá, a
  interface usa só o símbolo.
- **Menu de ícones ou com nome:** a recomendação é ícones, com o nome no hover e no leitor de tela,
  revendo depois do primeiro uso real.
- **Status no bloco:** só o ícone, com a palavra no detalhe e no leitor de tela, ou uma legenda
  curta na barra.
- **Domingo e dias fechados:** somem ou aparecem em cinza quando o salão abre em escala.
- **Visão do prestador:** falta desenhar a Agenda com uma pessoa só.
- **Tamanho mínimo da senha:** a tela pede 8 caracteres, como no desenho, mas os requisitos não
  fixam um mínimo.
- **Domínio do link:** o campo mostra só "agendar/", porque o domínio ainda não está definido.
- **Esqueci a senha:** não está no backlog, então a tela não oferece.
- **Escolha de empresa:** a tela de quem tem mais de uma empresa não está desenhada.
- **Casca no celular:** a proposta só desenha o desktop.
