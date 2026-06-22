export type ChatEvent =
  | { type: 'chat:message';   id: string; username: string; text: string; timestamp: string }
  | { type: 'chat:typing';    username: string; isTyping: boolean }
  | { type: 'user:joined';    username: string; timestamp: string }
  | { type: 'user:left';      username: string; timestamp: string }
  | { type: 'roster:updated'; users: string[] }
