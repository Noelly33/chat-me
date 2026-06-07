import { useState } from "react";
import Login from "./components/Login.jsx";
import Chat from "./components/Chat.jsx";

const STORAGE_KEY = "chatme-auth";

function loadAuth() {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? JSON.parse(raw) : null;
  } catch {
    return null;
  }
}

export default function App() {
  const [auth, setAuth] = useState(loadAuth);

  function handleAuth(data) {
    setAuth(data);
    localStorage.setItem(STORAGE_KEY, JSON.stringify(data));
  }

  function handleLogout() {
    setAuth(null);
    localStorage.removeItem(STORAGE_KEY);
  }

  if (!auth) {
    return <Login onAuth={handleAuth} />;
  }

  return <Chat auth={auth} onLogout={handleLogout} />;
}
