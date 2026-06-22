export default function ConversationList({ conversations, activeKey, online, onSelect }) {
  if (conversations.length === 0) {
    return <p className="last-seen" style={{ padding: "0 0.6rem" }}>Sin conversaciones todavía.</p>;
  }

  return (
    <ul className="user-list">
      {conversations.map((conv) => {
        const { otroUsuario } = conv;
        const isOnline = online.includes(otroUsuario.nombreUsuario);
        const displayName = otroUsuario.nombres
          ? `${otroUsuario.nombres} ${otroUsuario.apellidos ?? ""}`.trim()
          : otroUsuario.nombreUsuario;

        return (
          <li
            key={conv.key}
            className={`user-item ${conv.key === activeKey ? "me" : ""}`}
            onClick={() => onSelect(conv.key)}
            style={{ cursor: "pointer" }}
          >
            <span className={`dot ${isOnline ? "" : "off"}`} />
            <span className="user-name">{displayName}</span>
            {conv.noLeidos > 0 && <span className="last-seen">{conv.noLeidos}</span>}
          </li>
        );
      })}
    </ul>
  );
}
