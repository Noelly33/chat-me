import { randomUUID } from 'node:crypto'
import { Hono } from 'hono'
import { cors } from 'hono/cors'
import { createNodeWebSocket } from '@hono/node-ws'
import type { ChatEvent } from '../../domain/ChatEvent.js'
import type { ChatService } from '../../domain/ports/ChatService.js'

/** Maps domain events to the wire format the frontend expects */
function toClientPayload(event: ChatEvent): object {
  switch (event.type) {
    case 'chat:message':
      return { type: 'chat:message', payload: { id: event.id, username: event.username, text: event.text, timestamp: event.timestamp } }
    case 'chat:typing':
      return { type: 'chat:typing', payload: { username: event.username, isTyping: event.isTyping } }
    case 'user:joined':
      return { type: 'system:message', payload: { id: `sys-${Date.now()}`, text: `${event.username} se ha unido al chat`, timestamp: event.timestamp } }
    case 'user:left':
      return { type: 'system:message', payload: { id: `sys-${Date.now()}`, text: `${event.username} ha salido del chat`, timestamp: event.timestamp } }
    case 'roster:updated':
      return { type: 'users:online', payload: event.users }
  }
}

export function buildApp(chatService: ChatService, clientOrigin: string) {
  const app = new Hono()
  const { injectWebSocket, upgradeWebSocket } = createNodeWebSocket({ app })

  app.use('*', cors({ origin: clientOrigin }))

  app.get('/health', (c) => c.json({ status: 'ok', time: new Date().toISOString() }))

  /**
   * Username is injected by the API Gateway as a request header.
   * This node never validates tokens — that's the gateway's responsibility.
   */
  app.get(
    '/ws',
    upgradeWebSocket((c) => {
      const username = c.req.header('x-username') ?? 'anonymous'
      const sessionId = randomUUID()

      return {
        async onOpen(_, ws) {
          await chatService.handleConnect(sessionId, username, {
            send: (data) => ws.send(data),
          })
        },
        async onMessage(event) {
          await chatService.handleMessage(sessionId, event.data.toString())
        },
        async onClose() {
          await chatService.handleDisconnect(sessionId)
        },
      }
    }),
  )

  return { app, injectWebSocket, toClientPayload }
}
