import { AnimatePresence, motion } from 'motion/react'
import { useEffect, useState } from 'react'
import { AssistantAvatar } from './AssistantAvatar'

const phases = [
  'Entendendo a pergunta',
  'Escolhendo as ferramentas',
  'Consultando vendas e avaliações',
  'Cruzando os dados',
  'Escrevendo a resposta',
]

/** The agent does not stream its steps, so the indicator walks through the typical phases. */
export function ThinkingIndicator() {
  const [phase, setPhase] = useState(0)

  useEffect(() => {
    const timer = setInterval(() => setPhase((current) => Math.min(current + 1, phases.length - 1)), 2600)
    return () => clearInterval(timer)
  }, [])

  return (
    <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0 }} className="flex gap-4">
      <AssistantAvatar thinking />
      <div className="flex h-10 items-center gap-3">
        <div className="flex gap-1">
          {[0, 1, 2].map((dot) => (
            <motion.span
              key={dot}
              className="h-1.5 w-1.5 rounded-full bg-tomato-400"
              animate={{ y: [0, -5, 0], opacity: [0.4, 1, 0.4] }}
              transition={{ duration: 0.9, repeat: Infinity, delay: dot * 0.15 }}
            />
          ))}
        </div>
        <AnimatePresence mode="wait">
          <motion.span
            key={phase}
            initial={{ opacity: 0, y: 6 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -6 }}
            transition={{ duration: 0.25 }}
            className="text-shimmer text-sm"
          >
            {phases[phase]}…
          </motion.span>
        </AnimatePresence>
      </div>
    </motion.div>
  )
}
