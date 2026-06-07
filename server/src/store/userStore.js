const users = new Map();
const onlineUsers = new Map();
const lastSeen = new Map(); 


export function findUser(username) {
  return users.get(username.toLowerCase());
}

export function createUser({ username, passwordHash }) {
  const user = {
    username, 
    passwordHash,
    createdAt: new Date().toISOString(),
  };
  users.set(username.toLowerCase(), user);
  return user;
}

export function userExists(username) {
  return users.has(username.toLowerCase());
}


export function addOnlineUser(socketId, username) {
  onlineUsers.set(socketId, { username });
}

export function removeOnlineUser(socketId) {
  const user = onlineUsers.get(socketId);
  onlineUsers.delete(socketId);
  return user;
}

export function getOnlineUser(socketId) {
  return onlineUsers.get(socketId);
}

export function getOnlineUsernames() {
  const names = new Set();
  for (const { username } of onlineUsers.values()) {
    names.add(username);
  }
  return [...names].sort((a, b) => a.localeCompare(b));
}

export function isUserStillOnline(username) {
  for (const u of onlineUsers.values()) {
    if (u.username === username) return true;
  }
  return false;
}

export function setLastSeen(username) {
  lastSeen.set(username, new Date().toISOString());
}

// Lista combinada: conectados + los que se desconectaron en esta sesión.
export function getRoster() {
  const online = new Set(getOnlineUsernames());

  const roster = [...online].map((username) => ({
    username,
    online: true,
    lastSeen: null,
  }));

  for (const [username, ts] of lastSeen) {
    if (!online.has(username)) {
      roster.push({ username, online: false, lastSeen: ts });
    }
  }

  // En línea primero, luego alfabético.
  return roster.sort((a, b) =>
    a.online === b.online
      ? a.username.localeCompare(b.username)
      : a.online
        ? -1
        : 1
  );
}
