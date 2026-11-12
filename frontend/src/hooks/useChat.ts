import { useCallback, useEffect, useState } from 'react'
import { api } from '../api/client'
import type { ChatResponse } from '../api/types'

export interface ChatMessage {
  id: string
  role: 'user' | 'assistant'
  content: string
  response?: ChatResponse
  error?: boolean
}

const storageKey = 'restaurant-copilot:conversation'

function loadConversation(): ChatMessage[] {
  try {
    return JSON.parse(localStorage.getItem(storageKey) ?? '[]') as ChatMessage[]
  } catch {
    return []
  }
}

/** Conversation state, kept in localStorage so a page refresh does not lose it. */
export function useChat() {
  const [messages, setMessages] = useState<ChatMessage[]>(loadConversation)
  const [pending, setPending] = useState(false)

  useEffect(() => {
    try {
      localStorage.setItem(storageKey, JSON.stringify(messages))
    } catch {
      // Storage can be unavailable (private mode); the conversation simply is not persisted.
    }
  }, [messages])

  const send = useCallback(async (question: string) => {
    const text = question.trim()
    if (!text) return

    setMessages((current) => [...current, { id: crypto.randomUUID(), role: 'user', content: text }])
    setPending(true)
    try {
      const response = await api.ask(text)
      setMessages((current) => [...current, { id: crypto.randomUUID(), role: 'assistant', content: response.answer, response }])
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Não foi possível obter uma resposta.'
      setMessages((current) => [...current, { id: crypto.randomUUID(), role: 'assistant', content: message, error: true }])
    } finally {
      setPending(false)
    }
  }, [])

  const reset = useCallback(() => setMessages([]), [])

  return { messages, pending, send, reset }
}
