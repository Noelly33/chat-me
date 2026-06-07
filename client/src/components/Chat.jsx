import { useCallback } from "react";
import { useChat } from "../hooks/useChat.js";
import UserList from "./UserList.jsx";
import MessageList from "./MessageList.jsx";
import MessageInput from "./MessageInput.jsx";

export default function Chat({ auth, onLogout }) {
  const { token, username } = auth;

  const handleAuthError = useCallback(() => {
    alert("Tu sesión expiró. Inicia sesión nuevamente.");
    onLogout();
  }, [onLogout]);

  const {
    messages,
    onlineUsers,
    typingUsers,
    connected,
    sendMessage,
    notifyTyping,
  } = useChat({ token, onAuthError: handleAuthError });

  const othersTyping = typingUsers.filter((u) => u !== username);
  const typingText =
    othersTyping.length === 1
      ? `${othersTyping[0]} está escribiendo...`
      : othersTyping.length > 1
        ? `${othersTyping.length} personas están escribiendo...`
        : "";

  return (
    <div className="chat-layout">
      <aside className="sidebar">
        <div className="sidebar-brand">
          <img src="/logo.svg" alt="ChatMe" className="brand-logo" />
          <div className="sidebar-header">
            Chat<span>Me</span>
          </div>
        </div>

        <UserList users={onlineUsers} currentUser={username} />

        <div className="sidebar-footer">
          <div className="user-card">
            <div className="user-avatar">
              {username.charAt(0).toUpperCase()}
            </div>
            <div className="user-card-info">
              <span className="user-card-name">{username}</span>
              <span className="user-card-status">
                <span className="dot" /> En línea
              </span>
            </div>
          </div>
          <button className="btn-logout" onClick={onLogout}>
            Cerrar sesión
          </button>
        </div>
      </aside>

      <main className="chat-main">
        <div className="chat-topbar">
          <h2>Sala general</h2>
          <div className={`connection-status ${connected ? "" : "off"}`}>
            <span className="dot" />
            {connected ? "Conectado" : "Desconectado"}
          </div>
        </div>

        <MessageList messages={messages} currentUser={username} />

        <div className="typing-indicator">{typingText}</div>

        <MessageInput
          onSend={sendMessage}
          onTyping={notifyTyping}
          disabled={!connected}
        />
      </main>
    </div>
  );
}
