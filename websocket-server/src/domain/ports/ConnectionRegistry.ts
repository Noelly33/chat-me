import type { MessageSender } from './ChatService.js'

export interface ConnectionInfo {
  username: string
  userId: string
  sender: MessageSender
}

export interface ConnectionRegistry {
  add(sessionId: string, username: string, userId: string, sender: MessageSender): void
  remove(sessionId: string): ConnectionInfo | undefined
  broadcast(data: string): void
  count(): number
  getUsername(sessionId: string): string | undefined
  getUserId(sessionId: string): string | undefined
}
