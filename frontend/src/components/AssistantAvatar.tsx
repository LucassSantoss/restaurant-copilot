import { motion } from 'motion/react'
import { Sparkles } from 'lucide-react'

export function AssistantAvatar({ thinking = false }: { thinking?: boolean }) {
  return (
    <div className="relative h-10 w-10 shrink-0">
      {thinking && (
        <motion.span
          className="absolute inset-0 rounded-2xl bg-gradient-to-br from-saffron-300 to-tomato-500"
          animate={{ scale: [1, 1.35], opacity: [0.5, 0] }}
          transition={{ duration: 1.4, repeat: Infinity, ease: 'easeOut' }}
        />
      )}
      <div className="relative grid h-10 w-10 place-items-center rounded-2xl bg-gradient-to-br from-saffron-300 to-tomato-500 shadow-lg shadow-tomato-500/25">
        <Sparkles className="h-4.5 w-4.5 text-ink-950" strokeWidth={2.2} />
      </div>
    </div>
  )
}
