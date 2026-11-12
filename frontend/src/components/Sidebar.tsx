import { motion } from 'motion/react'
import { Plus } from 'lucide-react'
import { BrandMark } from './BrandMark'
import { HealthPanel } from './HealthPanel'
import { InsightsCard } from './InsightsCard'

export function Sidebar({ onNewChat, canReset }: { onNewChat: () => void; canReset: boolean }) {
  return (
    <motion.aside
      initial={{ opacity: 0, x: -16 }}
      animate={{ opacity: 1, x: 0 }}
      transition={{ duration: 0.5, ease: 'easeOut' }}
      className="hidden w-80 shrink-0 flex-col gap-7 overflow-y-auto border-r border-ink-700/70 bg-ink-900/40 p-6 backdrop-blur-xl lg:flex"
    >
      <BrandMark />

      <motion.button
        type="button"
        onClick={onNewChat}
        disabled={!canReset}
        whileTap={{ scale: 0.97 }}
        className="flex items-center justify-center gap-2 rounded-2xl border border-ink-600 bg-ink-800/70 px-4 py-2.5 text-sm font-medium text-cream-200 transition-colors hover:border-tomato-400/50 hover:text-cream-50 disabled:opacity-40"
      >
        <Plus className="h-4 w-4" /> Nova conversa
      </motion.button>

      <InsightsCard />
      <div className="mt-auto">
        <HealthPanel />
      </div>
    </motion.aside>
  )
}
