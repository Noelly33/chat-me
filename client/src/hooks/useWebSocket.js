import { useEffect, useRef, useState } from "react";
import { createSocket } from "../services/ws.js";

const MAX_BACKOFF_MS = 30000;

export function useWebSocket({ enabled, onMessage, onTyping, onSystemMessage, onOnlineUsers }) {
  const [status, setStatus] = useState("idle"); // idle | connecting | open | reconnecting
  const socketRef = useRef(null);
  const attemptRef = useRef(0);
  const retryTimerRef = useRef(null);
  const closedByUserRef = useRef(false);

  const handlersRef = useRef({});
  handlersRef.current = { onMessage, onTyping, onSystemMessage, onOnlineUsers };

  useEffect(() => {
    if (!enabled) return undefined;

    closedByUserRef.current = false;

    function connect() {
      setStatus(attemptRef.current === 0 ? "connecting" : "reconnecting");
      const socket = createSocket();
      socketRef.current = socket;

      socket.onOpen(() => {
        attemptRef.current = 0;
        setStatus("open");
      });

      socket.on("chat:message", (payload) => handlersRef.current.onMessage?.(payload));
      socket.on("chat:typing", (payload) => handlersRef.current.onTyping?.(payload));
      socket.on("system:message", (payload) => handlersRef.current.onSystemMessage?.(payload));
      socket.on("users:online", (payload) => handlersRef.current.onOnlineUsers?.(payload));

      socket.onClose(() => {
        if (closedByUserRef.current) return;
        setStatus("reconnecting");
        const delay = Math.min(1000 * 2 ** attemptRef.current, MAX_BACKOFF_MS);
        attemptRef.current += 1;
        retryTimerRef.current = setTimeout(connect, delay);
      });

      socket.onError(() => {
        socket.raw.close();
      });
    }

    connect();

    return () => {
      closedByUserRef.current = true;
      clearTimeout(retryTimerRef.current);
      socketRef.current?.close();
      socketRef.current = null;
      attemptRef.current = 0;
      setStatus("idle");
    };
  }, [enabled]);

  function send(type, payload) {
    socketRef.current?.send(type, payload);
  }

  return {
    status,
    sendMessage: (conversacionId, text) => send("chat:message", { conversacionId, text }),
    sendIniciarIndividual: (receptorId, text) => send("chat:iniciar_individual", { receptorId, text }),
    sendTyping: (isTyping) => send("chat:typing", isTyping),
    sendLeido: (conversacionId) => send("chat:leido", { conversacionId }),
  };
}
