import { motion } from 'motion/react'
import { ArrowUp } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'

const maxLength = 1000

export function Composer({ disabled, onSend }: { disabled: boolean; onSend: (question: string) => void }) {
  const [value, setValue] = useState('')
  const textarea = useRef<HTMLTextAreaElement>(null)

  // Grows with the text, up to a few lines.
  useEffect(() => {
    const element = textarea.current
    if (!element) return
    element.style.height = 'auto'
    element.style.height = `${Math.min(element.scrollHeight, 180)}px`
  }, [value])

  const submit = () => {
    if (disabled || !value.trim()) return
    onSend(value)
    setValue('')
  }

  return (
    <form
      onSubmit={(event) => {
        event.preventDefault()
        submit()
      }}
      className="group relative rounded-[28px] border border-ink-600/80 bg-ink-800/70 p-2 shadow-2xl shadow-black/40 backdrop-blur-xl transition-colors focus-within:border-tomato-400/60"
    >
      <div className="pointer-events-none absolute -inset-px rounded-[28px] bg-gradient-to-r from-tomato-500/0 via-tomato-400/20 to-saffron-300/0 opacity-0 blur-md transition-opacity group-focus-within:opacity-100" />
      <div className="relative flex items-end gap-2">
        <textarea
          ref={textarea}
          rows={1}
          value={value}
          maxLength={maxLength}
          placeholder="Pergunte sobre vendas, avaliações, cardápio…"
          onChange={(event) => setValue(event.target.value)}
          onKeyDown={(event) => {
            if (event.key === 'Enter' && !event.shiftKey) {
              event.preventDefault()
              submit()
            }
          }}
          className="max-h-44 flex-1 resize-none bg-transparent px-4 py-3 text-[15px] text-cream-50 placeholder:text-cream-500 focus:outline-none"
        />
        <motion.button
          type="submit"
          disabled={disabled || !value.trim()}
          whileTap={{ scale: 0.9 }}
          whileHover={{ scale: 1.05 }}
          aria-label="Enviar pergunta"
          className="grid h-11 w-11 shrink-0 place-items-center rounded-full bg-gradient-to-br from-tomato-400 to-tomato-500 text-cream-50 shadow-lg shadow-tomato-500/30 transition-opacity disabled:cursor-not-allowed disabled:opacity-35"
        >
          <ArrowUp className="h-5 w-5" strokeWidth={2.4} />
        </motion.button>
      </div>
    </form>
  )
}
