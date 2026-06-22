import { useEffect, useRef } from "react";
import MessageBubble from "./MessageBubble.jsx";

export default function MessageList({ messages, typingUsers }) {
  const containerRef = useRef(null);
  const wasAtBottomRef = useRef(true);

  useEffect(() => {
    const el = containerRef.current;
    if (!el) return;
    const lastMessage = messages[messages.length - 1];
    if (wasAtBottomRef.current || lastMessage?.mine) {
      el.scrollTop = el.scrollHeight;
    }
  }, [messages]);

  function handleScroll() {
    const el = containerRef.current;
    if (!el) return;
    wasAtBottomRef.current = el.scrollHeight - el.scrollTop - el.clientHeight < 80;
  }

  return (
    <>
      <div className="messages" ref={containerRef} onScroll={handleScroll}>
        {messages.map((message) => (
          <MessageBubble key={message.id} message={message} />
        ))}
      </div>
      <div className="typing-indicator">
        {typingUsers.length > 0 ? `${typingUsers.join(", ")} escribiendo…` : ""}
      </div>
    </>
  );
}
