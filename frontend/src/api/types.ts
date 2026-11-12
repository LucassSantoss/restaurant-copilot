// Contracts of the RestaurantCopilot API (see the OpenAPI document at /openapi/v1.json).

export interface ChatSource {
  kind: 'tool' | 'document'
  reference: string
  description: string
}

export interface ToolCall {
  tool: string
  arguments: Record<string, unknown>
  succeeded: boolean
  error: string | null
}

export interface ChatResponse {
  answer: string
  sources: ChatSource[]
  toolCalls: ToolCall[]
  usage: { inputTokens: number; outputTokens: number }
  cached: boolean
}

export type ReviewTheme = 'Delay' | 'Taste' | 'Packaging' | 'Price' | 'Service'

export interface ReviewInsights {
  from: string
  to: string
  totalReviews: number
  classifiedReviews: number
  averageRating: number | null
  sentiment: { positive: number; neutral: number; negative: number }
  themes: { theme: ReviewTheme; mentions: number; negative: number }[]
  mostComplainedDishes: { menuItemId: string; name: string; negativeReviews: number }[]
}

export type HealthStatus = 'Healthy' | 'Degraded' | 'Unhealthy'

export interface HealthReport {
  status: HealthStatus
  checks: Record<string, { status: HealthStatus; description: string | null }>
}
