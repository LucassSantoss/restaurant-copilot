import type { ReviewTheme } from '../api/types'

export const themeLabels: Record<ReviewTheme, string> = {
  Delay: 'Atraso',
  Taste: 'Sabor',
  Packaging: 'Embalagem',
  Price: 'Preço',
  Service: 'Atendimento',
}

export const toolLabels: Record<string, string> = {
  search_sales: 'Vendas',
  list_reviews: 'Avaliações',
  semantic_search: 'Busca semântica',
  generate_report: 'Relatório',
  query_database: 'Consulta SQL',
}

export function formatDate(isoDate: string): string {
  const [year, month, day] = isoDate.split('-')
  return `${day}/${month}/${year}`
}

export function formatNumber(value: number, fractionDigits = 0): string {
  return value.toLocaleString('pt-BR', { minimumFractionDigits: fractionDigits, maximumFractionDigits: fractionDigits })
}
