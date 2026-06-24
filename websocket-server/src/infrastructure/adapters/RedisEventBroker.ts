import { Redis } from 'ioredis'
import type { EventBroker } from '../../domain/ports/EventBroker.js'
import type { ChatEvent } from '../../domain/ChatEvent.js'
import { nextSeq, log } from '../logging/logger.js'

const CHANNEL = 'chat:events'

export class RedisEventBroker implements EventBroker {
  constructor(
    private readonly pub: Redis,
    private readonly sub: Redis,
  ) {}

  async publish(event: ChatEvent): Promise<void> {
    const seq = event._seq ?? nextSeq()
    await this.pub.publish(CHANNEL, JSON.stringify(event))
    log(seq, '✓ REDIS_PUB_OK', { channel: CHANNEL, type: event.type, bytes: JSON.stringify(event).length })
  }

  async subscribe(handler: (event: ChatEvent) => void): Promise<void> {
    await this.sub.subscribe(CHANNEL)
    log(nextSeq(), '✓ REDIS_SUB_OK', { channel: CHANNEL })
    this.sub.on('message', (_, message) => {
      try {
        const event = JSON.parse(message) as ChatEvent
        const seq = event._seq ?? nextSeq()
        log(seq, '← REDIS_SUB', { channel: CHANNEL, type: event.type })
        handler(event)
      } catch (err) {
        log(nextSeq(), '✗ REDIS_SUB_PARSE_FAIL', { reason: (err as Error).message })
      }
    })
  }

  async close(): Promise<void> {
    await this.pub.quit()
    await this.sub.quit()
  }
}
