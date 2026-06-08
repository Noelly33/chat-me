import type { ChatService, MessageSender } from '../domain/ports/ChatService.js'
import type { ConnectionRegistry } from '../domain/ports/ConnectionRegistry.js'
import type { EventBroker } from '../domain/ports/EventBroker.js'
import type { PresenceRepository } from '../domain/ports/PresenceRepository.js'

const MAX_TEXT_LENGTH = 1000

function makeId(): string {
  return `${Date.now()}-${Math.random().toString(36).slice(2, 9)}`
}

export class ChatUseCase implements ChatService {
  constructor(
    private readonly registry: ConnectionRegistry,
    private readonly broker: EventBroker,
    private readonly presence: PresenceRepository,
  ) {}

  async handleConnect(sessionId: string, username: string, sender: MessageSender): Promise<void> {
    this.registry.add(sessionId, username, sender)
    await this.presence.increment(username)

    const users = await this.presence.getRoster()
    await this.broker.publish({ type: 'user:joined', username, timestamp: new Date().toISOString() })
    await this.broker.publish({ type: 'roster:updated', users })
  }

  async handleMessage(sessionId: string, rawData: string): Promise<void> {
    const username = this.registry.getUsername(sessionId)
    if (!username) return

    let parsed: unknown
    try {
      parsed = JSON.parse(rawData)
    } catch {
      return
    }

    const msg = parsed as { type?: unknown; payload?: unknown }

    if (msg.type === 'chat:message') {
      const raw = (msg.payload as { text?: unknown } | null)?.text
      const text = (raw != null ? String(raw) : '').trim()
      if (!text || text.length > MAX_TEXT_LENGTH) return

      await this.broker.publish({
        type: 'chat:message',
        id: makeId(),
        username,
        text,
        timestamp: new Date().toISOString(),
      })
      return
    }

    if (msg.type === 'chat:typing') {
      await this.broker.publish({
        type: 'chat:typing',
        username,
        isTyping: Boolean(msg.payload),
      })
    }
  }

  async handleDisconnect(sessionId: string): Promise<void> {
    const info = this.registry.remove(sessionId)
    if (!info) return

    const remaining = await this.presence.decrement(info.username)
    const users = await this.presence.getRoster()

    if (remaining <= 0) {
      await this.broker.publish({
        type: 'user:left',
        username: info.username,
        timestamp: new Date().toISOString(),
      })
    }

    await this.broker.publish({ type: 'roster:updated', users })
  }
}
