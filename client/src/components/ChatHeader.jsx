export default function ChatHeader({ conversation, online, right }) {
  if (!conversation) {
    return (
      <div className="chat-topbar">
        <h2>Seleccioná una conversación</h2>
        {right}
      </div>
    );
  }

  const { otroUsuario } = conversation;
  const isOnline = online.includes(otroUsuario.nombreUsuario);
  const initial = (otroUsuario.nombres || otroUsuario.nombreUsuario || "?")[0].toUpperCase();
  const displayName = otroUsuario.nombres
    ? `${otroUsuario.nombres} ${otroUsuario.apellidos ?? ""}`.trim()
    : otroUsuario.nombreUsuario;

  return (
    <div className="chat-topbar">
      <div className="user-card" style={{ margin: 0 }}>
        <div className="user-avatar">{initial}</div>
        <div className="user-card-info">
          <span className="user-card-name">{displayName}</span>
          <span className="user-card-status">
            <span className={`dot ${isOnline ? "" : "off"}`} />
            {isOnline ? "en línea" : "desconectado"}
          </span>
        </div>
      </div>
      {right}
    </div>
  );
}
