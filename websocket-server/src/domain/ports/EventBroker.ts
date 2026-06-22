import type { ChatEvent } from '../ChatEvent.js'

/** Outbound port — pub/sub bridge between nodes (Redis adapter) */
export interface EventBroker {
  publish(event: ChatEvent): Promise<void>
  subscribe(handler: (event: ChatEvent) => void): Promise<void>
  close(): Promise<void>
}
