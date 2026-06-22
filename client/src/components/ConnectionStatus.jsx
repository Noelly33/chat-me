const LABELS = {
  idle: "Desconectado",
  connecting: "Conectando…",
  open: "Conectado",
  reconnecting: "Reconectando…",
};

export default function ConnectionStatus({ status }) {
  const off = status !== "open";
  return (
    <div className={`connection-status ${off ? "off" : ""}`}>
      <span className="dot" />
      {LABELS[status] ?? status}
    </div>
  );
}
