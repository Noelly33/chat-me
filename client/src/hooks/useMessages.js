import { useEffect, useRef } from "react";
import { useAppContext } from "../context/AppContext.jsx";
import * as api from "../services/api.js";

const TYPING_DEBOUNCE_MS = 2000;

function mapHistoryMessage(m, me) {
  return {
    id: m.id,
    username: m.emisorNombre,
    text: m.contenido,
    timestamp: m.fechaCreacion,
    mine: m.emisorNombre === me,
  };
}

export function useMessages(conversation) {
  const { me, sendMessage, sendTyping, setMessages } = useAppContext();
  const typingTimeoutRef = useRef(null);

  const messages = conversation?.mensajes ?? [];

  useEffect(() => {
    if (!conversation?.id || conversation.historyLoaded) return;
    const key = conversation.key;
    api
      .getMessages({ conversationId: conversation.id, page: 1, size: 50 })
      .then((page) => {
        const history = (page?.data ?? [])
          .slice()
          .reverse()
          .map((m) => mapHistoryMessage(m, me));
        setMessages(key, history);
      })
      .catch(() => {});
  }, [conversation?.id, conversation?.historyLoaded, conversation?.key, me, setMessages]);

  function send(text) {
    if (!conversation || !text.trim()) return;
    sendMessage(conversation, text.trim());
    clearTimeout(typingTimeoutRef.current);
    sendTyping(false, conversation.otroUsuario.nombreUsuario);
  }

  function notifyTyping() {
    if (!conversation) return;
    const paraUsername = conversation.otroUsuario.nombreUsuario;
    sendTyping(true, paraUsername);
    clearTimeout(typingTimeoutRef.current);
    typingTimeoutRef.current = setTimeout(() => sendTyping(false, paraUsername), TYPING_DEBOUNCE_MS);
  }

  return { messages, sendMessage: send, notifyTyping };
}
