import { useCallback, useEffect, useRef, useState } from "react";
import { createSocket } from "../services/socket.js";

export function useChat({ token, onAuthError }) {
  const [messages, setMessages] = useState([]); 
  const [onlineUsers, setOnlineUsers] = useState([]);
  const [typingUsers, setTypingUsers] = useState([]);
  const [connected, setConnected] = useState(false);

  const socketRef = useRef(null);
  const typingTimeoutRef = useRef(null);

  useEffect(() => {
    const socket = createSocket(token);
    socketRef.current = socket;


    socket.on("connect", () => setConnected(true));
    socket.on("disconnect", () => setConnected(false));
    socket.on("connect_error", (err) => {
      setConnected(false);
      if (/token|autenticación/i.test(err.message)) {
        onAuthError?.();
      }
    });

    socket.on("chat:message", (msg) => {
      setMessages((prev) => [...prev, { ...msg, type: "chat" }]);
    });

    socket.on("system:message", (msg) => {
      setMessages((prev) => [...prev, { ...msg, type: "system" }]);
    });

    socket.on("users:online", (users) => {
      setOnlineUsers(users);
    });

    socket.on("chat:typing", ({ username, isTyping }) => {
      setTypingUsers((prev) => {
        if (isTyping) {
          return prev.includes(username) ? prev : [...prev, username];
        }
        return prev.filter((u) => u !== username);
      });
    });

    socket.connect();


    return () => {
      socket.removeAllListeners();
      socket.disconnect();
      socketRef.current = null;
    };
  }, [token, onAuthError]);


  const sendMessage = useCallback((text) => {
    const socket = socketRef.current;
    if (!socket) return;
    socket.emit("chat:message", { text });
    socket.emit("chat:typing", false);
  }, []);

  const notifyTyping = useCallback(() => {
    const socket = socketRef.current;
    if (!socket) return;
    socket.emit("chat:typing", true);

    clearTimeout(typingTimeoutRef.current);
    typingTimeoutRef.current = setTimeout(() => {
      socket.emit("chat:typing", false);
    }, 2000);
  }, []);

  return { messages, onlineUsers, typingUsers, connected, sendMessage, notifyTyping };
}
