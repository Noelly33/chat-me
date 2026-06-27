import { useEffect, useState } from "react";
import * as api from "../services/api.js";

export default function ContactSearch({ onSelect }) {
  const [query, setQuery] = useState("");
  const [debounced, setDebounced] = useState("");
  const [results, setResults] = useState([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    const id = setTimeout(() => setDebounced(query.trim()), 300);
    return () => clearTimeout(id);
  }, [query]);

  useEffect(() => {
    if (!debounced) {
      setResults([]);
      return undefined;
    }
    let cancelled = false;
    setLoading(true);
    api
      .searchUsers(debounced)
      .then((data) => {
        if (!cancelled) setResults(data ?? []);
      })
      .catch(() => {
        if (!cancelled) setResults([]);
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [debounced]);

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
          marginTop: "0.8rem",
        }}
      />
      {debounced && (
        <ul className="user-list" style={{ marginTop: "0.5rem" }}>
          {loading && (
            <li className="user-name" style={{ padding: "0.5rem 0.6rem" }}>
              Buscando…
            </li>
          )}
          {!loading && results.length === 0 && (
            <li className="user-name" style={{ padding: "0.5rem 0.6rem" }}>
              Sin resultados
            </li>
          )}
          {results.map((user) => (
            <li
              key={user.id}
              className="user-item"
              style={{ cursor: "pointer" }}
              onClick={() => {
                onSelect(user);
                setQuery("");
                setResults([]);
              }}
            >
              <span className="user-name">
                {user.nombres} {user.apellidos} (@{user.nombreUsuario})
              </span>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
