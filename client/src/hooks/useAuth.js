import { useCallback, useEffect, useState } from "react";
import * as api from "../services/api.js";

export function useAuth() {
  const [user, setUser] = useState(null);
  const [booting, setBooting] = useState(true);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const data = await api.refresh();
        if (!cancelled) setUser(api.parseUserFromToken(data.accessToken));
      } catch {
        if (!cancelled) setUser(null);
      } finally {
        if (!cancelled) setBooting(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  const login = useCallback(async (identifier, password) => {
    const data = await api.login(identifier, password);
    setUser(api.parseUserFromToken(data.token));
  }, []);

  const register = useCallback(async (payload) => {
    const data = await api.register(payload);
    setUser(api.parseUserFromToken(data.token));
  }, []);

  const logout = useCallback(async () => {
    try {
      await api.logout();
    } finally {
      setUser(null);
    }
  }, []);

  return { user, booting, login, register, logout };
}
