import { useEffect } from "react";
import { useAppContext } from "../context/AppContext.jsx";
import { useConversations } from "../hooks/useConversations.js";
import { useMessages } from "../hooks/useMessages.js";
import ContactSearch from "../components/ContactSearch.jsx";
import ConversationList from "../components/ConversationList.jsx";
import ChatHeader from "../components/ChatHeader.jsx";
import MessageList from "../components/MessageList.jsx";
import MessageInput from "../components/MessageInput.jsx";
import ConnectionStatus from "../components/ConnectionStatus.jsx";

export default function Chat({ user, onLogout }) {
  const { online, typingUsers, wsStatus } = useAppContext();
  const { conversations, activeConversation, selectConversation, openContact } =
    useConversations();
  const { messages, sendMessage, notifyTyping } = useMessages(activeConversation);

  useEffect(() => {
    document.title = activeConversation
      ? `${activeConversation.otroUsuario.nombreUsuario} · ChatMe`
      : "ChatMe";
  }, [activeConversation]);

  return (
    <div className="chat-layout">
      <aside className="sidebar">
        <div className="sidebar-brand">
          <img src="/logo.svg" alt="ChatMe" className="brand-logo" />
          <div className="sidebar-header">
            Chat<span>Me</span>
          </div>
        </div>

        <ContactSearch onSelect={openContact} />

        <div className="online-title">Conversaciones</div>
        <ConversationList
          conversations={conversations}
          activeKey={activeConversation?.key}
          online={online}
          onSelect={selectConversation}
        />

        <div className="sidebar-footer">
          <div className="user-card">
            <div className="user-avatar">
              {(user.nombres || user.nombreUsuario || "?")[0].toUpperCase()}
            </div>
            <div className="user-card-info">
              <span className="user-card-name">
                {user.nombres ? `${user.nombres} ${user.apellidos}` : user.nombreUsuario}
              </span>
              <span className="user-card-status">@{user.nombreUsuario}</span>
            </div>
          </div>
          <button className="btn-logout" onClick={onLogout}>
            Cerrar sesión
          </button>
        </div>
      </aside>

      <main className="chat-main">
        <ChatHeader
          conversation={activeConversation}
          online={online}
          right={<ConnectionStatus status={wsStatus} />}
        />

        {activeConversation ? (
          <>
            <MessageList messages={messages} typingUsers={typingUsers} />
            <MessageInput
              disabled={wsStatus !== "open"}
              onSend={sendMessage}
              onTyping={notifyTyping}
            />
          </>
        ) : (
          <div className="messages" />
        )}
      </main>
    </div>
  );
}
