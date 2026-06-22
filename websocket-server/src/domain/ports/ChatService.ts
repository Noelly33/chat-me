export interface MessageSender {
  send(data: string): void
}

/** Inbound port — driven by the WS adapter */
export interface ChatService {
  handleConnect(sessionId: string, username: string, userId: string, sender: MessageSender): Promise<void>
  handleMessage(sessionId: string, rawData: string): Promise<void>
  handleDisconnect(sessionId: string): Promise<void>
}
