function formatLastSeen(ts) {
  return new Date(ts).toLocaleTimeString("es", {
    hour: "2-digit",
    minute: "2-digit",
  });
}

export default function UserList({ users, currentUser }) {
  const onlineCount = users.filter((u) => u.online).length;

  return (
    <>
      <p className="online-title">En línea — {onlineCount}</p>
      <ul className="user-list">
        {users.map((u) => (
          <li
            key={u.username}
            className={`user-item ${u.username === currentUser ? "me" : ""} ${
              u.online ? "" : "offline"
            }`}
          >
            <span className={`dot ${u.online ? "" : "off"}`} />
            <span className="user-name">
              {u.username}
              {u.username === currentUser && " (tú)"}
            </span>
            {!u.online && u.lastSeen && (
              <span className="last-seen">últ. vez {formatLastSeen(u.lastSeen)}</span>
            )}
          </li>
        ))}
      </ul>
    </>
  );
}
