import { motion } from 'motion/react'
import { Star, TriangleAlert } from 'lucide-react'
import { useCallback } from 'react'
import { api } from '../api/client'
import { useApi } from '../hooks/useApi'
import { formatDate, formatNumber, themeLabels } from '../lib/format'

/** Review pulse of the last 30 days, straight from /api/reviews/insights. */
export function InsightsCard() {
  const load = useCallback(() => api.insights(), [])
  const { data } = useApi(load)

  if (!data) {
    return <div className="h-64 animate-pulse rounded-3xl bg-ink-800/60" />
  }

  const { positive, neutral, negative } = data.sentiment
  const classified = Math.max(positive + neutral + negative, 1)
  const topThemes = data.themes.slice(0, 3)
  const maxMentions = Math.max(...topThemes.map((theme) => theme.mentions), 1)
  const worstDish = data.mostComplainedDishes[0]

  return (
    <motion.section
      initial={{ opacity: 0, y: 12 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.5, ease: 'easeOut' }}
      className="space-y-5 rounded-3xl border border-ink-600/60 bg-ink-800/50 p-5 backdrop-blur"
    >
      <header className="flex items-baseline justify-between">
        <h2 className="text-[11px] font-semibold uppercase tracking-[0.14em] text-cream-500">Avaliações</h2>
        <span className="text-[11px] text-cream-500">
          {formatDate(data.from)} – {formatDate(data.to)}
        </span>
      </header>

      <div className="flex items-end gap-3">
        <p className="font-display text-4xl font-semibold">{data.averageRating ? formatNumber(data.averageRating, 2) : '–'}</p>
        <div className="pb-1.5">
          <div className="flex gap-0.5">
            {[1, 2, 3, 4, 5].map((star) => (
              <Star
                key={star}
                className={`h-3.5 w-3.5 ${star <= Math.round(data.averageRating ?? 0) ? 'fill-saffron-300 text-saffron-300' : 'text-ink-600'}`}
              />
            ))}
          </div>
          <p className="mt-1 text-xs text-cream-500">{data.totalReviews} avaliações</p>
        </div>
      </div>

      <div>
        <div className="flex h-2 overflow-hidden rounded-full bg-ink-700">
          {[
            [positive, 'bg-basil-400'],
            [neutral, 'bg-cream-500'],
            [negative, 'bg-tomato-500'],
          ].map(([count, color], index) => (
            <motion.div
              key={color}
              className={color as string}
              initial={{ width: 0 }}
              animate={{ width: `${((count as number) / classified) * 100}%` }}
              transition={{ duration: 0.9, delay: 0.2 + index * 0.12, ease: 'easeOut' }}
            />
          ))}
        </div>
        <div className="mt-2 flex justify-between text-[11px] text-cream-500">
          <span>😊 {positive}</span>
          <span>😐 {neutral}</span>
          <span>😞 {negative}</span>
        </div>
      </div>

      {topThemes.length > 0 && (
        <ul className="space-y-2.5">
          {topThemes.map((theme, index) => (
            <li key={theme.theme} className="text-xs">
              <div className="mb-1 flex justify-between text-cream-200">
                <span>{themeLabels[theme.theme]}</span>
                <span className="text-cream-500">{theme.mentions} menções</span>
              </div>
              <div className="h-1.5 overflow-hidden rounded-full bg-ink-700">
                <motion.div
                  className="h-full rounded-full bg-gradient-to-r from-saffron-300 to-tomato-400"
                  initial={{ width: 0 }}
                  animate={{ width: `${(theme.mentions / maxMentions) * 100}%` }}
                  transition={{ duration: 0.8, delay: 0.4 + index * 0.1, ease: 'easeOut' }}
                />
              </div>
            </li>
          ))}
        </ul>
      )}

      {worstDish && (
        <div className="flex gap-3 rounded-2xl bg-tomato-500/10 p-3 ring-1 ring-tomato-500/25">
          <TriangleAlert className="mt-0.5 h-4 w-4 shrink-0 text-tomato-400" />
          <p className="text-xs leading-relaxed text-cream-200">
            <span className="font-semibold text-cream-50">{worstDish.name}</span> concentra {worstDish.negativeReviews}{' '}
            avaliações negativas no período.
          </p>
        </div>
      )}
    </motion.section>
  )
}
