# Frontend Build Prompt — ChatMe

> Prompt listo para pegar en Claude Code, Cursor u otra IA. Define el alcance, las restricciones y los criterios de aceptación para construir el SPA de React que consume el backend descrito en `docs/API.md`.

---

# Role
Sos un/a desarrollador/a senior React + TypeScript. Vas a migrar el frontend de ChatMe, una SPA de chat en tiempo real.

# Contexto del repo

El repo es un sistema de chat poliglota. El backend completo vive en Docker Compose:
- `nginx` (puerto 80) — API gateway. Hace `auth_request` contra SideCar.Auth.Api e inyecta `X-User-Id`, `X-Username`, `X-Email` a los upstreams.
- `SideCar.Auth.Api` (.NET 10) — register, login, refresh, logout, validate, profile.
- `Core.Mensajes.Api` (.NET 10) — messages y contacts.
- `websocket-server` (Hono + node-ws) — 3 nodos stateless, arquitectura hexagonal, fanout por Redis Pub/Sub.
- `redis`, `rabbitmq`, `messaging-worker` (.NET).

El front actual en `/client` apunta al server legacy Express+Socket.IO (`/server`, puerto 4000) y se descarta entero. NO mires `server/` para nada. debes mirar configuraciones de nginx y `/docs/API.md`

# Documentación que DEBÉS leer antes de tocar código

- `docs/API.md` — referencia completa de todos los endpoints REST + WS, modelo de cookies, eventos, códigos de error. Es tu single source of truth.
- `CLAUDE.md` (raíz) — arquitectura general.
- `/client/src/` — estructura actual (Login, Chat, components, hooks, services). La idea es ver qué hay y reemplazar.

# Stack del front

- React 18 + Vite (lo que ya está en `package.json`).
- **WebSocket nativo del browser** (NO Socket.IO). El browser no permite setear headers en `new WebSocket(url)` — por eso el backend usa cookies HttpOnly.
- **Fetch nativo** (NO axios) para REST.
- TypeScript si no está ya; agregalo si falta.
- CSS: el `index.css` actual sirve de base. NO instales Tailwind ni frameworks UI. Usá CSS plano o módulos.
- Sin Redux/Zustand. React state + Context alcanza.

# Setup inicial

1. Configurar `vite.config.js` con proxy de `/api` y `/ws` a `http://localhost:80`. Esto hace que el browser vea mismo origen (importante para que `SameSite=Lax` envíe cookies en POST):

   ```js
   server: {
     port: 5173,
     proxy: {
       '/api': { target: 'http://localhost:80', changeOrigin: true },
       '/ws':  { target: 'ws://localhost:80',   ws: true, changeOrigin: true },
     },
   }
   ```

2. Borrar `socket.io-client` del `package.json`. No agregar nada más a menos que sea estrictamente necesario.

# Auth: cómo funciona

- `POST /api/v1/auth/register` y `POST /api/v1/auth/login` devuelven 200 con `MsResponse<...>` Y setean cookies HttpOnly `chat_auth` (access, 15 min) y `chat_refresh` (refresh, 7 días).
- El browser manda las cookies automáticamente en cada request. **NO** las guardes en `localStorage`, **NO** mandes `Authorization` header manual.
- `MsRequest<T>` envelope: `{ header: { transactionId?, timestamp?, device? }, data: <T> }`.
- `MsResponse<T>` envelope: `{ success, message, errors, data }`. Si `success: false`, `data` es `null`.
- 401 → intentar `POST /api/v1/auth/refresh` una vez (con cookie automática); si también 401 → volver a login.

# Vistas

## 1) `/login` (ruta única, con tabs)
- Tab "Iniciar sesión": campos `Identifier` (email o `nombreUsuario`) + `Password`.
- Tab "Crear cuenta": campos `Nombres`, `Apellidos`, `NombreUsuario` (≥3), `Email`, `Password` (≥6), `NumeroTelefono` (E.164 `+...`), `FechaNacimiento` (date picker, opcional).
- Después de login/register exitoso → redirigir a `/`.
- Errores: mostrar `errors[]` del response, no solo el genérico.

## 2) `/` (chat principal, post-login)
Layout de 3 zonas (CSS grid):

