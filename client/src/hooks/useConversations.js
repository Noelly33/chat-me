import { useEffect } from "react";
import { useAppContext } from "../context/AppContext.jsx";
import * as api from "../services/api.js";

export function useConversations() {
  const { conversationList, activeKey, selectConversation, setContacts, setConversations, sendMessage } =
    useAppContext();

  useEffect(() => {
    api
      .getConversations()
      .then((data) => {
        setConversations(data ?? []);
      })
      .catch(() => {});

    api
      .getContacts({ page: 1, size: 10 })
      .then((data) => {
        setContacts(data.data);
      })
      .catch(() => {});
  }, [setContacts, setConversations]);

  const activeConversation = conversationList.find((c) => c.key === activeKey) ?? null;

  function openContact(contact) {
    const key = contact.nombreUsuario;
    const existing = conversationList.find((c) => c.key === key);
    if (existing) {
      selectConversation(key);
    } else {
      sendMessage({ otroUsuario: contact }, "Hola!");
    }
  }

  return {
    conversations: conversationList,
    activeConversation,
    selectConversation,
    openContact,
  };
}
