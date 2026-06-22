import { useAuth } from "./hooks/useAuth.js";
import { AppProvider } from "./context/AppContext.jsx";
import Login from "./routes/Login.jsx";
import Chat from "./routes/Chat.jsx";

export default function App() {
  const auth = useAuth();

  if (auth.booting) {
    return <div className="auth-container">Cargando…</div>;
  }

  if (!auth.user) {
    return <Login onLogin={auth.login} onRegister={auth.register} />;
  }

  return (
    <AppProvider auth={auth}>
      <Chat user={auth.user} onLogout={auth.logout} />
    </AppProvider>
  );
}
