import { useState } from "react";
import { loginRequest, registerRequest } from "../services/api.js";

export default function Login({ onAuth }) {
  const [isRegister, setIsRegister] = useState(false);
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e) {
    e.preventDefault();
    setError("");
    setLoading(true);
    try {
      const data = isRegister
        ? await registerRequest(username.trim(), password)
        : await loginRequest(username.trim(), password);
      onAuth(data); 
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }

  function toggleMode() {
    setIsRegister((v) => !v);
    setError("");
  }

  return (
    <div className="auth-container">
      <div className="auth-card">
        <div className="auth-logo">
          <img src="/logo.svg" alt="ChatMe" className="auth-logo-img" />
          <div className="auth-logo-text">Chat<span>Me</span></div>
        </div>
        <p className="auth-subtitle">
          {isRegister ? "Crea tu cuenta para chatear" : "Inicia sesión para chatear"}
        </p>

        <form className="auth-form" onSubmit={handleSubmit}>
          {error && <div className="error-banner">{error}</div>}

          <div className="field">
            <label htmlFor="username">Usuario</label>
            <input
              id="username"
              type="text"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              placeholder="usuario"
              autoComplete="username"
              autoFocus
              required
            />
          </div>

          <div className="field">
            <label htmlFor="password">Contraseña</label>
            <input
              id="password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="••••••••"
              autoComplete={isRegister ? "new-password" : "current-password"}
              required
            />
          </div>

          <button className="btn-primary" type="submit" disabled={loading}>
            {loading
              ? "Procesando..."
              : isRegister
                ? "Crear cuenta"
                : "Entrar"}
          </button>
        </form>

        <div className="auth-toggle">
          {isRegister ? "¿Ya tienes cuenta?" : "¿No tienes cuenta?"}{" "}
          <button type="button" onClick={toggleMode}>
            {isRegister ? "Inicia sesión" : "Regístrate"}
          </button>
        </div>
      </div>
    </div>
  );
}
