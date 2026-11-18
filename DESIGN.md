# Design — Dayspot

Referência visual do `web/`. A identidade é a **Dayspot** (pacote de assets da marca,
folha de contato `assets-dev` e a "Identidade visual — referência rápida"): ameixa,
ponto pêssego e Nunito. As cores do escuro seguem as versões `-escuro` dos assets. As telas de referência continuam as do canvas anterior (estrutura, medidas e
componentes), agora com as cores e a fonte abaixo. Mudou a identidade, muda este
arquivo no mesmo PR que levar a mudança para o código.

Direção **Grade**: calma e precisa, como uma grade de horários. Cada região tem uma
superfície, e a cor aparece só em ação, seleção e exceção.

## Regras

1. **Ponto é horário livre. Pílula é horário reservado** e cresce com a duração.
2. **O normal não leva selo.** Só a exceção ganha cor (confirmado, ativo e vinculado
   não têm selo).
3. **Um nível de cartão.** Dentro dele, separe com espaço e preenchimento. Borda só em
   campo.
4. **Ameixa na marca, em ação e seleção; pêssego só no ponto do símbolo e no hover do
   menu; verde só no sucesso.** O resto é tinta e cinza quente.
5. A **nav** (menu lateral) é a única superfície de cor sólida.
6. **Top-bar** em `superficie` com linha fina embaixo: branca no claro e um tom acima
   do fundo no escuro, para se destacar dele nos dois temas (o canvas usava a cor da
   nav no escuro; mudou em 17/10/2026). Página atual à esquerda, tema, busca e conta à
   direita.
7. O conteúdo centraliza em até **1040 px**.
8. Escuro sem brilho, sombra ou degradê: as superfícies sobem de tom.
9. **Mobile first.** O CSS parte do celular (390 px) e cresce com `@media (min-width: …)`.
   Toda tela é desenhada e testada primeiro no celular.

## Estrutura

| | Celular (até 1023 px) | Desktop (1024 px ou mais) |
|---|---|---|
| Menu | Gaveta de 304 px (`<dialog>` modal) aberta pelo botão de menu; logo e "Fechar" no topo | Menu lateral fixo de 232 px em `nav`; logo (símbolo 28, nome 20) no topo |
| Top-bar | 56 px: menu, título (16/500), tema, busca e conta | 64 px: título (15/500) à esquerda; tema, busca e conta à direita |
| Ação principal | Botão flutuante de 56 px, raio 16, no canto inferior direito | Botão primário no cabeçalho da página |
| Conteúdo | padding 16 × 20 | padding 24 × 40, centralizado em até 1040 px |

- O **título da página** fica na top-bar, como trilha ("Agenda › Novo agendamento"),
  e não se repete dentro da página.
- O menu mostra só as telas que existem: cada item entra com o card da sua tela.
- Empresa ativa, busca e conta entram com os seus cards (sessão na Fatia 2).
- Menu recolhido (72 px, só ícones): quando o calendário da 7.1 precisar de espaço.
- **Tema:** botão redondo de 36 px (`afundado`) no canto direito da top-bar, com o
  ícone do tema em uso (sol ou lua); o clique alterna entre claro e escuro. Sem
  escolha, segue o sistema (`prefers-color-scheme`). A escolha fica no `localStorage`
  e é aplicada em `<html data-tema>` antes do Angular montar, para não piscar.

## Tokens de cor

Mesmo nome nos dois temas. No código viram variáveis CSS (`--fundo`, `--marca`...)
com `light-dark(claro, escuro)`: o `color-scheme` do `<html>` escolhe o lado (sistema,
ou o tema escolhido no botão da top-bar).