```
┌──────────────────────────────────────────────────┐
│ TopBar: nombre app · connection status · logout  │
├──────────────┬───────────────────────────────────┤
│ Buscador     │ Header del chat activo            │
│ de contactos │ (avatar + nombre + "en línea")    │
│ ──────────── │───────────────────────────────────│
│ Lista de     │                                   │
│ conversacio- │ Mensajes (propios derecha,        │
│ nes (más     │  ajenos izquierda,                │
│ reciente     │  sistema centrado y gris)         │
│ arriba)      │                                   │
│              │ Typing indicator                  │
│              │ Input + botón enviar             │
└──────────────┴───────────────────────────────────┘
```

### Buscador (top-left)
- Input que debouncea 300 ms.
- Llama a `GET /api/v1/contacts?page=1&size=10`. Si el endpoint soporta query de búsqueda, usá `?search=`. Si no, filtrá local sobre la lista cacheada.
- Click en un contacto que NO tiene conversación → enviar WS `chat:iniciar_individual` con `{ receptorId, text }` (pasale un saludo corto para crear la conversación).

> **Gap conocido**: el backend NO expone `GET /api/v1/conversations`. La lista de conversaciones del front se construye client-side a partir de los mensajes que llegan por WS. Si querés un endpoint server-side, flagealo al final como follow-up.

### Lista de conversaciones (left)
- Estado: array de conversaciones. Cada item: `{ id, otroUsuario: { id, nombreUsuario, nombres, apellidos, avatarUrl }, ultimoMensaje, noLeidos }`.
- Click → seleccionar activa → cargar historial con `GET /api/v1/messages?conversationId=<id>&page=1&size=50`.
- Cuando llega un `chat:message` por WS para una conversación existente, agregarlo. Si no estaba, crearla con el emisor como `otroUsuario`.

### Vista de chat (centro)
- Header: avatar + nombre + "en línea" / "desconectado" (basado en `users:online` del WS).
- Burbujas: propias a la derecha (color acento), ajenas a la izquierda, sistema centrado y gris.
- Auto-scroll al fondo cuando llega mensaje propio o si el usuario ya estaba al fondo.
- Typing indicator debajo de la lista, sobre el input.
- Input: Enter para enviar, Shift+Enter para nueva línea.
- Disabled visual cuando WS está desconectado.

# WebSocket — punto crítico

URL: `new WebSocket(\`ws(s)://\${location.host}/ws\`)`. NO pasar token en headers ni query. La cookie viaja sola.

Lifecycle:
1. Construir WS cuando el usuario está autenticado.
2. `onopen` → nada explícito (el backend hace `auth_request` via cookie y emite `user:joined`).
3. `onmessage` → parsear JSON. Eventos que te interesan:

| type entrante  | Acción |
|---|---|
| `chat:message` | agregar a la conversación correspondiente. Si no existe, crearla. |
| `chat:typing`  | si `username !== me` y coincide con la conversación activa → mostrar typing 3s |
| `system:message` | mostrar como mensaje del sistema (gris, centrado) en la conversación activa |
| `users:online` | array de usernames en línea; úsalo para el indicador del header |

Eventos salientes (cliente → server):

| type | payload |
|---|---|
| `chat:message`            | `{ conversacionId, text }` |
| `chat:iniciar_individual` | `{ receptorId, text }`     |
| `chat:typing`             | `true` al empezar, `false` al dejar (debounce 2s) |
| `chat:leido`              | `{ conversacionId }` cuando se abre una conversación |

Reconexión: en `onclose`/`onerror`, reintentar con backoff exponencial (1s, 2s, 4s, max 30s). Mostrar "Reconectando..." en el topbar.

# Manejo de sesión

- En `App.jsx`: estado `auth` (`null` o `{ user: { id, email, nombreUsuario, nombres, apellidos, avatarUrl } }`). **NO** guardar tokens en estado ni localStorage.
- Al montar: intentar `GET /api/v1/auth/profile`. Si 200 → setear `auth`, renderizar `/`. Si 401 → intentar `/refresh`. Si 401 → `/login`.
- Wrapper de fetch con retry automático (el 401 dispara refresh una sola vez):

  ```js
  async function authFetch(path, init = {}) {
    const doFetch = () => fetch(path, { credentials: 'include', ...init });
    let res = await doFetch();
    if (res.status === 401 && path !== '/api/v1/auth/refresh') {
      const refreshRes = await fetch('/api/v1/auth/refresh', {
        method: 'POST',
        credentials: 'include',
      });
      if (refreshRes.ok) res = await doFetch();
    }
    if (!res.ok) throw new ApiError(res);
    return res.json();
  }
  ```

