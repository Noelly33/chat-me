import type { Redis } from 'ioredis'
import type { PresenceRepository } from '../../domain/ports/PresenceRepository.js'

const KEY = 'chat:presence'

export class RedisPresenceRepository implements PresenceRepository {
  constructor(private readonly redis: Redis) {}

  async increment(username: string): Promise<void> {
    await this.redis.hincrby(KEY, username, 1)
  }

  async decrement(username: string): Promise<number> {
    const count = await this.redis.hincrby(KEY, username, -1)
    if (count <= 0) await this.redis.hdel(KEY, username)
    return count
  }

  async getRoster(): Promise<string[]> {
    return this.redis.hkeys(KEY)
  }
}
