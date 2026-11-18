// Dados de exemplo para a tela de agendamento enquanto a API não lista empresa, serviços e
// profissionais (o POST ainda usa SERVICO_FIXO e PRESTADOR_FIXO). Quando as rotas existirem,
// troque os métodos de Api que leem daqui por chamadas HTTP; os tipos já são os da tela.
import type { Empresa, Prestador, Servico } from "./index";

export const EMPRESA_EXEMPLO: Empresa = {
  nome: "Barbearia Lima",
  endereco: "Rua Augusta, 1200",
  telefone: "1140001234",
};

export const SERVICOS_EXEMPLO: Servico[] = [
  { id: "exemplo-corte", nome: "Corte", duracao_min: 30, preco_centavos: 4500 },
  { id: "exemplo-corte-barba", nome: "Corte + barba", duracao_min: 50, preco_centavos: 7000 },
  { id: "exemplo-barba", nome: "Barba", duracao_min: 20, preco_centavos: 3000 },
  { id: "exemplo-sobrancelha", nome: "Sobrancelha", duracao_min: 15, preco_centavos: 2000 },
  { id: "exemplo-platinado", nome: "Platinado", duracao_min: 120, preco_centavos: 18000 },
];

export const PRESTADORES_EXEMPLO: Prestador[] = [
  { id: "exemplo-lucas", nome: "Lucas Lima" },
  { id: "exemplo-ana", nome: "Ana Rocha" },
];
