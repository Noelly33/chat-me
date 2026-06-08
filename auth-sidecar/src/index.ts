import 'dotenv/config'
import { Hono } from 'hono'
import { serve } from '@hono/node-server'
import jwt from 'jsonwebtoken'

const PORT = Number(process.env.PORT) || 3000
const JWT_SECRET = process.env.JWT_SECRET

if (!JWT_SECRET) {
  console.error('[auth-sidecar] JWT_SECRET env var is required')
  process.exit(1)
}

const app = new Hono()

app.get('/health', (c) => c.json({ status: 'ok' }))

/**
 * Nginx llama este endpoint vía `auth_request /internal/auth` antes de
 * hacer el proxy upgrade a /ws.
 *
 * Nginx reenvía la petición original incluyendo:
 *   - Authorization: Bearer <token>   (si el cliente usa header)
 *   - X-Original-URI: /ws?token=<jwt> (si el cliente usa query param)
 *
 * Respuesta exitosa → 200 + cabecera X-Username
 * Nginx captura X-Username con `auth_request_set` y lo inyecta
 * en la petición al upstream como `proxy_set_header X-Username`.
 *
 * Respuesta fallida → 401 (Nginx devuelve el error al cliente)
 */
app.get('/auth', (c) => {
  let token: string | undefined

  const authHeader = c.req.header('authorization')
  if (authHeader?.startsWith('Bearer ')) {
    token = authHeader.slice(7)
  }

  if (!token) {
    const originalUri = c.req.header('x-original-uri') ?? ''
    try {
      const url = new URL(originalUri, 'http://localhost')
      token = url.searchParams.get('token') ?? undefined
    } catch { /* URI malformada */ }
  }

  if (!token) return c.text('Unauthorized', 401)

  try {
    const payload = jwt.verify(token, JWT_SECRET!) as { username?: string }
    if (!payload.username) return c.text('Unauthorized', 401)

    c.header('X-Username', payload.username)
    return c.text('OK', 200)
  } catch {
    return c.text('Unauthorized', 401)
  }
})

serve({ fetch: app.fetch, port: PORT }, () => {
  console.log(`[auth-sidecar] listening on :${PORT}`)
})
