//  Middleware de autenticación para Socket.IO
import { verifyToken } from "../auth/jwt.js";

export function socketAuthMiddleware(socket, next) {
  const token = socket.handshake.auth?.token;

  if (!token) {
    return next(new Error("No se proporcionó token de autenticación."));
  }

  try {
    const payload = verifyToken(token);
    socket.username = payload.username; 
    return next();
  } catch (err) {
    return next(new Error("Token inválido o expirado."));
  }
}
