import { motion } from 'motion/react'
import { BarChart3, CalendarRange, Lightbulb, MessageSquareWarning, Package, Clock } from 'lucide-react'
import type { LucideIcon } from 'lucide-react'

const suggestions: { icon: LucideIcon; text: string }[] = [
  { icon: BarChart3, text: 'Qual foi meu faturamento na semana de 02/11/2026 a 08/11/2026 comparado à semana anterior?' },
  { icon: MessageSquareWarning, text: 'Quais pratos tiveram mais reclamações em outubro de 2026?' },
  { icon: Package, text: 'O que os clientes estão falando sobre a embalagem?' },
  { icon: Lightbulb, text: 'Sugira uma promoção para aumentar as vendas de terça-feira.' },
  { icon: Clock, text: 'Em qual hora do dia entram mais pedidos?' },
  { icon: CalendarRange, text: 'Faça um resumo do desempenho do restaurante em outubro de 2026.' },
]

export function EmptyState({ onPick }: { onPick: (question: string) => void }) {
  return (
    <div className="mx-auto flex max-w-3xl flex-col items-center px-4 pt-[12vh] text-center">
      <motion.span
        initial={{ opacity: 0, y: 8 }}
        animate={{ opacity: 1, y: 0 }}
        className="mb-5 inline-flex items-center gap-2 rounded-full border border-ink-600 bg-ink-800/60 px-3 py-1 text-xs text-cream-400 backdrop-blur"
      >
        <span className="h-1.5 w-1.5 rounded-full bg-basil-400" />
        Vendas, avaliações e cardápio, consultados em tempo real
      </motion.span>

      <motion.h1
        initial={{ opacity: 0, y: 16 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.6, delay: 0.05 }}
        className="font-display text-4xl font-semibold leading-tight tracking-tight sm:text-5xl"
      >
        O que você quer saber sobre o{' '}
        <span className="bg-gradient-to-r from-saffron-300 via-tomato-400 to-tomato-500 bg-clip-text text-transparent">
          seu restaurante
        </span>
        ?
      </motion.h1>

      <motion.p
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        transition={{ duration: 0.6, delay: 0.2 }}
        className="mt-4 max-w-xl text-cream-400"
      >
        Pergunte em português. O copiloto consulta os dados reais, cruza as informações e mostra de onde tirou cada resposta.
      </motion.p>

      <motion.ul
        initial="hidden"
        animate="visible"
        variants={{ visible: { transition: { staggerChildren: 0.07, delayChildren: 0.3 } } }}
        className="mt-10 grid w-full gap-3 text-left sm:grid-cols-2"
      >
        {suggestions.map(({ icon: Icon, text }) => (
          <motion.li
            key={text}
            variants={{ hidden: { opacity: 0, y: 14 }, visible: { opacity: 1, y: 0 } }}
          >
            <motion.button
              type="button"
              onClick={() => onPick(text)}
              whileHover={{ y: -3 }}
              whileTap={{ scale: 0.98 }}
              className="group flex h-full w-full items-start gap-3 rounded-2xl text-left border border-ink-600/70 bg-ink-800/50 p-4 text-sm text-cream-200 backdrop-blur transition-colors hover:border-tomato-400/50 hover:bg-ink-700/60"
            >
              <span className="grid h-8 w-8 shrink-0 place-items-center rounded-xl bg-ink-700 text-saffron-300 transition-colors group-hover:bg-tomato-500/20 group-hover:text-tomato-400">
                <Icon className="h-4 w-4" />
              </span>
              <span className="leading-relaxed">{text}</span>
            </motion.button>
          </motion.li>
        ))}
      </motion.ul>
    </div>
  )
}
