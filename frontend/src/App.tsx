import { AnimatePresence } from 'motion/react'
import { useEffect, useRef } from 'react'
import { AnimatedBackground } from './components/AnimatedBackground'
import { BrandMark } from './components/BrandMark'
import { Composer } from './components/Composer'
import { EmptyState } from './components/EmptyState'
import { Message } from './components/Message'
import { Sidebar } from './components/Sidebar'
import { ThinkingIndicator } from './components/ThinkingIndicator'
import { useChat } from './hooks/useChat'

export default function App() {
  const { messages, pending, send, reset } = useChat()
  const bottom = useRef<HTMLDivElement>(null)

  useEffect(() => {
    bottom.current?.scrollIntoView({ behavior: 'smooth', block: 'end' })
  }, [messages.length, pending])

  return (
    <div className="flex h-full">
      <AnimatedBackground />
      <Sidebar onNewChat={reset} canReset={messages.length > 0 && !pending} />

      <main className="relative flex min-w-0 flex-1 flex-col">
        <header className="flex items-center justify-between border-b border-ink-700/70 px-4 py-3 lg:hidden">
          <BrandMark />
        </header>

        <div className="flex-1 overflow-y-auto">
          {messages.length === 0 && !pending ? (
            <EmptyState onPick={send} />
          ) : (
            <div className="mx-auto max-w-3xl space-y-8 px-4 py-10">
              <AnimatePresence initial={false}>
                {messages.map((message) => (
                  <Message key={message.id} message={message} />
                ))}
                {pending && <ThinkingIndicator key="thinking" />}
              </AnimatePresence>
              <div ref={bottom} className="h-4" />
            </div>
          )}
        </div>

        <div className="mx-auto w-full max-w-3xl px-4 pb-6">
          <Composer disabled={pending} onSend={send} />
          <p className="mt-2 text-center text-[11px] text-cream-500">
            As respostas vêm de consultas aos dados reais do restaurante. Confira as fontes em “Como cheguei aqui”.
          </p>
        </div>
      </main>
    </div>
  )
}
