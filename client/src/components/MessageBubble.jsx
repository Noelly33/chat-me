export default function MessageBubble({ message }) {
  if (message.system) {
    return <div className="system-message">{message.text}</div>;
  }

  const time = new Date(message.timestamp).toLocaleTimeString([], {
    hour: "2-digit",
    minute: "2-digit",
  });

  return (
    <div className={`message ${message.mine ? "mine" : ""}`}>
      <div className="message-meta">
        <span className="message-author">{message.mine ? "Tú" : message.username}</span>
        <span className="message-time">{time}</span>
      </div>
      <div className="message-text">{message.text}</div>
    </div>
  );
}
