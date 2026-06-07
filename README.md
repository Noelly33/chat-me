# Sistema de Chat en Tiempo Real

ChatMe permite a múltiples usuarios autenticarse y comunicarse de forma instantánea a través de un servidor centralizado.

## Características

- Autenticación con registro e inicio de sesión (contraseñas con `bcrypt` + JWT).
- Mensajería en tiempo real mediante Socket.IO (WebSockets).
- Lista de usuarios conectados actualizada en vivo.
- Indicador de "escribiendo...".
- Avisos del sistema cuando un usuario entra o sale.
- Interfaz responsiva (React + Vite).
- Conexión de socket protegida, solo entra quien tiene un token válido.

## Tecnologías

| Capa      | Tecnologías                                              |
|-----------|---------------------------------------------------------|
| Servidor  | Node.js, Express, Socket.IO, jsonwebtoken, bcryptjs      |
| Cliente   | React, Vite, socket.io-client                           |

## Ejecución local

### Requisitos
- Node.js 18 o superior

### 1. Servidor

```bash
cd server
npm install
cp .env.example .env     
npm run dev               # arranca en http://localhost:4000
```

### 2. Cliente 
```bash
cd client
npm install
cp .env.example .env      
npm run dev               # arranca en http://localhost:5173
```

Abre **http://localhost:5173** en el navegador. Para probar el chat con varios usuarios, abre dos ventanas o una en incógnito y regístrate con cuentas distintas.

---

## Variables de entorno

### `server/.env`
| Variable         | Descripción                                  |
|------------------|----------------------------------------------|
| `PORT`           | Puerto del servidor (por defecto 4000)       |
| `JWT_SECRET`     | Secreto para firmar los JWT                   |
| `JWT_EXPIRES_IN` | Expiración del token (ej. `2h`)               |
| `CLIENT_ORIGIN`  | Origen permitido para CORS (URL del cliente)  |

### `client/.env`
| Variable           | Descripción                              |
|--------------------|------------------------------------------|
| `VITE_SERVER_URL`  | URL del servidor (REST + Socket.IO)      |

---