| Token | Claro | Escuro | Uso |
|---|---|---|---|
| `fundo` | `#FBF7F5` | `#1A1417` | fundo do app (papel) |
| `superficie` | `#FFFFFF` | `#251D21` | cartão e folha |
| `afundado` | `#F4ECEA` | `#30272B` | trilho e botão secundário |
| `linha` | `#EEE3E0` | `#3A3035` | divisória |
| `linha-forte` | `#DCCFCB` | `#463A40` | borda de campo e ponto livre |
| `texto` | `#1A1417` | `#F3E9EC` | texto principal (tinta) |
| `texto-2` | `#6B5F64` | `#B3A2A9` | secundário, rótulo de apoio |
| `texto-3` | `#8E807F` | `#857880` | placeholder, desabilitado |
| `nav` | `#86315C` | `#3A1A2B` | menu lateral, logo claro |
| `marca` | `#86315C` | `#EE9DBF` | traço do símbolo fora da nav |
| `ponto` | `#F4A08A` | `#F4A08A` | ponto do símbolo, hover do ícone no menu |
| `sucesso` | `#1F6B4A` | `#6FCB9C` | texto e ícone de sucesso |
| `sucesso-suave` | `#E3F3EA` | `#12291F` | fundo do aviso de sucesso |
| `destaque` | `#86315C` | `#EE9DBF` | ação, seleção, "agora" |
| `destaque-texto` | `#86315C` | `#EE9DBF` | link (hover: `#5E1F40` / `#F4A08A`) |
| `agendado` | `#F7E6E3` | `#3D2632` | bloco de agendamento (hora em `destaque`) |
| `sobre-cor` | `#FFFFFF` | `#1A1417` | texto sobre `destaque` ou `nav` |
| `atencao` | `#935A00` | `#E3B062` | ponto e texto de atenção |
| `atencao-suave` | `#FCF1DC` | `#2B2214` | fundo do selo e aviso de atenção |
| `perigo` | `#B3261E` | `#F0908A` | texto e ação destrutiva, erro |
| `perigo-suave` | `#FBEAE8` | `#2E1A1D` | fundo do selo e aviso de erro |

Na nav (ameixa nos dois temas) o texto do menu é `#F3DDE6`, o item ativo leva branco a
16 % e o hover branco a 10 %.

Foco: anel de 3 px em `destaque` com transparência (`rgba(134,49,92,.22)` no claro,
`rgba(238,157,191,.32)` no escuro) e borda do campo em `destaque`.

## Tipografia

**Nunito** em tudo: logo, títulos e texto. Arredondada como o símbolo.

| Papel | Fonte | Tamanho / altura |
|---|---|---|
| Título de página (top-bar) | Nunito 700 | 15 / 24 no desktop, 16 / 24 no celular |
| Título de tela sem top-bar | Nunito 800 | 24 / 32, espaçamento −2% |
| Número de destaque (dinheiro) | Nunito 800 | 28 |
| Título de seção | Nunito 700 | 15 / 22 |
| Texto e controles | Nunito 400 (700 em botão) | 14 / 20 |
| Rótulo e descrição | Nunito 600 | 13 / 18 |
| Metadado e selo | Nunito 600 | 12 |

Hora e dinheiro com algarismos tabulares (`font-variant-numeric: tabular-nums`).

## Forma

- Raio **8** em controle, **12** em cartão, **16** em folha (painel lateral, modal);
  **pílula** (`999px`) em horário e selo. Bloco de agendamento: 10.
- Controles com **36 px** de altura; segmento (Dia/Semana) 30 px dentro de um trilho
  `afundado` com 2 px de folga.

## Componentes

| Componente | Como é |
|---|---|
| Botão primário | fundo `destaque`, texto `sobre-cor`, 500, padding 0 14, ícone opcional à esquerda |
| Botão secundário | fundo `afundado`, texto `texto`, sem borda |
| Botão terciário | transparente, texto `texto-2`; destrutivo em `perigo` |
| Botão desabilitado | fundo `afundado`, texto `texto-3` |
| Campo (`input`, `select`) | `superficie`, borda 1 px `linha-forte`, padding 0 12; rótulo acima, 13/500, 6 px de distância |
| Horário livre | pílula de 32 px em `afundado`; selecionado em `destaque` com `sobre-cor` |
| Agendamento | bloco `agendado`, hora e serviço 12/600 em `destaque`, nome 12 |
| Selo | pílula de 22 px, 12/500, fundo `*-suave` e texto da cor do estado; neutro em `afundado`/`texto-2` |
| Aviso | parágrafo com ícone, padding 10 14, raio 10, fundo `*-suave`, texto da cor (`marca`, `atencao`, `perigo`) |
| Interruptor | 36 × 20, ligado em `destaque`, desligado em `linha-forte` |

### Status do agendamento

| Status | Visual |
|---|---|
| Confirmado | bloco `agendado`, sem selo |
| A concluir | bloco `agendado` + ponto `atencao` no canto; selo "A concluir" |
| Concluído | bloco `afundado`, texto `texto-2`, ícone de check |
| Cancelado | `superficie` com borda tracejada `linha-forte`, texto riscado; **oculto por padrão** ("Mostrar cancelados") |

