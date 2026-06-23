const API_BASE_URL = (import.meta.env.VITE_API_URL ?? "").replace(/\/+$/, "");

function resolveWsUrl() {
  if (API_BASE_URL) {
    const url = new URL(API_BASE_URL);
    const scheme = url.protocol === "https:" ? "wss:" : "ws:";
    return `${scheme}//${url.host}/ws`;
  }
  const scheme = location.protocol === "https:" ? "wss:" : "ws:";
  return `${scheme}//${location.host}/ws`;
}

export function createSocket() {
  const socket = new WebSocket(resolveWsUrl());

  const listeners = new Map();

  socket.onmessage = (event) => {
    let parsed;
    try {
      parsed = JSON.parse(event.data);
    } catch {
      return;
    }
    const handlers = listeners.get(parsed.type);
    if (handlers) {
      handlers.forEach((handler) => handler(parsed.payload));
    }
  };

  return {
    raw: socket,
    send(type, payload) {
      if (socket.readyState !== WebSocket.OPEN) return;
      socket.send(JSON.stringify({ type, payload }));
    },
    on(type, handler) {
      if (!listeners.has(type)) listeners.set(type, new Set());
      listeners.get(type).add(handler);
      return () => listeners.get(type)?.delete(handler);
    },
    onOpen(handler) {
      socket.addEventListener("open", handler);
    },
    onClose(handler) {
      socket.addEventListener("close", handler);
    },
    onError(handler) {
      socket.addEventListener("error", handler);
    },
    close() {
      listeners.clear();
      socket.close();
    },
  };
}
