import { useEffect, useMemo, useState } from "react";

export default function ContactSearch({ contacts, onSelect }) {
  const [query, setQuery] = useState("");
  const [debounced, setDebounced] = useState("");

  useEffect(() => {
    const id = setTimeout(() => setDebounced(query.trim().toLowerCase()), 300);
    return () => clearTimeout(id);
  }, [query]);

  const filtered = useMemo(() => {
    if (!debounced) return [];
    return contacts.filter((c) =>
      `${c.nombreUsuario} ${c.nombres} ${c.apellidos}`.toLowerCase().includes(debounced),
    );
  }, [contacts, debounced]);

  return (
    <div style={{ position: "relative", marginBottom: "1rem" }}>
      <input
        type="text"
        placeholder="Buscar contactos…"
        value={query}
        onChange={(e) => setQuery(e.target.value)}
        style={{
          width: "100%",
          padding: "0.6rem 0.8rem",
          borderRadius: "8px",
          border: "1px solid var(--bg-softer)",
          outline: "none",
        }}
      />
      {debounced && (
        <ul className="user-list" style={{ marginTop: "0.5rem" }}>
          {filtered.length === 0 && (
            <li className="user-name" style={{ padding: "0.5rem 0.6rem" }}>
              Sin resultados
            </li>
          )}
          {filtered.map((contact) => (
            <li
              key={contact.id}
              className="user-item"
              style={{ cursor: "pointer" }}
              onClick={() => {
                onSelect(contact);
                setQuery("");
              }}
            >
              <span className="user-name">
                {contact.nombres} {contact.apellidos} (@{contact.nombreUsuario})
              </span>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
