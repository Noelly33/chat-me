import type { ConnectionRegistry, ConnectionInfo } from '../../domain/ports/ConnectionRegistry.js'
import type { MessageSender } from '../../domain/ports/ChatService.js'

export class InMemoryConnectionRegistry implements ConnectionRegistry {
  private readonly connections = new Map<string, ConnectionInfo>()

  add(sessionId: string, username: string, userId: string, sender: MessageSender): void {
    this.connections.set(sessionId, { username, userId, sender })
  }

  remove(sessionId: string): ConnectionInfo | undefined {
    const info = this.connections.get(sessionId)
    this.connections.delete(sessionId)
    return info
  }

  broadcast(data: string): void {
    for (const { sender } of this.connections.values()) {
      try { sender.send(data) } catch { /* connection already closed */ }
    }
  }

  sendToUsernames(usernames: string[], data: string): void {
    const targets = new Set(usernames)
    for (const { username, sender } of this.connections.values()) {
      if (!targets.has(username)) continue
      try { sender.send(data) } catch { /* connection already closed */ }
    }
  }

  count(): number {
    return this.connections.size
  }

  getUsername(sessionId: string): string | undefined {
    return this.connections.get(sessionId)?.username
  }

  getUserId(sessionId: string): string | undefined {
    return this.connections.get(sessionId)?.userId
  }
}
