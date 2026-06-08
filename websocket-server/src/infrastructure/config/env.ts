export const env = {
  port: Number(process.env.PORT) || 4001,
  redisUrl: process.env.REDIS_URL || 'redis://localhost:6379',
  clientOrigin: process.env.CLIENT_ORIGIN || 'http://localhost:5173',
} as const
