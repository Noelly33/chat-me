import { useEffect, useRef } from "react";

function formatTime(iso) {
  try {
    return new Date(iso).toLocaleTimeString("es-EC", {
      hour: "2-digit",
      minute: "2-digit",
    });
  } catch {
    return "";
  }
}

export default function MessageList({ messages, currentUser }) {
  const endRef = useRef(null);

  useEffect(() => {
    endRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages]);

  return (
    <div className="messages">
      {messages.map((msg) => {
        if (msg.type === "system") {
          return (
            <div key={msg.id} className="system-message">
              {msg.text}
            </div>
          );
        }

        const mine = msg.username === currentUser;
        return (
          <div key={msg.id} className={`message ${mine ? "mine" : ""}`}>
            <div className="message-meta">
              <span className="message-author">
                {mine ? "Tú" : msg.username}
              </span>
              <span className="message-time">{formatTime(msg.timestamp)}</span>
            </div>
            <div className="message-text">{msg.text}</div>
          </div>
        );
      })}
      <div ref={endRef} />
    </div>
  );
}
