import { useCallback } from 'react'
import { api } from '../api/client'
import type { HealthStatus } from '../api/types'
import { useApi } from '../hooks/useApi'

const labels: Record<string, string> = { postgres: 'Banco de dados', redis: 'Cache', gemini: 'IA (Gemini)' }

const colors: Record<HealthStatus, string> = {
  Healthy: 'bg-basil-400',
  Degraded: 'bg-saffron-300',
  Unhealthy: 'bg-tomato-500',
}

export function HealthPanel() {
  const load = useCallback(() => api.health(), [])
  const { data, failed } = useApi(load, 30_000)
  const checks = data ? Object.entries(data.checks) : []

  return (
    <section className="space-y-2">
      <h2 className="text-[11px] font-semibold uppercase tracking-[0.14em] text-cream-500">Status</h2>
      {failed && <p className="text-sm text-tomato-400">API fora do ar</p>}
      <ul className="space-y-1.5">
        {checks.map(([name, check]) => (
          <li key={name} className="flex items-center justify-between text-sm" title={check.description ?? undefined}>
            <span className="text-cream-200">{labels[name] ?? name}</span>
            <span className="relative flex h-2.5 w-2.5">
              {check.status === 'Healthy' && (
                <span className={`absolute inline-flex h-full w-full animate-ping rounded-full opacity-50 ${colors[check.status]}`} />
              )}
              <span className={`relative inline-flex h-2.5 w-2.5 rounded-full ${colors[check.status]}`} />
            </span>
          </li>
        ))}
      </ul>
    </section>
  )
}
