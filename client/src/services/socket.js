import { io } from "socket.io-client";

const SERVER_URL = import.meta.env.VITE_SERVER_URL || "http://localhost:4000";

export function createSocket(token) {
  return io(SERVER_URL, {
    auth: { token },
    autoConnect: false,
    transports: ["websocket", "polling"],
  });
}
