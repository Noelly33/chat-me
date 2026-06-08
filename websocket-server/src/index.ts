import 'dotenv/config'
import { Redis } from 'ioredis'
import { serve } from '@hono/node-server'
import { env } from './infrastructure/config/env.js'
import { InMemoryConnectionRegistry } from './infrastructure/adapters/InMemoryConnectionRegistry.js'
import { RedisEventBroker } from './infrastructure/adapters/RedisEventBroker.js'
import { RedisPresenceRepository } from './infrastructure/adapters/RedisPresenceRepository.js'
import { ChatUseCase } from './application/ChatUseCase.js'
import { buildApp } from './infrastructure/http/HonoApp.js'

// --- Outbound adapters ---
const registry = new InMemoryConnectionRegistry()

const pubClient = new Redis(env.redisUrl)
const subClient = new Redis(env.redisUrl)
const dataClient = new Redis(env.redisUrl)

const broker = new RedisEventBroker(pubClient, subClient)
const presence = new RedisPresenceRepository(dataClient)

const chatService = new ChatUseCase(registry, broker, presence)

const { app, injectWebSocket, toClientPayload } = buildApp(chatService, env.clientOrigin)

await broker.subscribe((event) => {
  registry.broadcast(JSON.stringify(toClientPayload(event)))
})

const server = serve({ fetch: app.fetch, port: env.port }, () => {
  console.log(`[ws-server] ws://localhost:${env.port}/ws`)
  console.log(`[ws-server] redis: ${env.redisUrl}`)
})

injectWebSocket(server)
