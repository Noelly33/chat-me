import type { MessageSender } from './ChatService.js'

export interface ConnectionInfo {
  username: string
  sender: MessageSender
}

export interface ConnectionRegistry {
  add(sessionId: string, username: string, sender: MessageSender): void
  remove(sessionId: string): ConnectionInfo | undefined
  broadcast(data: string): void
  getUsername(sessionId: string): string | undefined
}
