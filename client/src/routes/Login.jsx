import { useState } from "react";

const emptyRegister = {
  nombres: "",
  apellidos: "",
  nombreUsuario: "",
  email: "",
  password: "",
  numeroTelefono: "",
  fechaNacimiento: "",
};

export default function Login({ onLogin, onRegister }) {
  const [tab, setTab] = useState("login");
  const [errors, setErrors] = useState([]);
  const [submitting, setSubmitting] = useState(false);

  const [loginForm, setLoginForm] = useState({ identifier: "", password: "" });
  const [registerForm, setRegisterForm] = useState(emptyRegister);

  function switchTab(next) {
    setTab(next);
    setErrors([]);
  }

  async function handleLoginSubmit(e) {
    e.preventDefault();
    setErrors([]);
    setSubmitting(true);
    try {
      await onLogin(loginForm.identifier, loginForm.password);
    } catch (err) {
      setErrors(err.errors?.length ? err.errors : [err.message]);
    } finally {
      setSubmitting(false);
    }
  }

  async function handleRegisterSubmit(e) {
    e.preventDefault();
    setErrors([]);
    setSubmitting(true);
    try {
      await onRegister({
        ...registerForm,
        fechaNacimiento: registerForm.fechaNacimiento
          ? new Date(registerForm.fechaNacimiento).toISOString()
          : undefined,
      });
    } catch (err) {
      setErrors(err.errors?.length ? err.errors : [err.message]);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="auth-container">
      <div className="auth-card">
        <div className="auth-logo">
          <img src="/logo.svg" alt="ChatMe" className="auth-logo-img" />
          <div className="auth-logo-text">
            Chat<span>Me</span>
          </div>
        </div>
        <p className="auth-subtitle">
          {tab === "login" ? "Inicia sesión para continuar" : "Crea tu cuenta para empezar a chatear"}
        </p>

        {errors.length > 0 && (
          <div className="error-banner" style={{ marginBottom: "1rem" }}>
            {errors.join(" · ")}
          </div>
        )}

        {tab === "login" ? (
          <form className="auth-form" onSubmit={handleLoginSubmit}>
            <div className="field">
              <label htmlFor="identifier">Email o usuario</label>
              <input
                id="identifier"
                type="text"
                required
                value={loginForm.identifier}
                onChange={(e) => setLoginForm({ ...loginForm, identifier: e.target.value })}
              />
            </div>
            <div className="field">
              <label htmlFor="password">Contraseña</label>
              <input
                id="password"
                type="password"
                required
                value={loginForm.password}
                onChange={(e) => setLoginForm({ ...loginForm, password: e.target.value })}
              />
            </div>
            <button type="submit" className="btn-primary" disabled={submitting}>
              {submitting ? "Ingresando…" : "Iniciar sesión"}
            </button>
          </form>
        ) : (
          <form className="auth-form" onSubmit={handleRegisterSubmit}>
            <div className="field">
              <label htmlFor="nombres">Nombres</label>
              <input
                id="nombres"
                type="text"
                required
                value={registerForm.nombres}
                onChange={(e) => setRegisterForm({ ...registerForm, nombres: e.target.value })}
              />
            </div>
            <div className="field">
              <label htmlFor="apellidos">Apellidos</label>
              <input
                id="apellidos"
                type="text"
                required
                value={registerForm.apellidos}
                onChange={(e) => setRegisterForm({ ...registerForm, apellidos: e.target.value })}
              />
            </div>
            <div className="field">
              <label htmlFor="nombreUsuario">Nombre de usuario</label>
              <input
                id="nombreUsuario"
                type="text"
                required
                minLength={3}
                value={registerForm.nombreUsuario}
                onChange={(e) =>
                  setRegisterForm({ ...registerForm, nombreUsuario: e.target.value })
                }
              />
            </div>
            <div className="field">
              <label htmlFor="email">Email</label>
              <input
                id="email"
                type="email"
                required
                value={registerForm.email}
                onChange={(e) => setRegisterForm({ ...registerForm, email: e.target.value })}
              />
            </div>
            <div className="field">
              <label htmlFor="reg-password">Contraseña</label>
              <input
                id="reg-password"
                type="password"
                required
                minLength={6}
                value={registerForm.password}
                onChange={(e) => setRegisterForm({ ...registerForm, password: e.target.value })}
              />
            </div>
            <div className="field">
              <label htmlFor="numeroTelefono">Teléfono (+código país)</label>
              <input
                id="numeroTelefono"
                type="text"
                required
                placeholder="+5491112345678"
                pattern="^\+?[1-9]\d{1,14}$"
                value={registerForm.numeroTelefono}
                onChange={(e) =>
                  setRegisterForm({ ...registerForm, numeroTelefono: e.target.value })
                }
              />
            </div>
            <div className="field">
              <label htmlFor="fechaNacimiento">Fecha de nacimiento (opcional)</label>
              <input
                id="fechaNacimiento"
                type="date"
                value={registerForm.fechaNacimiento}
                onChange={(e) =>
                  setRegisterForm({ ...registerForm, fechaNacimiento: e.target.value })
                }
              />
            </div>
            <button type="submit" className="btn-primary" disabled={submitting}>
              {submitting ? "Creando cuenta…" : "Crear cuenta"}
            </button>
          </form>
        )}

        <div className="auth-toggle">
          {tab === "login" ? (
            <>
              ¿No tenés cuenta? <button type="button" onClick={() => switchTab("register")}>Creá una</button>
            </>
          ) : (
            <>
              ¿Ya tenés cuenta? <button type="button" onClick={() => switchTab("login")}>Iniciá sesión</button>
            </>
          )}
        </div>
      </div>
    </div>
  );
}
