import { createContext, useCallback, useContext, useMemo, useReducer } from "react";
import { useWebSocket } from "../hooks/useWebSocket.js";
import { uuid } from "../utils/uuid.js";

const AppContext = createContext(null);

const initialState = {
  online: [],
  contactsByUsername: {},
  conversations: {},
  activeKey: null,
  typingUsers: [],
};

function upsertConversation(state, key, patch) {
  const existing = state.conversations[key] ?? {
    id: null,
    otroUsuario: { nombreUsuario: key },
    mensajes: [],
    ultimoMensaje: null,
    noLeidos: 0,
    historyLoaded: false,
  };
  return {
    ...state,
    conversations: {
      ...state.conversations,
      [key]: { ...existing, ...patch },
    },
  };
}

function reducer(state, action) {
  switch (action.type) {
    case "SET_ONLINE":
      return { ...state, online: action.usernames };

    case "SET_CONVERSATIONS": {
      const conversations = { ...state.conversations };
      for (const c of action.conversations) {
        const key = c.otroUsuario.nombreUsuario;
        const existing = conversations[key];
        conversations[key] = {
          id: c.id,
          otroUsuario: c.otroUsuario,
          mensajes: existing?.mensajes ?? [],
          ultimoMensaje: c.ultimoMensaje ?? existing?.ultimoMensaje ?? null,
          noLeidos: existing?.noLeidos ?? 0,
        };
      }
      return { ...state, conversations };
    }

    case "SET_CONTACTS": {
      const contactsByUsername = { ...state.contactsByUsername };
      for (const contact of action.contacts) {
        contactsByUsername[contact.nombreUsuario] = contact;
      }
      let next = { ...state, contactsByUsername };
      for (const contact of action.contacts) {
        if (!contact.ultimoMensaje) continue;
        const key = contact.nombreUsuario;
        if (next.conversations[key]) continue;
        next = upsertConversation(next, key, {
          otroUsuario: contact,
          ultimoMensaje: contact.ultimoMensaje,
        });
      }
      return next;
    }

    case "OPEN_CONVERSATION": {
      const key = action.contact.nombreUsuario;
      const next = upsertConversation(state, key, { otroUsuario: action.contact, noLeidos: 0 });
      return { ...next, activeKey: key, typingUsers: [] };
    }

    case "SELECT_CONVERSATION": {
      const existing = state.conversations[action.key];
      const next = existing ? upsertConversation(state, action.key, { noLeidos: 0 }) : state;
      return { ...next, activeKey: action.key, typingUsers: [] };
    }

    case "SEND_MESSAGE": {
      const existing = state.conversations[action.key];
      return upsertConversation(state, action.key, {
        mensajes: [...(existing?.mensajes ?? []), action.message],
        ultimoMensaje: { contenido: action.message.text, fechaCreacion: action.message.timestamp },
      });
    }

    case "RECEIVE_MESSAGE": {
      const { id, username, senderId, text, timestamp } = action.message;
      // Eco del propio mensaje (el broadcast incluye también al emisor): ya
      // se renderizó vía SEND_MESSAGE, así que se descarta. El payload de
      // chat:message no incluye conversacionId (ver follow-ups en README).
      if (username === action.me) return state;

      const key = username;
      const existing = state.conversations[key];
      if (existing?.mensajes.some((m) => m.id === id)) return state;

      const otroUsuario = existing?.otroUsuario ??
        state.contactsByUsername[username] ?? { nombreUsuario: username, id: senderId };

      const isActive = state.activeKey === key;

      return upsertConversation(state, key, {
        otroUsuario,
        mensajes: [...(existing?.mensajes ?? []), { id, username, text, timestamp, mine: false }],
        ultimoMensaje: { contenido: text, fechaCreacion: timestamp },
        noLeidos: isActive ? 0 : (existing?.noLeidos ?? 0) + 1,
      });
    }

    case "SET_MESSAGES": {
      const existing = state.conversations[action.key];
      if (!existing) return state;
      const seen = new Set(action.messages.map((m) => m.id));
      const pending = existing.mensajes.filter((m) => !seen.has(m.id));
      return upsertConversation(state, action.key, {
        mensajes: [...action.messages, ...pending],
        historyLoaded: true,
      });
    }

    case "RECEIVE_SYSTEM_MESSAGE": {
      if (!state.activeKey) return state;
      const existing = state.conversations[state.activeKey];
      if (!existing) return state;
      return upsertConversation(state, state.activeKey, {
        mensajes: [...existing.mensajes, { ...action.message, system: true }],
      });
    }

    case "SET_TYPING": {
      const { username, isTyping } = action;
      const active = state.activeKey && state.conversations[state.activeKey];
      if (!active || active.otroUsuario.nombreUsuario !== username) return state;
      const typingUsers = isTyping
        ? state.typingUsers.includes(username)
          ? state.typingUsers
          : [...state.typingUsers, username]
        : state.typingUsers.filter((u) => u !== username);
      return { ...state, typingUsers };
    }

    default:
      return state;
  }
}