## Marca

Símbolo: um "d" arredondado (haste, base e curva subindo à direita) com um ponto pêssego
em cima: um lugar marcado no dia. O desenho vem de `simbolo.svg` (quadro `4 0 56 64`);
no menu usa-se `simbolo-negativo.svg`. Nome **dayspot** em Nunito 800, minúsculo, espaçamento −2%. Tagline: "O seu
lugar no dia."

- Ponto sempre em `ponto` (pêssego). Traço em `marca` sobre fundo claro e em `#FBF7F5`
  sobre ameixa ou escuro, inclusive no menu lateral e na gaveta (`nav`).
- Favicon: `favicon.svg` é o símbolo inteiro em ameixa, rosa (`#EE9DBF`) quando o
  sistema está no escuro; `favicon.ico` (16/32/48) e `favicon-32x32.png` para
  navegadores sem SVG. Ícone de app (fundo ameixa, símbolo claro, ponto pêssego) em
  `apple-touch-icon.png`, `icon-192.png`, `icon-512.png` e `icon-maskable-512.png`
  (`manifest.webmanifest`). Compartilhamento: `og-image.png` (1200 × 630) e
  `twitter-card.png` (1200 × 600), todos em `web/public/`.
- Tamanhos: 16 (aba), 24 (barra do celular), 28 (menu lateral, nome a 20, 8 px entre os
  dois), 48 (entrar, versão empilhada com nome a 24).
- Ícones de interface: traço `currentColor` de 2,25 no quadro de 24, com o ponto em
  `--ponto` (o `--icone-ponto` dos assets). Cada ícone entra com a tela dele no menu.
- Ilustrações de estado (vazio e erro): traço, linha e preenchimento em cinza quente e o
  ponto em ameixa; as cores vêm das variáveis `--ilu-*` do `styles.css`, então um único
  desenho serve aos dois temas. Em uso: `agenda-vazia` (agenda sem horários) e
  `sem-clientes`. As demais (`sem-servicos`, `sem-horarios`, `erro`, `nao-encontrado`),
  o painel e a faixa do login e o padrão da semana entram com os cards das suas telas.
- O degradê da marca (pêssego claro `#FDD4B1` → `#F4A08A` → `#C95F7C` → ameixa) é das
  peças de divulgação, login e Telegram; no app ele não entra (regra 8).
- Não pode: girar o símbolo, mudar o ponto de lugar ou de cor, ponto em âmbar ou
  vermelho (são cores de estado), sombra, contorno ou nome esticado.

## Telas de referência

Boards do canvas e o card que realiza cada um.

| Board | Card |
|---|---|
| Agenda da semana, detalhe aberto (claro, escuro, menu recolhido) | 7.1, 7.2, 5.4 |
| Agenda do dia no celular, menu aberto no celular | 7.1 |
| Mensagens do bot no Telegram | Fatia 3 |
| Financeiro do mês (administrador) | 7.5 |
| Prestadores, confirmar remoção | 2.20, 5.8 |
| Serviços e preços | 6.5a |
| Clientes e Telegram | 3.8, 3.9 |
| Disponibilidade da semana | 6.5b |
| Entrar | 2.10 |

**Fatia 1:** a lista de `/agendamentos` (1.10) é provisória, e o calendário da 7.1
a substitui. O formulário de `/agendamentos/novo` (1.11) não tem board: usa os
componentes acima (campos, botão primário, aviso de erro para o 409). A estrutura
(menu, top-bar, gaveta, tema) já vale desde a 1.10, com o menu só com a Agenda.

## Acessibilidade

- Texto com contraste AA (4,5:1). Os pares de texto e estado acima passam, conferidos
  em 16/11/2026. A exceção é `texto-3` (3,8:1 no claro, 3,9:1 no escuro), que fica
  só em placeholder e desabilitado e nunca em texto que precisa ser lido. Cor nova
  precisa ser conferida.
- Estado nunca só por cor: selo tem texto, cancelado é riscado e concluído tem ícone.
- Foco sempre visível (o anel acima); grupos de seleção com `role`/`aria-pressed` ou
  `aria-checked`.
- Ícone sem texto leva `aria-label`.
