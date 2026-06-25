export type ChatEvent =
  | { type: 'chat:message';   id: string; username: string; senderId: string; text: string; timestamp: string; to?: string[]; _seq?: number }
  | { type: 'chat:typing';    username: string; isTyping: boolean; to?: string[]; _seq?: number }
  | { type: 'user:joined';    username: string; timestamp: string; _seq?: number }
  | { type: 'user:left';      username: string; timestamp: string; _seq?: number }
  | { type: 'roster:updated'; users: string[]; _seq?: number }
