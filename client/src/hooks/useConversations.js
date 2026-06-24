import { useEffect } from "react";
import { useAppContext } from "../context/AppContext.jsx";
import * as api from "../services/api.js";

export function useConversations() {
  const { conversationList, activeKey, selectConversation, setContacts, sendMessage } =
    useAppContext();

  useEffect(() => {
    api
      .getContacts({ page: 1, size: 10 })
      .then((data) => {
        setContacts(data.data);
      })
      .catch(() => {});
  }, [setContacts]);

  const activeConversation = conversationList.find((c) => c.key === activeKey) ?? null;

  function openContact(contact) {
    const key = contact.nombreUsuario;
    const hasConversation = conversationList.some((c) => c.key === key);
    if (hasConversation) {
      selectConversation(key);
    } else {
      sendMessage(contact, "Hola!");
    }
  }

  return {
    conversations: conversationList,
    activeConversation,
    selectConversation,
    openContact,
  };
}
