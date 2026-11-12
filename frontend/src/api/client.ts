import type { ChatResponse, HealthReport, ReviewInsights } from './types'

export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, init)
  const body = await response.json().catch(() => null)

  // /health answers 503 with a regular report when a dependency is down.
  if (!response.ok && !(path === '/health' && body)) {
    throw new ApiError(response.status, problemMessage(response.status, body))
  }

  return body as T
}

function problemMessage(status: number, body: { detail?: string; title?: string } | null): string {
  if (status === 503) {
    return 'A IA está indisponível no momento (limite do plano gratuito ou serviço fora do ar). Tente de novo em instantes.'
  }

  if (status === 400) {
    return 'A pergunta não é válida. Escreva algo entre 1 e 1000 caracteres.'
  }

  return body?.detail ?? body?.title ?? `Erro inesperado (${status}).`
}

export const api = {
  ask: (question: string) =>
    request<ChatResponse>('/api/chat', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ question }),
    }),

  insights: () => request<ReviewInsights>('/api/reviews/insights'),

  health: () => request<HealthReport>('/health'),
}
