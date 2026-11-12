import { AnimatePresence, motion } from 'motion/react'
import { AlertCircle, ChevronDown, Database, FileText, Wrench, Zap } from 'lucide-react'
import { useState } from 'react'
import Markdown from 'react-markdown'
import remarkGfm from 'remark-gfm'
import type { ChatMessage } from '../hooks/useChat'
import { formatNumber, toolLabels } from '../lib/format'
import { AssistantAvatar } from './AssistantAvatar'

export function Message({ message }: { message: ChatMessage }) {
  if (message.role === 'user') {
    return (
      <motion.div
        layout
        initial={{ opacity: 0, y: 12, scale: 0.98 }}
        animate={{ opacity: 1, y: 0, scale: 1 }}
        transition={{ type: 'spring', stiffness: 380, damping: 30 }}
        className="flex justify-end"
      >
        <p className="max-w-[80%] rounded-3xl rounded-br-lg bg-gradient-to-br from-tomato-400 to-tomato-500 px-5 py-3 text-[15px] leading-relaxed text-cream-50 shadow-lg shadow-tomato-500/20">
          {message.content}
        </p>
      </motion.div>
    )
  }

  return (
    <motion.div
      layout
      initial={{ opacity: 0, y: 16 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.45, ease: 'easeOut' }}
      className="flex gap-4"
    >
      <AssistantAvatar />
      <div className="min-w-0 flex-1 pt-1.5">
        {message.error ? (
          <div className="flex gap-2 rounded-2xl bg-tomato-500/10 p-4 text-sm text-cream-200 ring-1 ring-tomato-500/30">
            <AlertCircle className="mt-0.5 h-4 w-4 shrink-0 text-tomato-400" />
            {message.content}
          </div>
        ) : (
          <div className="markdown text-[15px]">
            <Markdown remarkPlugins={[remarkGfm]}>{message.content}</Markdown>
          </div>
        )}
        {message.response && <AnswerDetails response={message.response} />}
      </div>
    </motion.div>
  )
}

function AnswerDetails({ response }: { response: NonNullable<ChatMessage['response']> }) {
  const [open, setOpen] = useState(false)
  const tools = [...new Set(response.toolCalls.map((call) => call.tool))]
  const documents = response.sources.filter((source) => source.kind === 'document')

  return (
    <div className="mt-5 space-y-3">
      <div className="flex flex-wrap items-center gap-2 text-xs">
        {response.cached && (
          <span className="inline-flex items-center gap-1 rounded-full bg-saffron-300/15 px-2.5 py-1 font-medium text-saffron-300">
            <Zap className="h-3 w-3" /> Do cache
          </span>
        )}
        {tools.map((tool) => (
          <span key={tool} className="inline-flex items-center gap-1 rounded-full bg-ink-700 px-2.5 py-1 text-cream-200">
            {tool === 'query_database' ? <Database className="h-3 w-3" /> : <Wrench className="h-3 w-3" />}
            {toolLabels[tool] ?? tool}
          </span>
        ))}
        {documents.length > 0 && (
          <span className="inline-flex items-center gap-1 rounded-full bg-ink-700 px-2.5 py-1 text-cream-200">
            <FileText className="h-3 w-3" /> {documents.length} documentos
          </span>
        )}
        <button
          type="button"
          onClick={() => setOpen((current) => !current)}
          className="ml-auto inline-flex items-center gap-1 rounded-full px-2.5 py-1 text-cream-400 transition-colors hover:bg-ink-700 hover:text-cream-50"
        >
          Como cheguei aqui
          <motion.span animate={{ rotate: open ? 180 : 0 }}>
            <ChevronDown className="h-3.5 w-3.5" />
          </motion.span>
        </button>
      </div>

      <AnimatePresence initial={false}>
        {open && (
          <motion.div
            initial={{ height: 0, opacity: 0 }}
            animate={{ height: 'auto', opacity: 1 }}
            exit={{ height: 0, opacity: 0 }}
            transition={{ duration: 0.3, ease: 'easeInOut' }}
            className="overflow-hidden"
          >
            <div className="space-y-4 rounded-2xl border border-ink-600/70 bg-ink-900/70 p-4 text-xs">
              <ol className="relative space-y-3 border-l border-ink-600 pl-4">
                {response.toolCalls.map((call, index) => (
                  <li key={index} className="relative">
                    <span
                      className={`absolute -left-[21px] top-1 h-2.5 w-2.5 rounded-full ring-4 ring-ink-900 ${call.succeeded ? 'bg-basil-400' : 'bg-tomato-500'}`}
                    />
                    <p className="font-medium text-cream-50">{toolLabels[call.tool] ?? call.tool}</p>
                    <pre className="mt-1 overflow-x-auto whitespace-pre-wrap break-words rounded-lg bg-ink-800 p-2 font-mono text-[11px] text-cream-400">
                      {typeof call.arguments.sql === 'string' ? call.arguments.sql : JSON.stringify(call.arguments, null, 1)}
                    </pre>
                    {call.error && <p className="mt-1 text-tomato-400">{call.error}</p>}
                  </li>
                ))}
              </ol>

              {response.sources.length > 0 && (
                <div>
                  <p className="mb-2 font-semibold uppercase tracking-[0.12em] text-cream-500">Fontes</p>
                  <ul className="space-y-1.5">
                    {response.sources.map((source, index) => (
                      <li key={index} className="line-clamp-2 text-cream-400">
                        {source.kind === 'tool' ? '🔧' : '📄'} {source.description}
                      </li>
                    ))}
                  </ul>
                </div>
              )}

              <p className="text-cream-500">
                {formatNumber(response.usage.inputTokens)} tokens de entrada · {formatNumber(response.usage.outputTokens)} de saída
              </p>
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  )
}
