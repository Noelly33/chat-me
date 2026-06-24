import { useRef } from "react";
import { useAppContext } from "../context/AppContext.jsx";

const TYPING_DEBOUNCE_MS = 2000;

export function useMessages(conversation) {
  const { sendMessage, sendTyping } = useAppContext();
  const typingTimeoutRef = useRef(null);

  const messages = conversation?.mensajes ?? [];

  function send(text) {
    if (!conversation || !text.trim()) return;
    sendMessage(conversation, text.trim());
    clearTimeout(typingTimeoutRef.current);
    sendTyping(false);
  }

  function notifyTyping() {
    sendTyping(true);
    clearTimeout(typingTimeoutRef.current);
    typingTimeoutRef.current = setTimeout(() => sendTyping(false), TYPING_DEBOUNCE_MS);
  }

  return { messages, sendMessage: send, notifyTyping };
}
