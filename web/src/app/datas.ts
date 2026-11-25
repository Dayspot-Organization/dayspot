// A API fala em UTC (ISO 8601); o painel, no fuso do navegador; a página pública, no fuso da empresa.
// Formatos da tela (DESIGN.md › Como escrevemos): "quarta, 10h" em frase; 10:00 em grade e lista;
// "18 de novembro" em data.

/** Date no formato "AAAA-MM-DD" do <input type="date">, no dia local (toISOString daria o dia em UTC). */
export function diaLocal(d: Date): string {
  const p = (n: number) => String(n).padStart(2, "0");
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`;
}

/** "AAAA-MM-DD" + n dias; setDate lida com virada de mês e horário de verão. */
export function somarDias(dia: string, n: number): string {
  const d = new Date(`${dia}T00:00`);
  d.setDate(d.getDate() + n);
  return diaLocal(d);
}

/** Meia-noite local do dia em ISO UTC. "AAAA-MM-DDTHH:MM" sem fuso é lido como hora local. */
export const inicioDoDia = (dia: string) => new Date(`${dia}T00:00`).toISOString();

/** Segunda-feira da semana do dia (a grade da agenda começa na segunda). */
export function segundaDaSemana(dia: string): string {
  const semana = new Date(`${dia}T00:00`).getDay(); // 0 = domingo
  return somarDias(dia, semana === 0 ? -6 : 1 - semana);
}

/** 0 = domingo … 6 = sábado. */
export const diaDaSemana = (dia: string) => new Date(`${dia}T00:00`).getDay();

const fmtDiaCurto = new Intl.DateTimeFormat("pt-BR", { weekday: "short" });
const fmtDiaLongo = new Intl.DateTimeFormat("pt-BR", { weekday: "long" });
const fmtDiaMes = new Intl.DateTimeFormat("pt-BR", { day: "numeric", month: "long" });
const fmtMes = new Intl.DateTimeFormat("pt-BR", { month: "long" });
const fmtHora = new Intl.DateTimeFormat("pt-BR", { hour: "2-digit", minute: "2-digit" });

const maiuscula = (s: string) => s.charAt(0).toUpperCase() + s.slice(1);
const semPonto = (s: string) => s.replace(".", "");

/** "ter" */
export const diaCurto = (dia: string) => semPonto(fmtDiaCurto.format(new Date(`${dia}T00:00`)));
/** "terça" (sem "-feira") */
export const diaLongo = (dia: string) => fmtDiaLongo.format(new Date(`${dia}T00:00`)).replace("-feira", "");
/** "17" */
export const numeroDoDia = (dia: string) => String(new Date(`${dia}T00:00`).getDate());
/** "17 de novembro" */
export const diaEMes = (dia: string) => fmtDiaMes.format(new Date(`${dia}T00:00`));
/** "Terça, 17 de novembro" */
export const dataLonga = (dia: string) => `${maiuscula(diaLongo(dia))}, ${diaEMes(dia)}`;
/** "Novembro" */
export const nomeDoMes = (dia: string) => maiuscula(fmtMes.format(new Date(`${dia}T00:00`)));

/** "09:30", no fuso do navegador. */
export const formatarHora = (iso: string | Date) => fmtHora.format(new Date(iso));

/** "09:30" vira "9h30"; "10:00" vira "10h": o horário dentro de frase. */
export const horaEmFrase = (hora: string) => `${Number(hora.slice(0, 2))}h${hora.endsWith(":00") ? "" : hora.slice(3)}`;

/** "hoje", "amanhã", "quarta" (até 7 dias) ou "18 de novembro": o dia dentro de frase. */
export function diaEmFrase(dia: string, hoje: string): string {
  if (dia === hoje) return "hoje";
  if (dia === somarDias(hoje, 1)) return "amanhã";
  if (dia > hoje && dia < somarDias(hoje, 7)) return diaLongo(dia);
  return diaEMes(dia);
}

/** Dia e hora de um instante no fuso informado: { dia: "AAAA-MM-DD", hora: "HH:MM" }. */
export function noFuso(instante: number | string, fuso: string) {
  const partes = new Intl.DateTimeFormat("en-CA", {
    timeZone: fuso,
    hourCycle: "h23",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  }).formatToParts(new Date(instante));
  const p = (tipo: Intl.DateTimeFormatPartTypes) => partes.find((x) => x.type === tipo)?.value;
  return { dia: `${p("year")}-${p("month")}-${p("day")}`, hora: `${p("hour")}:${p("minute")}` };
}

/** Instante (ms) em que o relógio do fuso marca esse dia e hora. */
export function instanteNoFuso(dia: string, hora: string, fuso: string): number {
  const alvo = Date.parse(`${dia}T${hora}:00Z`);
  // Duas passadas: o deslocamento certo é o do instante final, que pode cair depois de uma troca de horário de verão.
  let t = alvo;
  for (let i = 0; i < 2; i++) {
    const lido = noFuso(t, fuso);
    t += alvo - Date.parse(`${lido.dia}T${lido.hora}:00Z`);
  }
  return t;
}

/** Minutos desde a meia-noite local. */
export function minutosDoDia(iso: string | Date): number {
  const d = new Date(iso);
  return d.getHours() * 60 + d.getMinutes();
}

export const minutosEntre = (inicio: string, fim: string) =>
  Math.round((Date.parse(fim) - Date.parse(inicio)) / 60_000);

/** "30 min", "1 h", "1 h 30" */
export function duracaoEmTexto(minutos: number): string {
  if (minutos < 60) return `${minutos} min`;
  const h = Math.floor(minutos / 60);
  const m = minutos % 60;
  return m ? `${h} h ${String(m).padStart(2, "0")}` : `${h} h`;
}

/** "R$ 70,00" */
const fmtDinheiro = new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" });
export const dinheiro = (centavos: number) => fmtDinheiro.format(centavos / 100).replace(/\u00a0/g, " ");

/** Iniciais do avatar: primeira letra do primeiro e do último nome. */
export function iniciais(nome: string): string {
  const partes = nome.trim().split(/\s+/).filter(Boolean);
  if (partes.length === 0) return "?";
  const primeira = partes[0][0];
  const ultima = partes.length > 1 ? partes[partes.length - 1][0] : "";
  return (primeira + ultima).toUpperCase();
}

/** "a", "a e b", "a, b e c" */
export function juntar(partes: string[]): string {
  if (partes.length <= 1) return partes.join("");
  return `${partes.slice(0, -1).join(", ")} e ${partes[partes.length - 1]}`;
}
