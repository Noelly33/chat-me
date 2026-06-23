const AUTH_PREFIX = "/api/v1/auth";
const MESSAGES_PATH = "/api/v1/messages";
const CONTACTS_PATH = "/api/v1/contacts";

export class ApiError extends Error {
  constructor(message, errors = [], status = 0) {
    super(message);
    this.name = "ApiError";
    this.errors = errors;
    this.status = status;
  }
}

function envelope(data) {
  return {
    header: { timestamp: new Date().toISOString(), device: "web" },
    data,
  };
}

let refreshInFlight = null;

async function refreshSession() {
  if (!refreshInFlight) {
    refreshInFlight = fetch(`${AUTH_PREFIX}/refresh`, {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(envelope({})),
    }).finally(() => {
      refreshInFlight = null;
    });
  }
  return refreshInFlight;
}

export async function authFetch(path, init = {}) {
  const doFetch = () => fetch(path, { credentials: "include", ...init });

  let res = await doFetch();

  if (res.status === 401 && !path.startsWith(`${AUTH_PREFIX}/refresh`)) {
    const refreshRes = await refreshSession();
    if (refreshRes.ok) {
      res = await doFetch();
    }
  }

  const body = await res.json().catch(() => null);

  if (!res.ok || body?.success === false) {
    throw new ApiError(
      body?.message || "Error de conexión con el servidor.",
      body?.errors || [],
      res.status,
    );
  }

  return body.data;
}

function postJson(path, data) {
  return authFetch(path, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(envelope(data)),
  });
}

export function register(data) {
  return postJson(`${AUTH_PREFIX}/register`, data);
}

export function login(identifier, password) {
  return postJson(`${AUTH_PREFIX}/login`, { identifier, password });
}

export function refresh() {
  return postJson(`${AUTH_PREFIX}/refresh`, {});
}

export function logout() {
  return postJson(`${AUTH_PREFIX}/logout`, {});
}

export function getProfile() {
  return authFetch(`${AUTH_PREFIX}/profile`, { method: "GET" });
}

export function updateProfile(data) {
  return authFetch(`${AUTH_PREFIX}/profile`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(envelope(data)),
  });
}

export function getContacts({ page = 1, size = 10 } = {}) {
  return authFetch(`${CONTACTS_PATH}?page=${page}&size=${size}`, {
    method: "GET",
  });
}

export function getContactById(id) {
  return authFetch(`${CONTACTS_PATH}/${id}`, { method: "GET" });
}

export function getMessages({ conversationId, page = 1, size = 50 }) {
  return authFetch(
    `${MESSAGES_PATH}?conversationId=${conversationId}&page=${page}&size=${size}`,
    { method: "GET" },
  );
}

// No existe GET /api/v1/auth/profile en el backend (solo PUT). Los endpoints
// register/login/refresh devuelven el JWT en el body: lo decodificamos
// client-side (sin persistirlo) para reconstruir el usuario autenticado.
function decodeJwt(token) {
  const payload = token.split(".")[1];
  const json = atob(payload.replace(/-/g, "+").replace(/_/g, "/"));
  return JSON.parse(decodeURIComponent(escape(json)));
}

const CLAIM = {
  id: "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
  username: "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name",
  email: "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress",
};

export function parseUserFromToken(token) {
  const claims = decodeJwt(token);
  return {
    id: claims[CLAIM.id] ?? claims.sub,
    nombreUsuario: claims[CLAIM.username] ?? claims.unique_name,
    email: claims[CLAIM.email] ?? claims.email,
    nombres: claims.Nombres ?? "",
    apellidos: claims.Apellidos ?? "",
  };
}
