import type { ChatService, MessageSender } from '../domain/ports/ChatService.js'
import type { ConnectionRegistry } from '../domain/ports/ConnectionRegistry.js'
import type { EventBroker } from '../domain/ports/EventBroker.js'
import type { PresenceRepository } from '../domain/ports/PresenceRepository.js'
import type { MessagePublisher } from '../domain/ports/MessagePublisher.js'

const MAX_TEXT_LENGTH = 1000

export class ChatUseCase implements ChatService {
  constructor(
    private readonly registry: ConnectionRegistry,
    private readonly broker: EventBroker,
    private readonly presence: PresenceRepository,
    private readonly publisher: MessagePublisher,
  ) {}

  async handleConnect(sessionId: string, username: string, userId: string, sender: MessageSender): Promise<void> {
    this.registry.add(sessionId, username, userId, sender)
    await this.presence.increment(username)

    const users = await this.presence.getRoster()
    await this.broker.publish({ type: 'user:joined', username, timestamp: new Date().toISOString() })
    await this.broker.publish({ type: 'roster:updated', users })
  }

  async handleMessage(sessionId: string, rawData: string): Promise<void> {
    const username = this.registry.getUsername(sessionId)
    const userId   = this.registry.getUserId(sessionId)
    if (!username || !userId) return

    let parsed: unknown
    try {
      parsed = JSON.parse(rawData)
    } catch {
      return
    }

    const msg = parsed as { type?: unknown; payload?: unknown }

    // ── chat:message — message within an existing conversation ──────────────
    if (msg.type === 'chat:message') {
      const p = msg.payload as { id?: unknown; conversacionId?: unknown; text?: unknown; paraUsername?: unknown } | null
      const text          = ((p?.text != null ? String(p.text) : '')).trim()
      const conversacionId = p?.conversacionId != null ? String(p.conversacionId) : null
      const paraUsername   = p?.paraUsername != null ? String(p.paraUsername) : null
      const mensajeId     = p?.id != null ? String(p.id) : crypto.randomUUID()

      if (!text || text.length > MAX_TEXT_LENGTH || !conversacionId || !paraUsername) return

      await Promise.all([
        this.broker.publish({
          type: 'chat:message',
          id: mensajeId,
          username,
          senderId: userId,
          text,
          timestamp: new Date().toISOString(),
          to: [username, paraUsername],
        }),
        this.publisher.publishMensajeEnviado({
          MensajeId:        mensajeId,
          ConversacionId:   conversacionId,
          EmisorId:         userId,
          Contenido:        text,
          TipoMensajeCodigo: 'TEXTO',
        }),
      ])
      return
    }

    // ── chat:iniciar_individual — first message, new 1-to-1 conversation ───
    if (msg.type === 'chat:iniciar_individual') {
      const p = msg.payload as { id?: unknown; receptorId?: unknown; text?: unknown; paraUsername?: unknown } | null
      const text        = ((p?.text != null ? String(p.text) : '')).trim()
      const receptorId  = p?.receptorId != null ? String(p.receptorId) : null
      const paraUsername = p?.paraUsername != null ? String(p.paraUsername) : null
      const mensajeId   = p?.id != null ? String(p.id) : crypto.randomUUID()

      if (!text || text.length > MAX_TEXT_LENGTH || !receptorId || !paraUsername) return

      await Promise.all([
        this.broker.publish({
          type: 'chat:message',
          id: mensajeId,
          username,
          senderId: userId,
          text,
          timestamp: new Date().toISOString(),
          to: [username, paraUsername],
        }),
        this.publisher.publishIniciarChatIndividual({
          MensajeId:        mensajeId,
          EmisorId:         userId,
          ReceptorId:       receptorId,
          Contenido:        text,
          TipoMensajeCodigo: 'TEXTO',
        }),
      ])
      return
    }

    // ── chat:typing ─────────────────────────────────────────────────────────
    if (msg.type === 'chat:typing') {
      const p = msg.payload as { isTyping?: unknown; paraUsername?: unknown } | null
      const paraUsername = p?.paraUsername != null ? String(p.paraUsername) : null
      if (!paraUsername) return

      await this.broker.publish({
        type: 'chat:typing',
        username,
        isTyping: Boolean(p?.isTyping),
        to: [username, paraUsername],
      })
      return
    }

    // ── chat:leido — mark conversation as read ──────────────────────────────
    if (msg.type === 'chat:leido') {
      const p = msg.payload as { conversacionId?: unknown } | null
      const conversacionId = p?.conversacionId != null ? String(p.conversacionId) : null
      if (!conversacionId) return

      await this.publisher.publishChatLeido({
        ConversacionId: conversacionId,
        UsuarioId:      userId,
        LeidoAt:        new Date().toISOString(),
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
