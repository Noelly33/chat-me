import { Redis } from 'ioredis'
import type { EventBroker } from '../../domain/ports/EventBroker.js'
import type { ChatEvent } from '../../domain/ChatEvent.js'

const CHANNEL = 'chat:events'

export class RedisEventBroker implements EventBroker {
  constructor(
    private readonly pub: Redis,
    private readonly sub: Redis,
  ) {}

  async publish(event: ChatEvent): Promise<void> {
    await this.pub.publish(CHANNEL, JSON.stringify(event))
  }

  async subscribe(handler: (event: ChatEvent) => void): Promise<void> {
    await this.sub.subscribe(CHANNEL)
    this.sub.on('message', (_, message) => {
      try {
        handler(JSON.parse(message) as ChatEvent)
      } catch { /* malformed event — ignore */ }
    })
  }

  async close(): Promise<void> {
    await this.pub.quit()
    await this.sub.quit()
  }
}
