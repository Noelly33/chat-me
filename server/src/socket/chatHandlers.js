import { addOnlineUser, removeOnlineUser, getRoster, setLastSeen, isUserStillOnline,} from "../store/userStore.js";

const MAX_MESSAGE_LENGTH = 1000;

function makeId() {
  return `${Date.now()}-${Math.random().toString(36).slice(2, 9)}`;
}

export function registerChatHandlers(io, socket) {
  const username = socket.username;

  addOnlineUser(socket.id, username);

  io.emit("users:online", getRoster());

  socket.broadcast.emit("system:message", {
    id: makeId(),
    text: `${username} se ha unido al chat`,
    timestamp: new Date().toISOString(),
  });

  socket.on("chat:message", (payload, ack) => {
    const raw = typeof payload === "string" ? payload : payload?.text;
    const text = (raw ?? "").toString().trim();

    if (!text) {
      if (typeof ack === "function") ack({ ok: false, error: "Mensaje vacío." });
      return;
    }
    if (text.length > MAX_MESSAGE_LENGTH) {
      if (typeof ack === "function") {
        ack({ ok: false, error: "El mensaje es demasiado largo." });
      }
      return;
    }

    const message = {
      id: makeId(),
      username,
      text,
      timestamp: new Date().toISOString(),
    };

    io.emit("chat:message", message);

    if (typeof ack === "function") ack({ ok: true, id: message.id });
  });

  socket.on("chat:typing", (isTyping) => {
    socket.broadcast.emit("chat:typing", {
      username,
      isTyping: Boolean(isTyping),
    });
  });

  socket.on("disconnect", () => {
    removeOnlineUser(socket.id);

    if (!isUserStillOnline(username)) {
      setLastSeen(username);
      io.emit("system:message", {
        id: makeId(),
        text: `${username} ha salido del chat`,
        timestamp: new Date().toISOString(),
      });
    }

    io.emit("users:online", getRoster());
  });
}