export function AppProvider({ children, auth }) {
  const [state, dispatch] = useReducer(reducer, initialState);
  const me = auth.user?.nombreUsuario;

  const ws = useWebSocket({
    enabled: !!auth.user,
    onMessage: (payload) => dispatch({ type: "RECEIVE_MESSAGE", message: payload, me }),
    onTyping: (payload) => dispatch({ type: "SET_TYPING", ...payload }),
    onSystemMessage: (payload) => dispatch({ type: "RECEIVE_SYSTEM_MESSAGE", message: payload }),
    onOnlineUsers: (usernames) => dispatch({ type: "SET_ONLINE", usernames }),
  });

  const setContacts = useCallback((contacts) => dispatch({ type: "SET_CONTACTS", contacts }), []);

  const setConversations = useCallback(
    (conversations) => dispatch({ type: "SET_CONVERSATIONS", conversations }),
    [],
  );

  const selectConversation = useCallback((key) => {
    dispatch({ type: "SELECT_CONVERSATION", key });
  }, []);

  const setMessages = useCallback(
    (key, messages) => dispatch({ type: "SET_MESSAGES", key, messages }),
    [],
  );

  const sendMessage = useCallback(
    (conversation, text) => {
      if (!conversation?.otroUsuario?.id || !text.trim() || !me) return;
      const otroUsuario = conversation.otroUsuario;
      const key = otroUsuario.nombreUsuario;
      const id = uuid();
      const timestamp = new Date().toISOString();
      dispatch({ type: "OPEN_CONVERSATION", contact: otroUsuario });
      dispatch({ type: "SEND_MESSAGE", key, message: { id, username: me, text, timestamp, mine: true } });
      if (conversation.id) {
        ws.sendMessage(conversation.id, otroUsuario.nombreUsuario, text);
      } else {
        ws.sendIniciarIndividual(otroUsuario.id, otroUsuario.nombreUsuario, text);
      }
    },
    [me, ws],
  );

  const conversationList = useMemo(() => {
    return Object.entries(state.conversations)
      .map(([key, conv]) => ({ key, ...conv }))
      .sort((a, b) => {
        const at = a.ultimoMensaje?.fechaCreacion ?? "";
        const bt = b.ultimoMensaje?.fechaCreacion ?? "";
        return bt.localeCompare(at);
      });
  }, [state.conversations]);

  const value = {
    ...state,
    me,
    conversationList,
    wsStatus: ws.status,
    sendTyping: ws.sendTyping,
    setContacts,
    setConversations,
    selectConversation,
    setMessages,
    sendMessage,
  };

  return <AppContext.Provider value={value}>{children}</AppContext.Provider>;
}

export function useAppContext() {
  const ctx = useContext(AppContext);
  if (!ctx) throw new Error("useAppContext debe usarse dentro de AppProvider");
  return ctx;
}
