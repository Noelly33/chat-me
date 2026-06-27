import { useState } from "react";

export default function MessageInput({ disabled, onSend, onTyping }) {
  const [text, setText] = useState("");

  function handleSubmit(e) {
    e.preventDefault();
    if (!text.trim()) return;
    onSend(text);
    setText("");
  }

  function handleKeyDown(e) {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      handleSubmit(e);
    }
  }

  return (
    <form className="composer" onSubmit={handleSubmit}>
      <input
        type="text"
        placeholder={disabled ? "Esperando conexión…" : "Escribe un mensaje"}
        value={text}
        disabled={disabled}
        onChange={(e) => {
          setText(e.target.value);
          onTyping();
        }}
        onKeyDown={handleKeyDown}
        autoComplete="off"
      />
      <button type="submit" disabled={disabled || !text.trim()}>
        Enviar
      </button>
    </form>
  );
}
