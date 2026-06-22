# ChatMe — Documentación de API

Documentación de referencia de los endpoints HTTP y WebSocket expuestos por **nginx** (`http://localhost:80`) en el modo Docker Compose (`docker compose up --build`).

> **Nota de cobertura**: Esta documentación cubre únicamente los endpoints que pasan por `nginx/nginx.conf`. El servidor legado `server/` (Express + Socket.IO con `register`/`login` y chat por Socket.IO) **no** está expuesto por nginx y solo se usa en desarrollo local con `npm run dev`. Si necesitas esa ruta, ejecuta `server/` directamente en `http://localhost:4000`.

> **Modelo de autenticación**: A partir de la versión con cookies, los navegadores se autentican contra la API **mediante cookies HttpOnly** que SideCar.Auth.Api setea en `register`/`login`/`refresh`. nginx extrae el JWT de la cookie `chat_auth` para validar cada request. Los clientes no-browser (mobile, server-to-server) pueden seguir enviando `Authorization: Bearer <jwt>` — nginx prefiere el header sobre la cookie si ambos están presentes. Ver [§3](#3-cabeceras-y-autenticación) para el detalle.

---

## Tabla de contenidos

- [1. Arquitectura y routing](#1-arquitectura-y-routing)
- [2. Envoltorios comunes](#2-envoltorios-comunes)
- [3. Cabeceras y autenticación](#3-cabeceras-y-autenticación)
- [4. Auth API — `POST /api/v1/auth/register`](#4-auth-api--post-apiv1authregister)
- [5. Auth API — `POST /api/v1/auth/login`](#5-auth-api--post-apiv1authlogin)
- [6. Auth API — `POST /api/v1/auth/refresh`](#6-auth-api--post-apiv1authrefresh)
- [7. Auth API — `PUT /api/v1/auth/profile`](#7-auth-api--put-apiv1authprofile)
- [8. Auth API — `POST /api/v1/auth/logout`](#8-auth-api--post-apiv1authlogout)
- [9. Auth API — `POST /internal/auth/validate` (interno)](#9-auth-api--post-internalauthvalidate-interno)
- [10. Messages API — `GET /api/v1/messages`](#10-messages-api--get-apiv1messages)
- [11. Contacts API — `GET /api/v1/contacts`](#11-contacts-api--get-apiv1contacts)
- [12. Contacts API — `GET /api/v1/contacts/{id}`](#12-contacts-api--get-apiv1contactsid)
- [13. WebSocket — `GET /ws`](#13-websocket--get-ws)
- [14. Health — `GET /health`](#14-health--get-health)
- [15. Códigos de error transversales](#15-códigos-de-error-transversales)

---

## 1. Arquitectura y routing

```
Browser / Cliente
   │
   ▼
nginx (puerto 80)
   │
   ├── /api/v1/auth/register   ──► sidecar-auth-api:5016  (SideCar.Auth.Api)  [set cookie]
   ├── /api/v1/auth/login      ──► sidecar-auth-api:5016  (SideCar.Auth.Api)  [set cookie]
   ├── /api/v1/auth/refresh    ──► sidecar-auth-api:5016  (SideCar.Auth.Api)  [set cookie]
   ├── /api/v1/auth/profile    ──► sidecar-auth-api:5016  (SideCar.Auth.Api)  [+ JWT]
   ├── /api/v1/auth/logout     ──► sidecar-auth-api:5016  (SideCar.Auth.Api)  [clear cookie]
   │
   ├── /api/v1/messages        ──► core-mensajes-api:5017 (Core.Mensajes.Api) [+ JWT]
   ├── /api/v1/contacts        ──► core-mensajes-api:5017 (Core.Mensajes.Api) [+ JWT]
   │
   ├── /ws                     ──► ws-node-{1,2,3}:4001   (websocket-server)   [+ JWT, upgrade]
   └── /health                 ──► ws-node-{1,2,3}:4001   (websocket-server)
```

Los endpoints marcados con **[+ JWT]** ejecutan primero `auth_request /internal/auth` (sub-petición a `SideCar.Auth.Api /api/v1/auth/validate`). Si esa sub-petición devuelve `200`, nginx inyecta las cabeceras `X-User-Id`, `X-Username` y `X-Email` antes de proxy-pass al upstream.

Los endpoints marcados con **[set cookie]** escriben las cookies `chat_auth` (access token) y `chat_refresh` (refresh token). El endpoint marcado con **[clear cookie]** las borra.

| Servicio | Imagen / Build | Puerto interno |
|---|---|---|
| `sidecar-auth-api` | `./SideCar.Auth.Api` (ASP.NET Core 10 + EF Core) | `5016` |
| `core-mensajes-api` | `./Core.Mensajes.Api` (ASP.NET Core 10 + EF Core) | `5017` |
| `ws-node-1..3` | `./websocket-server` (Hono + `@hono/node-ws`) | `4001` (×3) |
| `redis` | `redis:7-alpine` | `6379` |
| `rabbitmq` | `rabbitmq:3.13-management-alpine` | `5672` |
| `messaging-worker` | `./Consumer.Messaging.Worker` (.NET 10 + MassTransit) | — |
| `nginx` | `nginx:1.27-alpine` | `80` (publicado al host) |

---

## 2. Envoltorios comunes

Todas las APIs .NET (`SideCar.Auth.Api` y `Core.Mensajes.Api`) reciben y responden con un sobre uniforme.

### 2.1 `MsRequest<T>` (entrada)

```jsonc
{
  "header": {
    "transactionId": "8c4f...-...-...",   // string, opcional, se autogenera si falta
    "timestamp": "2026-06-21T15:30:00Z",  // ISO-8601 UTC
    "device": "web"                       // string opcional
  },
  "data": { /* payload específico del endpoint */ }
}
```

Definido en `SideCar.Auth.Api/DTOS/MsRequest.cs` y duplicado en `Core.Mensajes.Api/Domain/DTOS/MsRequest.cs`.

### 2.2 `MsResponse<T>` (salida)

```jsonc
{
  "success": true,
  "message": "Success",
  "errors": [],
  "data": { /* payload específico del endpoint */ }
}
```

Constructores estáticos: `Ok(data)`, `Fail(error)`, `Fail(errors[])`. Cuando el resultado es `Fail`, `data` es `null`.

### 2.3 Validación (FluentValidation)

Validaciones aplicadas en `SideCar.Auth.Api/InfraStructure/Validators/*.cs`. Si fallan, el controlador responde `400 Bad Request` con `MsResponse.Fail(errors[])`.

| Endpoint | Reglas |
|---|---|
| `register` | `Email` no vacío + formato email + único; `Password` ≥ 6; `NombreUsuario` ≥ 3; `Nombres` y `Apellidos` no vacíos; `NumeroTelefono` con regex `^\+?[1-9]\d{1,14}$`. |
| `login` | `Identifier` y `Password` no vacíos. |
| `profile (PUT)` | `Nombres`/`Apellidos` no vacíos si se envían; `NumeroTelefono` con regex E.164; `AvatarUrl` debe ser URL absoluta válida. |

---

## 3. Cabeceras y autenticación

### 3.1 Cabeceras de proxy (siempre presentes)

nginx añade siempre al upstream, vía `proxy_set_header`:

- `Host` → `$host`
- `X-Real-IP` → IP del cliente
- `X-Forwarded-For` → cadena de proxies
- `X-Forwarded-Proto` → `http` (o `https`)

### 3.2 Cabeceras inyectadas en endpoints protegidos

Solo en endpoints con `auth_request /internal/auth`, nginx extrae las cabeceras del sub-request y las reenvía:

| Cabecera | Origen |
|---|---|
| `X-User-Id` | `validate.UserId` (Guid) |
| `X-Username` | `validate.Username` |
| `X-Email` | `validate.Email` |

`Core.Mensajes.Api/Controllers/ContactosController.GetContactos` consume `X-User-Id` mediante `[FromHeader(Name = "x-user-id")]`.

### 3.3 Cookies de autenticación

SideCar.Auth.Api setea dos cookies HttpOnly en `register`, `login` y `refresh`:

| Cookie | Contiene | TTL | Path |
|---|---|---|---|
| `chat_auth` | access token (JWT) | `AccessTokenExpirationInMinutes` (15 min default) | `/` |
| `chat_refresh` | refresh token (string opaco persistido en `refresh_tokens`) | `RefreshTokenExpirationInDays` (7 días default) | `/api/v1/auth` |

Flags aplicadas:

- `HttpOnly` — JS no puede leerlas.
- `Secure` — solo se envían sobre HTTPS (configurable vía `JwtSettings:CookieSecureOnly`; poner `false` solo en dev local HTTP).
- `SameSite=Lax` — enviadas en requests same-site y en navegaciones top-level. Cambiable a `Strict` o `None` vía `JwtSettings:CookieSameSite`.

Nombres y TTL son configurables vía `JwtSettings:{AuthCookieName,RefreshCookieName,CookiePath,RefreshCookiePath,CookieSameSite,CookieSecureOnly}` en `appsettings.json` o env vars (`AUTH_COOKIE_NAME`, `REFRESH_COOKIE_NAME`, `COOKIE_SAME_SITE`, `COOKIE_SECURE_ONLY`).

### 3.4 Cómo llega el JWT al servidor

**Para el cliente browser:**

El browser envía automáticamente las cookies en cada request al mismo origen. nginx extrae el JWT de la cookie `chat_auth` y lo pasa al sub-request `/internal/auth` para validar. No requiere header `Authorization`.

**Para clientes no-browser (mobile, server-to-server):**

Enviar `Authorization: Bearer <access_token>` en el header. nginx prefiere el header Authorization sobre la cookie si ambos están presentes (la precedence la decide el `map` en `nginx.conf:8-20`).

**Para SideCar.Auth.Api misma (endpoint `/profile` con `[Authorize]`):**

El middleware `JwtBearer` lee el token desde la cookie `chat_auth` vía un `OnMessageReceived` configurado en `Program.cs`. Si la cookie no está presente, intenta leer del header `Authorization` (compatibilidad no-browser).

### 3.5 Diagrama de headers en una llamada autenticada

```
Cliente (browser)                              nginx                                   upstream
────────────────                              ─────                                   ────────
GET /api/v1/contacts?page=1&size=10           │
Cookie: chat_auth=eyJhbGciOi…                 │
                                              │
                                              ├─ sub-request POST /internal/auth ──► sidecar-auth-api/api/v1/auth/validate
                                              │   body: {"data":{"token":"eyJ…"}}                │ (token extraído
                                              │                                                 │  de cookie)
                                              │                                                 │ 200 OK
                                              │   ◄── X-User-Id, X-Username, X-Email ──────────  │
                                              │
                                              ├─ proxy_pass http://messages_api/api/v1/contacts
                                              │   + X-User-Id: f5f5…                          ──► core-mensajes-api
                                              │   + X-Username: ada                                  ContactosController
                                              │   + X-Email: ada@example.com                         lee x-user-id header
                                              │
                                              ◄── 200 JSON ────────────────────────────────────
```

Para clientes no-browser, el mismo flujo aplica sustituyendo `Cookie: chat_auth=...` por `Authorization: Bearer eyJ...`.

---

## 4. Auth API — `POST /api/v1/auth/register`

- **Upstream**: `sidecar-auth-api:5016` → `SideCar.Auth.Api/Controllers/AuthController.Register`
- **Auth requerida**: ❌ No
- **Content-Type**: `application/json`
- **Cookies emitidas**: `chat_auth`, `chat_refresh`

### Request

```jsonc
POST /api/v1/auth/register
Content-Type: application/json

{
  "header": { "transactionId": "…", "timestamp": "…", "device": "web" },
  "data": {
    "nombres": "Ada",
    "apellidos": "Lovelace",
    "numeroTelefono": "+5491112345678",
    "password": "P@ssword1",
    "email": "ada@example.com",
    "fechaNacimiento": "1990-12-10T00:00:00Z",
    "nombreUsuario": "ada"
  }
}
```

| Campo (`data.*`) | Tipo | Requerido | Reglas |
|---|---|---|---|
| `nombres` | string | ✅ | no vacío |
| `apellidos` | string | ✅ | no vacío |
| `numeroTelefono` | string | ✅ | regex E.164 `^\+?[1-9]\d{1,14}$` |
| `password` | string | ✅ | ≥ 6 caracteres |
| `email` | string | ✅ | email válido + único |
| `fechaNacimiento` | ISO-8601 / `DateTime` | ❌ | default `DateTime.MinValue` |
| `nombreUsuario` | string | ✅ | ≥ 3 caracteres |

### Respuestas

- **200 OK** → usuario creado y tokens emitidos:

  ```http
  HTTP/1.1 200 OK
  Set-Cookie: chat_auth=eyJhbGciOi…; HttpOnly; Secure; SameSite=Lax; Path=/; Max-Age=900
  Set-Cookie: chat_refresh=8d2…; HttpOnly; Secure; SameSite=Lax; Path=/api/v1/auth; Expires=…
  Content-Type: application/json

  {
    "success": true,
    "message": "Success",
    "errors": [],
    "data": {
      "email": "ada@example.com",
      "token": "<jwt_access>",
      "refreshToken": "<jwt_refresh>",
      "refreshTokenExpiration": "2026-06-28T15:30:00Z"
    }
  }
  ```

  El body sigue conteniendo `token` y `refreshToken` para clientes no-browser que no quieran o no puedan usar cookies. Los browsers pueden ignorar esos campos y usar solo las cookies.

- **400 Bad Request** → validación fallida (`RegisterUserValidator`):

  ```json
  {
    "success": false,
    "message": "Multiple errors occurred",
    "errors": ["El email ya se encuentra registrado.", "Número de teléfono no válido"],
    "data": null
  }
  ```

---

## 5. Auth API — `POST /api/v1/auth/login`

- **Upstream**: `sidecar-auth-api:5016` → `AuthController.Login`
- **Auth requerida**: ❌ No
- **Cookies emitidas**: `chat_auth`, `chat_refresh`

### Request

```jsonc
POST /api/v1/auth/login
Content-Type: application/json

{
  "header": { "transactionId": "…", "timestamp": "…", "device": "web" },
  "data": {
    "identifier": "ada",           // email o nombreUsuario
    "password": "P@ssword1"
  }
}
```

El servicio prueba primero `GetUserByEmail(identifier)` y luego `GetUserByUsername(identifier)`. Verifica la contraseña con `PasswordHasher<Usuario>`.

### Respuestas

- **200 OK** (`LoginResultDTO`):

  ```http
  HTTP/1.1 200 OK
  Set-Cookie: chat_auth=eyJhbGciOi…; HttpOnly; Secure; SameSite=Lax; Path=/; Max-Age=900
  Set-Cookie: chat_refresh=8d2…; HttpOnly; Secure; SameSite=Lax; Path=/api/v1/auth; Expires=…

  {
    "success": true,
    "message": "Success",
    "errors": [],
    "data": {
      "email": "ada@example.com",
      "token": "<jwt_access>",
      "refreshToken": "<jwt_refresh>",
      "refreshTokenExpiration": "2026-06-28T15:30:00Z"
    }
  }
  ```

- **400 Bad Request** → campos vacíos.
- **401 Unauthorized** → credenciales inválidas:

  ```json
  {
    "success": false,
    "message": "Login rechazado",
    "errors": ["Credenciales inválidas"],
    "data": null
  }
  ```

---

## 6. Auth API — `POST /api/v1/auth/refresh`

- **Upstream**: `sidecar-auth-api:5016` → `AuthController.Refresh`
- **Auth requerida**: ❌ No (refresh token en cookie `chat_refresh`, body opcional)
- **Cookies emitidas**: `chat_auth`, `chat_refresh` (rotación)

### Request

El browser debe enviar la cookie `chat_refresh` (path `/api/v1/auth`); no requiere body. Clientes no-browser pueden enviar `data.refreshToken` en el body.

```jsonc
POST /api/v1/auth/refresh
Content-Type: application/json
Cookie: chat_refresh=<jwt_refresh>

{
  "header": { "transactionId": "…", "timestamp": "…", "device": "web" },
  "data": {
    "refreshToken": "<jwt_refresh>"   // opcional; la cookie tiene prioridad
  }
}
```

| Campo | Tipo | Notas |
|---|---|---|
| `data.refreshToken` | string | opcional. La cookie `chat_refresh` tiene prioridad; el body se usa solo si la cookie no está presente. |
| `data.token` | string | opcional (legacy). Si se envía, el servicio verifica que su `userId` coincida con el del refresh token (defensa contra robo). |

El servicio lee el refresh token (cookie > body), lo busca en `refresh_tokens`, y verifica `!EstaRevocado && FechaExpiracion > UtcNow`. Si todo está OK, lo marca revocado y emite un par nuevo vía `ITokenService.GenerarTokens`.

### Respuestas

- **200 OK** (`TokenResponseDto`) — cookies rotadas:

  ```http
  HTTP/1.1 200 OK
  Set-Cookie: chat_auth=<jwt_access_nuevo>; HttpOnly; Secure; SameSite=Lax; Path=/; Max-Age=900
  Set-Cookie: chat_refresh=<jwt_refresh_nuevo>; HttpOnly; Secure; SameSite=Lax; Path=/api/v1/auth; Expires=…

  {
    "success": true,
    "message": "Success",
    "errors": [],
    "data": {
      "accessToken": "<jwt_access_nuevo>",
      "refreshToken": "<jwt_refresh_nuevo>",
      "refreshTokenExpiration": "2026-06-28T15:30:00Z"
    }
  }
  ```

- **401 Unauthorized** → `SecurityTokenException`. **Las dos cookies se borran** en la response (forzar re-login):

  ```json
  {
    "success": false,
    "message": "Refresh token rechazado",
    "errors": ["Refresh token inválido o expirado"],
    "data": null
  }
  ```

---

## 7. Auth API — `PUT /api/v1/auth/profile`

- **Upstream**: `sidecar-auth-api:5016` → `AuthController.UpdateProfile`
- **Auth requerida**: ✅ JWT — nginx `auth_request /internal/auth` inyecta `X-User-Id`; el middleware `JwtBearer` valida firma/issuer/audience/lifetime desde la cookie `chat_auth` (vía `OnMessageReceived` en `Program.cs`) o del header `Authorization: Bearer`.
- **Autorización interna**: `[Authorize]` + claim `ClaimTypes.NameIdentifier` (Guid `userId`).

### Request

```http
PUT /api/v1/auth/profile
Cookie: chat_auth=<jwt_access>
Content-Type: application/json

{
  "header": { "transactionId": "…", "timestamp": "…", "device": "web" },
  "data": {
    "nombres": "Augusta",
    "apellidos": "King",
    "numeroTelefono": "+442071838750",
    "fechaNacimiento": "1815-12-10T00:00:00Z",
    "avatarUrl": "https://cdn.example.com/augusta.png"
  }
}
```

> Clientes no-browser pueden usar `Authorization: Bearer <jwt_access>` en lugar de la cookie.

> Todos los campos son **opcionales** (parche parcial). Solo se actualizan los que vienen no-nulos.
> Si `fechaNacimiento` es `DateTime.MinValue`, se interpreta como "borrar fecha".

### Respuestas

- **200 OK** (`UpdateUserResultDTO`):

  ```json
  {
    "success": true,
    "message": "Success",
    "errors": [],
    "data": {
      "id": "f5f5…",
      "email": "ada@example.com",
      "nombreUsuario": "ada",
      "nombres": "Augusta",
      "apellidos": "King",
      "numeroTelefono": "+442071838750",
      "fechaNacimiento": "1815-12-10T00:00:00Z",
      "avatarUrl": "https://cdn.example.com/augusta.png"
    }
  }
  ```

- **400 Bad Request** → validación FluentValidation.
- **401 Unauthorized** → JWT inválido/ausente o `userId` no parseable.

---

## 8. Auth API — `POST /api/v1/auth/logout`

- **Upstream**: `sidecar-auth-api:5016` → `AuthController.Logout`
- **Auth requerida**: ❌ No (la presencia de la cookie es suficiente para identificar la sesión a cerrar)
- **Cookies emitidas**: `Set-Cookie: chat_auth=; … Max-Age=0` y `Set-Cookie: chat_refresh=; … Max-Age=0`

### Request

```http
POST /api/v1/auth/logout
Cookie: chat_refresh=<jwt_refresh>
```

No requiere body. Si la cookie `chat_refresh` está presente, el refresh token se marca `EstaRevocado=true` en la DB; si está ausente o ya revocado, la operación es no-op (idempotente).

### Respuestas

- **200 OK**:

  ```http
  HTTP/1.1 200 OK
  Set-Cookie: chat_auth=; HttpOnly; Secure; SameSite=Lax; Path=/; Max-Age=0
  Set-Cookie: chat_refresh=; HttpOnly; Secure; SameSite=Lax; Path=/api/v1/auth; Max-Age=0

  {
    "success": true,
    "message": "Success",
    "errors": [],
    "data": { "message": "Sesión cerrada" }
  }
  ```

Logout funciona aunque el access token ya haya expirado (es lo normal). El browser ya no podrá renovar; cualquier próximo request a un endpoint protegido devolverá 401.

---

## 9. Auth API — `POST /internal/auth/validate` (interno)

> **No es accesible al cliente.** nginx lo invoca como `auth_request` antes de cualquier endpoint protegido. Está marcado `internal;` en `nginx.conf`.

- **Upstream**: `sidecar-auth-api:5016/api/v1/auth/validate`
- **Auth requerida**: ❌ (el JWT se pasa en el body, extraído por nginx del header `Authorization` o de la cookie `chat_auth`)

### Cómo lo llama nginx

nginx extrae el JWT con dos `map`:

```nginx
map $http_authorization $bearer_token {
    default "";
    "~^Bearer\s+(?<t>.+)$" $t;
}

map $http_cookie $cookie_token {
    default "";
    ~*chat_auth=(?<t>[^;]+) $t;
}

map "$bearer_token:$cookie_token" $jwt_token {
    default $cookie_token;     # fallback si no hay Bearer
    "~^[^:]+:" $bearer_token;  # Bearer tiene prioridad si está presente
}
```

El Bearer del header `Authorization` se prefiere cuando está presente; si no, se usa el valor de la cookie `chat_auth`. Esto preserva la compatibilidad con clientes no-browser y con tests/CI.

Luego construye un body fijo:

```jsonc
POST /internal/auth
{
  "header": { "transactionId": "nginx-sidecar", "timestamp": "<iso8601>", "device": "nginx" },
  "data":   { "token": "<jwt>" }
}
```

### Respuestas

- **200 OK** → JWT válido. Cabeceras de respuesta pobladas por `AuthController.Validate`:

  | Cabecera | Ejemplo |
  |---|---|
  | `X-User-Id` | `f5f5f5f5-…` |
  | `X-Username` | `ada` |
  | `X-Email` | `ada@example.com` |

  ```json
  {
    "success": true,
    "message": "Success",
    "errors": [],
    "data": {
      "valid": true,
      "reason": null,
      "userId": "f5f5…",
      "username": "ada",
      "email": "ada@example.com",
      "expiresAt": "2026-06-21T15:45:00Z"
    }
  }
  ```

- **401 Unauthorized** → JWT inválido/expirado; nginx devuelve al cliente `{"error":"Unauthorized"}`.

---

## 10. Messages API — `GET /api/v1/messages`

- **Upstream**: `core-mensajes-api:5017` → `Core.Mensajes.Api/Controllers/MensajesController.GetMensajes`
- **Auth requerida**: ✅ JWT (nginx `auth_request`). Cabeceras `X-User-Id`/`X-Username`/`X-Email` se reenvían aunque el controlador **no las usa** (la autorización fina por participante está fuera de este endpoint).
- **Validación**: `conversationId` GUID, `page` y `size` enteros > 0. Si faltan o son inválidos → `400`.

### Request

```http
GET /api/v1/messages?conversationId=<guid>&page=1&size=20
Cookie: chat_auth=<jwt_access>
```

| Query | Tipo | Requerido | Notas |
|---|---|---|---|
| `conversationId` | GUID | ✅ | `400` si falta |
| `page` | int > 0 | ✅ | `400` si inválido |
| `size` | int > 0 | ✅ | `400` si inválido. El servicio clamp a `[1, 100]` y por defecto `10`. |

### Respuestas

- **200 OK** (`MsResponse<PagedResponse<MessageReponseDTO>>`):

  ```json
  {
    "success": true,
    "message": "Success",
    "errors": [],
    "data": {
      "pageNumber": 1,
      "pageSize": 20,
      "totalPages": 3,
      "totalRecords": 47,
      "data": [
        {
          "id": "3b1d…",
          "tipoMensaje": "TEXTO",
          "contenido": "Hola!",
          "fechaCreacion": "2026-06-21T15:30:00Z",
          "emisorId": "f5f5…",
          "emisorNombre": "ada"
        }
      ]
    }
  }
  ```

- **400 Bad Request** → faltan/invalidan query params.

Definido en `Core.Mensajes.Api/Domain/DTOS/MessageReponseDTO.cs` y `Core.Mensajes.Api/Domain/Entities/PagedResponse.cs`.

---

## 11. Contacts API — `GET /api/v1/contacts`

- **Upstream**: `core-mensajes-api:5017` → `Core.Mensajes.Api/Controllers/ContactosController.GetContactos`
- **Auth requerida**: ✅ JWT (nginx `auth_request`)
- **Cabecera crítica**: `x-user-id` (Guid). Aunque nginx siempre la inyecta tras `auth_request`, el controlador la lee explícitamente con `[FromHeader(Name = "x-user-id")]` y valida el formato. Si falta o no es GUID → `400`.

### Request

```http
GET /api/v1/contacts?page=1&size=20
Cookie: chat_auth=<jwt_access>
```

`x-user-id`, `x-username` y `x-email` se inyectan automáticamente por nginx, pero `x-user-id` es la que el backend consume.

| Query | Tipo | Requerido | Notas |
|---|---|---|---|
| `page` | int > 0 | ✅ | `400` si inválido |
| `size` | int > 0 | ✅ | clamp del servicio a `[1, 100]`, default `10` |

### Respuestas

- **200 OK** (`MsResponse<PagedResponse<ContactoListadoDTO>>`):

  ```json
  {
    "success": true,
    "message": "Success",
    "errors": [],
    "data": {
      "pageNumber": 1,
      "pageSize": 20,
      "totalPages": 1,
      "totalRecords": 2,
      "data": [
        {
          "id": "9c4a…",
          "nombreUsuario": "babbage",
          "email": "charles@example.com",
          "nombres": "Charles",
          "apellidos": "Babbage",
          "fechaNacimiento": "1791-12-26T00:00:00Z",
          "numeroTelefono": "+447…",
          "avatarUrl": "https://cdn.example.com/cb.png",
          "creadoAt": "2026-05-01T12:00:00Z",
          "ultimoMensaje": {
            "id": "aa12…",
            "contenido": "ok",
            "fechaCreacion": "2026-06-20T18:11:00Z",
            "tipoMensaje": "TEXTO",
            "emisorId": "f5f5…"
          }
        }
      ]
    }
  }
  ```

- **400 Bad Request** → falta/incorrecto `x-user-id`, `page` o `size`.

---

## 12. Contacts API — `GET /api/v1/contacts/{id}`

- **Upstream**: `core-mensajes-api:5017` → `Core.Mensajes.Api/Controllers/ContactosController.GetContactoById`
- **Auth requerida**: ✅ JWT (nginx `auth_request`)
- **Route param**: `{id}` Guid.

### Request

```http
GET /api/v1/contacts/9c4a4a4a-0000-0000-0000-000000000000
Cookie: chat_auth=<jwt_access>
```

### Respuestas

- **200 OK** (`MsResponse<ContactoResponseDTO>`):

  ```json
  {
    "success": true,
    "message": "Success",
    "errors": [],
    "data": {
      "id": "9c4a…",
      "nombreUsuario": "babbage",
      "email": "charles@example.com",
      "nombres": "Charles",
      "apellidos": "Babbage",
      "fechaNacimiento": "1791-12-26T00:00:00Z",
      "numeroTelefono": "+447…",
      "avatarUrl": "https://cdn.example.com/cb.png",
      "creadoAt": "2026-05-01T12:00:00Z"
    }
  }
  ```

- **404 Not Found** → contacto inexistente:

  ```json
  {
    "success": false,
    "message": "An error occurred",
    "errors": ["No se encontró un contacto con el id {id}"],
    "data": null
  }
  ```

---

## 13. WebSocket — `GET /ws`

- **Upstream**: `ws_nodes` (least_conn sobre `ws-node-1/2/3:4001`) → `websocket-server/src/infrastructure/http/HonoApp.ts`
- **Auth requerida**: ✅ JWT (nginx `auth_request`). Tras validar, inyecta `X-User-Id` y `X-Username` como cabeceras HTTP; el nodo WS las lee con `c.req.header('x-username')` / `c.req.header('x-user-id')` (nunca toca JWT).
- **Transporte**: WebSocket (`Upgrade: websocket`, `Connection: upgrade`).
- **Cabeceras de proxy**: `proxy_read_timeout` y `proxy_send_timeout = 3600s`, `proxy_buffering off`.
- **CORS**: Habilitado por Hono contra `env.CLIENT_ORIGIN` (`http://localhost` por defecto).

### 13.1 Apertura de la conexión

**Cliente browser:**

```http
GET /ws HTTP/1.1
Host: localhost
Upgrade: websocket
Connection: Upgrade
Cookie: chat_auth=<jwt_access>
Sec-WebSocket-Key: …
Sec-WebSocket-Version: 13
```

El browser adjunta automáticamente la cookie `chat_auth`. nginx la extrae, llama a `/internal/auth`, y forwardea al WS-nodo con `X-Username` y `X-User-Id`. El nodo crea `sessionId = randomUUID()`.

**Cliente no-browser:**

```http
GET /ws HTTP/1.1
Host: localhost
Upgrade: websocket
Connection: Upgrade
Authorization: Bearer <jwt_access>
Sec-WebSocket-Key: …
Sec-WebSocket-Version: 13
```

nginx prefiere `Authorization` sobre la cookie. El resto del handshake es idéntico.

> **Importante**: el browser no permite setear headers custom en `new WebSocket(url)`, incluido `Authorization`. Por eso este sistema usa cookie para el browser (envío automático) y `Authorization: Bearer` para clientes no-browser.

### 13.2 Envelope del cliente (entrante)

Todos los mensajes del cliente son JSON con forma:

```jsonc
{ "type": "<nombre_evento>", "payload": { /* … */ } }
```

Si el JSON está malformado, el servidor lo descarta silenciosamente (`ChatUseCase.handleMessage`).

### 13.3 Eventos del cliente → servidor

| `type` | `payload` | Validación | Efecto |
|---|---|---|---|
| `chat:message` | `{ id?, conversacionId, text }` | `text` no vacío, ≤ 1000 chars, `conversacionId` obligatorio | Difunde `chat:message` por Redis Pub/Sub **y** publica `MensajeEnviadoEvent` en RabbitMQ (exchange `…:MensajeEnviadoEvent`) |
| `chat:iniciar_individual` | `{ id?, receptorId, text }` | `text` no vacío, ≤ 1000 chars, `receptorId` obligatorio | Difunde `chat:message` por Redis **y** publica `IniciarChatIndividualEvent` (exchange `…:IniciarChatIndividualEvent`) para crear conversación 1-a-1 + mensaje |
| `chat:typing` | `boolean` (se evalúa con `Boolean(payload)`) | — | Difunde `chat:typing` por Redis |
| `chat:leido` | `{ conversacionId }` | `conversacionId` obligatorio | Publica `ChatLeidoEvent` (exchange `…:ChatLeidoEvent`). **No** se difunde por Redis |

### 13.4 Eventos del servidor → cliente

El servidor adapta cada `ChatEvent` a un payload de cliente en `HonoApp.toClientPayload(event)`:

| `type` saliente | `payload` | Origen |
|---|---|---|
| `chat:message` | `{ id, username, text, timestamp }` | recibido de Redis Pub/Sub |
| `chat:typing` | `{ username, isTyping }` | recibido de Redis Pub/Sub |
| `system:message` | `{ id: "sys-<ms>", text: "<u> se ha unido al chat", timestamp }` o `"<u> ha salido del chat"` | `user:joined` / `user:left` reempaquetados |
| `users:online` | `string[]` (array de usernames en línea) | `roster:updated` |

### 13.5 Eventos publicados en RabbitMQ (worker)

`RabbitMQMessagePublisher` (websocket-server) envía envelopes compatibles con MassTransit (`Consumer.Messaging.Worker.Domain.Events.*`) a los exchanges fanout durables:

| Evento | Exchange | Payload interno (`message.*`) |
|---|---|---|
| `MensajeEnviadoEvent` | `Consumer.Messaging.Worker.Domain.Events:MensajeEnviadoEvent` | `{ MensajeId, ConversacionId, EmisorId, Contenido, TipoMensajeCodigo: "TEXTO" }` |
| `IniciarChatIndividualEvent` | `Consumer.Messaging.Worker.Domain.Events:IniciarChatIndividualEvent` | `{ MensajeId, EmisorId, ReceptorId, Contenido, TipoMensajeCodigo: "TEXTO" }` |
| `ChatLeidoEvent` | `Consumer.Messaging.Worker.Domain.Events:ChatLeidoEvent` | `{ ConversacionId, UsuarioId, LeidoAt }` |

El consumer `.NET` (`Consumer.Messaging.Worker`) persiste los eventos a PostgreSQL y, opcionalmente, lee desde Redis para notificaciones.

### 13.6 Eventos de ciclo de vida (internos del WS-nodo)

| Disparador | Evento Redis | Notas |
|---|---|---|
| Cliente abre WS (`onOpen`) | `user:joined` + `roster:updated` | incrementa contador de presencia en Redis |
| Cliente cierra WS (`onClose`) | `roster:updated` siempre; `user:left` solo si contador llega a 0 | decrementa contador de presencia |

### 13.7 Restricciones y reglas de negocio

- **Longitud máxima de mensaje**: `MAX_TEXT_LENGTH = 1000` (`websocket-server/src/application/ChatUseCase.ts`).
- **Sanitizado**: `text.trim()`, IDs coercidos a `String`.
- **Anonimato**: si nginx no inyecta `X-Username`, el WS-nodo usa `'anonymous'` (solo fallback de desarrollo).
- **Sin acuse**: el WS-nodo **no** envía `ack`/`callback` por mensaje; la confirmación real es la retransmisión del `chat:message` por Redis Pub/Sub.

### 13.8 Flujo extremo a extremo (ejemplo: enviar mensaje)

```
[Cliente] ─► JSON {type:"chat:message", payload:{conversacionId,text}}
   │ (wss://…/ws)
   ▼
nginx /ws ──► auth_request /internal/auth  ──► sidecar-auth-api OK
   │   inyecta X-Username, X-User-Id
   ▼
ws-node-X (HonoApp.onMessage → ChatUseCase.handleMessage)
   │
   ├─► RedisEventBroker.publish('chat:message')   ──► Redis Pub/Sub (channel "chat:events")
   │       │
   │       ▼ (todos los ws-node reciben)
   │       registry.broadcast(toClientPayload(event))
   │           └─► ws.send(JSON) a TODOS los sockets locales (incluido el emisor)
   │
   └─► RabbitMQMessagePublisher.publishMensajeEnviado(payload)
           └─► exchange "Consumer.Messaging.Worker.Domain.Events:MensajeEnviadoEvent"
                   └─► Consumer.Messaging.Worker persiste en PostgreSQL (tabla `mensajes`)
```

---

## 14. Health — `GET /health`

- **Upstream**: `ws_nodes` (Hono) — `websocket-server/src/infrastructure/http/HonoApp.ts`
- **Auth requerida**: ❌ No

### Request

```http
GET /health
```

### Respuesta

- **200 OK** (`HonoApp`):

  ```json
  { "status": "ok", "time": "2026-06-21T15:30:00.123Z" }
  ```

---

## 15. Códigos de error transversales

| Código | Origen | Cuerpo | Causa |
|---|---|---|---|
| `400 Bad Request` | API .NET | `MsResponse.Fail(...)` | Validación FluentValidation o query/headers faltantes |
| `401 Unauthorized` | `AuthController.Login`/`Refresh`/`Validate`/`UpdateProfile` | `MsResponse.Fail("…", "Login rechazado" / "Refresh token rechazado" / "Token inválido" / "Token no contiene un userId válido")` | Credenciales inválidas, JWT malformado/expirado |
| `404 Not Found` | `MensajesController`, `ContactosController.GetContactoById` | `MsResponse.Fail(...)` | Recurso inexistente |
| `401 Unauthorized` (nginx) | `error_page 401 = @error_401` | `{"error":"Unauthorized"}` | Cualquier endpoint protegido cuyo `auth_request /internal/auth` falla |
| `502/503/504 Bad Gateway / Service Unavailable` | `error_page 502 503 504 = @error_upstream` | `{"error":"Service unavailable"}` | Upstream no responde |
| `503 Service Unavailable` | nginx upstream | idem | `ws_nodes` cae |

> **Refresh + cookies**: cuando `POST /api/v1/auth/refresh` falla con 401, la response incluye `Set-Cookie: chat_auth=; Max-Age=0` y `Set-Cookie: chat_refresh=; Max-Age=0`. El browser borra ambas cookies y el cliente puede redirigir al login sin más limpieza manual.

> **Logout**: idempotente. Si la cookie `chat_refresh` está ausente o ya revocado, devuelve 200 igualmente con `Set-Cookie` de borrado.

---

## Apéndice A — Resumen del flujo de auth

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ BROWSER                                                                     │
│  1. POST /api/v1/auth/login {identifier, password}                           │
│     ◄── 200 + Set-Cookie: chat_auth=<jwt>; HttpOnly; Secure; SameSite=Lax   │
│     ◄── 200 + Set-Cookie: chat_refresh=<rt>; HttpOnly; …; Path=/api/v1/auth │
│                                                                              │
│  2. (15 min después) access token expira → próximo request protegido 401    │
│                                                                              │
│  3. POST /api/v1/auth/refresh    (cookie chat_refresh adjunta sola)          │
│     ◄── 200 + Set-Cookie: chat_auth=<jwt_nuevo>; …                          │
│     ◄── 200 + Set-Cookie: chat_refresh=<rt_nuevo>; …   (rotación)           │
│                                                                              │
│  4. new WebSocket("/ws")   (browser adjunta cookie chat_auth automáticamente)│
│     ◄── 101 Switching Protocols (válido)                                     │
│                                                                              │
│  5. fetch("/api/v1/messages?…")   (cookie chat_auth adjunta)                │
│     ◄── 200 con MsResponse<…>                                               │
│                                                                              │
│  6. POST /api/v1/auth/logout                                                 │
│     ◄── 200 + Set-Cookie: chat_auth=; Max-Age=0                             │
│     ◄── 200 + Set-Cookie: chat_refresh=; Max-Age=0                          │
└──────────────────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────────────────┐
│ NO-BROWSER (mobile / server-to-server / tests)                              │
│  1. POST /api/v1/auth/login {identifier, password}                           │
│     ◄── 200 {data:{token, refreshToken}}                                    │
│                                                                              │
│  2. GET /api/v1/messages?…  Authorization: Bearer <token>                   │
│     ◄── 200 con MsResponse<…>                                               │
│                                                                              │
│  3. POST /api/v1/auth/refresh {refreshToken}                                │
│     ◄── 200 {data:{accessToken, refreshToken}}   (rotación)                 │
│                                                                              │
│  4. new WebSocket("/ws") no es viable: el browser-like API de WebSocket     │
│     no permite setear headers custom. Workaround con cookie manual o usar  │
│     el truco Sec-WebSocket-Protocol (no soportado por esta versión).        │
│     Para WebSocket no-browser, considerar un cliente HTTP que primero       │
│     intercambie la cookie y luego abra WS.                                  │
└──────────────────────────────────────────────────────────────────────────────┘
```

---

## Apéndice B — Configuración de cookies (`appsettings.json`)

```jsonc
"JwtSettings": {
  // ...existing...
  "AccessTokenExpirationInMinutes": 15,
  "RefreshTokenExpirationInDays": 7,

  "AuthCookieName":       "chat_auth",     // override vía AUTH_COOKIE_NAME
  "RefreshCookieName":    "chat_refresh",  // override vía REFRESH_COOKIE_NAME
  "CookiePath":           "/",             // path del access token
  "RefreshCookiePath":    "/api/v1/auth",  // path del refresh token (más restrictivo)
  "CookieSameSite":       "Lax",           // "Strict" | "Lax" | "None"
  "CookieSecureOnly":     true             // false solo para dev HTTP local
}
```

Notas operativas:

- **`SameSite=None` requiere `Secure=true`**. Si la app corre tras un HTTPS público, podés usar `None; Secure` para entornos cross-origin (front en otro dominio).
- **Dev local con cliente en `localhost:5173` y API en `localhost:80`**: el browser trata los puertos como orígenes distintos. Con `SameSite=Lax` las cookies no se envían en POST cross-origin → usar Vite proxy (`server.proxy['/api']`) para que el front quede same-origin con la API.
- **`CookieSecureOnly=false` en dev**: necesario si vas a navegar por `http://localhost`. En prod, siempre `true`.
- **Rotación de refresh tokens**: `chat_refresh` se reemplaza en cada `/refresh`. El token viejo queda marcado `EstaRevocado=true` y ya no se puede reutilizar.

---

## Apéndice C — Envelope RabbitMQ (formato MassTransit)

`RabbitMQMessagePublisher.buildEnvelope` genera este cuerpo (campos relevantes):

```jsonc
{
  "messageId":          "<uuid>",
  "conversationId":     "<uuid_nuevo_por_publish>",
  "sourceAddress":      "rabbitmq://rabbitmq/websocket-server",
  "destinationAddress": "rabbitmq://rabbitmq/<exchange>",
  "messageType":        ["urn:message:<exchange>"],
  "message":            { /* payload del evento */ },
  "host": {
    "machineName": "websocket-server",
    "processName": "websocket-server",
    "frameworkVersion": "node",
    "massTransitVersion": "9.0.0"
  },
  "sentTime":       "<iso8601>",
  "expirationTime": null
}
```

Es un sobre MassTransit estándar; el consumer (`Consumer.Messaging.Worker`) extrae `message.*` y descarta el resto.

---

**Versión del documento**: 2026-06-21
**Fuentes**: `nginx/nginx.conf`, `docker-compose.yml`, `SideCar.Auth.Api/Controllers/AuthController.cs`, `SideCar.Auth.Api/Program.cs`, `SideCar.Auth.Api/InfraStructure/CookieAuth/AuthCookies.cs`, `SideCar.Auth.Api/Application/Services/TokenService.cs`, `Core.Mensajes.Api/Controllers/*.cs`, `websocket-server/src/infrastructure/http/HonoApp.ts`, `websocket-server/src/application/ChatUseCase.ts`, `websocket-server/src/domain/ChatEvent.ts`, `websocket-server/src/domain/ports/MessagePublisher.ts`.