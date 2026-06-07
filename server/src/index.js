//  PUNTO DE ENTRADA DEL SERVIDOR
import "dotenv/config";
import http from "node:http";
import path from "node:path";
import { fileURLToPath } from "node:url";
import express from "express";
import cors from "cors";
import { Server } from "socket.io";

import authRoutes from "./auth/authRoutes.js";
import { socketAuthMiddleware } from "./socket/socketAuth.js";
import { registerChatHandlers } from "./socket/chatHandlers.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

const PORT = process.env.PORT || 4000;
const CLIENT_ORIGIN = process.env.CLIENT_ORIGIN || "http://localhost:5173";

const app = express();

app.use(cors({ origin: CLIENT_ORIGIN }));
app.use(express.json());

app.use("/api", authRoutes);


app.get("/api/health", (_req, res) => {
  res.json({ status: "ok", time: new Date().toISOString() });
});


const clientDist = path.resolve(__dirname, "../../client/dist");
app.use(express.static(clientDist));
app.get("*", (req, res, next) => {
  if (req.path.startsWith("/api")) return next();
  res.sendFile(path.join(clientDist, "index.html"), (err) => {
    if (err) next();
  });
});

const server = http.createServer(app);

const io = new Server(server, {
  cors: { origin: CLIENT_ORIGIN, methods: ["GET", "POST"] },
});

io.use(socketAuthMiddleware);

io.on("connection", (socket) => {
  console.log(`🔌 Conectado: ${socket.username} (${socket.id})`);
  registerChatHandlers(io, socket);

  socket.on("disconnect", () => {
    console.log(`Desconectado: ${socket.username} (${socket.id})`);
  });
});

server.listen(PORT, () => {
  console.log(`Servidor escuchando en http://localhost:${PORT}`);
  console.log(`   CORS permitido para: ${CLIENT_ORIGIN}`);
});
