# Telas de referência · v2.1

As 13 telas do novo front-end, já normalizadas pelo sistema de design (17/11/2026). Cada arquivo
`.dc.html` é uma página HTML autocontida: abra no navegador para ver. Os estilos usam só os tokens
`--ds-*` (mesmos valores de [`../tokens.css`](../tokens.css)) e o logo vem de
[`../assets/logo-horizontal.svg`](../assets/logo-horizontal.svg). A linha `support.js` do `<head>` é do editor de design e pode
dar 404 fora dele, sem efeito na tela.

Regras e componentes: [`../README.md`](../README.md). Correções feitas: [`../auditoria.md`](../auditoria.md).

| Arquivo | Tela | Padrão | Largura × altura |
|---|---|---|---|
| [`Main.dc.html`](Main.dc.html) | Sistema visual: cores, tipografia, status, componentes, estados de interação, tema escuro | — | 1440 × 1960 |
| [`Entrar.dc.html`](Entrar.dc.html) | Entrar | G | 1440 × 900 |
| [`Cadastro.dc.html`](Cadastro.dc.html) | Cadastro da empresa (com erro de endereço já em uso) | G | 1440 × 900 |
| [`Agenda-semana.dc.html`](Agenda-semana.dc.html) | Agenda da semana + detalhe com mensagens do WhatsApp | B | 1440 × 1160 |
| [`Novo-agendamento-web.dc.html`](Novo-agendamento-web.dc.html) | Novo agendamento em passos + resumo | C | 1440 × 1290 |
| [`Clientes.dc.html`](Clientes.dc.html) | Clientes, estado dos avisos, edição | A | 1440 × 960 |
| [`Servicos.dc.html`](Servicos.dc.html) | Serviços e preços | A | 1440 × 960 |
| [`Disponibilidade.dc.html`](Disponibilidade.dc.html) | Disponibilidade por pessoa + prévia de horários livres | D | 1440 × 1000 |
| [`Prestadores.dc.html`](Prestadores.dc.html) | Equipe, convites e confirmação de remoção | A | 1440 × 1010 |
| [`Financeiro.dc.html`](Financeiro.dc.html) | Financeiro do mês (dados de exemplo) | E | 1440 × 1200 |
| [`Configuracoes.dc.html`](Configuracoes.dc.html) | Empresa e avisos pelo WhatsApp | D | 1440 × 1100 |
| [`Meus-horarios-cliente.dc.html`](Meus-horarios-cliente.dc.html) | Página do cliente aberta pelo link do WhatsApp | F | 1440 × 1060 |
| [`Estados.dc.html`](Estados.dc.html) | Vazios, erro, sessão expirada, não encontrado, carregando | — | 1440 × 1060 |

Os padrões A–G estão na seção 5 do README do sistema. Os dados (Barbearia Lima, Lucas, Ana,
Marina…) são fictícios. As versões de celular ainda não foram desenhadas.
