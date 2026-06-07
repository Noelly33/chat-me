import { useState } from "react";

export default function MessageInput({ onSend, onTyping, disabled }) {
  const [text, setText] = useState("");

  function handleSubmit(e) {
    e.preventDefault();
    const value = text.trim();
    if (!value) return;
    onSend(value);
    setText("");
  }

  function handleChange(e) {
    setText(e.target.value);
    if (e.target.value) onTyping();
  }

  return (
    <form className="composer" onSubmit={handleSubmit}>
      <input
        type="text"
        value={text}
        onChange={handleChange}
        placeholder={disabled ? "Conectando..." : "Escribe un mensaje..."}
        disabled={disabled}
        autoComplete="off"
      />
      <button type="submit" disabled={disabled || !text.trim()}>
        Enviar
      </button>
    </form>
  );
}
