const SERVER_URL = import.meta.env.VITE_SERVER_URL || "http://localhost:4000";

async function request(path, body) {
  const res = await fetch(`${SERVER_URL}${path}`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });

  const data = await res.json().catch(() => ({}));
  if (!res.ok) {
    throw new Error(data.error || "Error de conexión con el servidor.");
  }
  return data; 
}

export function registerRequest(username, password) {
  return request("/api/register", { username, password });
}

export function loginRequest(username, password) {
  return request("/api/login", { username, password });
}