- `credentials: 'include'` en TODO fetch (con el proxy de Vite queda same-origin, no hace daño).

# Archivos a entregar

Reemplazar completamente el contenido de `/client/src/`:

```
client/
├── vite.config.js          (modificado: agregar proxy)
├── package.json            (modificado: quitar socket.io-client, agregar TS si falta)
├── index.html              (sin cambios)
└── src/
    ├── main.jsx
    ├── App.jsx             (router + auth gate)
    ├── index.css           (estilos globales; podés reusar el actual)
    ├── routes/
    │   ├── Login.jsx       (login + register tabs)
    │   └── Chat.jsx        (layout 3 zonas)
    ├── components/
    │   ├── ContactSearch.jsx
    │   ├── ConversationList.jsx
    │   ├── ChatHeader.jsx
    │   ├── MessageList.jsx
    │   ├── MessageBubble.jsx
    │   ├── MessageInput.jsx
    │   └── ConnectionStatus.jsx
    ├── hooks/
    │   ├── useAuth.js           (boot: profile → refresh → login)
    │   ├── useWebSocket.js      (connect, reconnect backoff, dispatch eventos)
    │   ├── useConversations.js  (lista client-side)
    │   └── useMessages.js       (por conversación, paginación)
    ├── services/
    │   ├── api.js          (authFetch + helpers register/login/refresh/logout/profile/contacts/messages)
    │   └── ws.js           (createSocket → { send, on, close })
    └── context/
        └── AppContext.jsx  (auth, me, online users, conversations, messages, typing)
```

# Lo que NO debés hacer

- NO uses Socket.IO. El backend es WS nativo.
- NO guardes tokens en `localStorage` ni `sessionStorage`. Las cookies HttpOnly son la fuente de verdad.
- NO mandes `Authorization: Bearer ...` manual. Las cookies se adjuntan solas.
- NO toques archivos de `SideCar.Auth.Api/`, `Core.Mensajes.Api/`, `websocket-server/`, `server/` ni `nginx/`. Este trabajo es solo front.
- NO instales librerías UI pesadas (Material UI, Chakra, etc.). CSS plano o el `index.css` existente.
- NO uses librerías de estado global (Redux, Zustand). Context + useReducer alcanza.
- NO crees un endpoint nuevo en el backend. Si falta algo, flagealo como follow-up.
- NO agregues tests automatizados (fuera de scope salvo que se pida).
- NO implementes drag-and-drop, adjuntos, emojis, video-llamada ni otras features. Solo texto plano + typing + online status.

# Definition of Done

- Login y register funcionan y dejan al usuario en `/`.
- Token refresh funciona transparente (no se ve en UI; en network tab se ve el 401 → 200).
- Logout limpia cookies y vuelve a `/login`.
- WS conecta, reconecta con backoff si se cae.
- Lista de conversaciones aparece al recibir el primer mensaje.
- Buscador permite encontrar contactos y abrir conversación nueva vía `chat:iniciar_individual`.
- Mensajes se envían y se muestran en orden cronológico.
- Typing indicator aparece y desaparece a los 3s.
- Online status del header refleja `users:online`.
- `cd client && npm install && npm run dev` levanta en `localhost:5173` con proxy a nginx en `:80`. Login con un usuario seed (registrarlo la primera vez) funciona end-to-end.

# Follow-ups para reportar al final

- `GET /api/v1/conversations` (listar conversaciones del usuario con paginación) — no existe en el backend actual.
- `GET /api/v1/contacts?search=<q>` — verificar si el endpoint soporta búsqueda; si no, considerar agregar.
- Marcar mensajes como leídos: `chat:leido` ya se publica pero confirmar que `Consumer.Messaging.Worker` lo persiste antes de mostrar contador de no leídos en el front.