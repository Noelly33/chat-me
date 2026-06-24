import 'dotenv/config'
import { Redis } from 'ioredis'
import { serve } from '@hono/node-server'
import { env } from './infrastructure/config/env.js'
import { InMemoryConnectionRegistry } from './infrastructure/adapters/InMemoryConnectionRegistry.js'
import { RedisEventBroker } from './infrastructure/adapters/RedisEventBroker.js'
import { RedisPresenceRepository } from './infrastructure/adapters/RedisPresenceRepository.js'
import { RabbitMQMessagePublisher } from './infrastructure/adapters/RabbitMQMessagePublisher.js'
import { ChatUseCase } from './application/ChatUseCase.js'
import { buildApp } from './infrastructure/http/HonoApp.js'
import { nextSeq, log } from './infrastructure/logging/logger.js'

// --- Outbound adapters ---
const registry = new InMemoryConnectionRegistry()

const pubClient  = new Redis(env.redisUrl)
const subClient  = new Redis(env.redisUrl)
const dataClient = new Redis(env.redisUrl)

const broker    = new RedisEventBroker(pubClient, subClient)
const presence  = new RedisPresenceRepository(dataClient)
const publisher = await RabbitMQMessagePublisher.create(env.rabbitmqUrl)

const chatService = new ChatUseCase(registry, broker, presence, publisher)

const { app, injectWebSocket, toClientPayload } = buildApp(chatService, env.clientOrigin)

await broker.subscribe((event) => {
  const seq = event._seq ?? nextSeq()
  const recipients = registry.count()
  log(seq, '→ BROADCAST', {
    type: event.type,
    recipients,
    payload: JSON.stringify(toClientPayload(event)).length + 'B',
  })
  registry.broadcast(JSON.stringify(toClientPayload(event)))
})

log(nextSeq(), '✓ WS_NODE_READY', {
  ws: `ws://localhost:${env.port}/ws`,
  redis: env.redisUrl,
  rabbitmq: env.rabbitmqUrl,
})

const server = serve({ fetch: app.fetch, port: env.port }, () => {
  log(nextSeq(), '✓ HTTP_LISTENING', { port: env.port })
})

injectWebSocket(server)
