import bcrypt from "bcryptjs";
import { createUser, findUser, userExists } from "../store/userStore.js";
import { signToken } from "./jwt.js";

const SALT_ROUNDS = 10;

// Reglas de validación de entrada.
function validateCredentials(username, password) {
  if (typeof username !== "string" || typeof password !== "string") {
    return "Usuario y contraseña son obligatorios.";
  }
  const name = username.trim();
  if (name.length < 3 || name.length > 20) {
    return "El usuario debe tener entre 3 y 20 caracteres.";
  }
  if (!/^[a-zA-Z0-9_]+$/.test(name)) {
    return "El usuario solo puede contener letras, números y guion bajo.";
  }
  if (password.length < 4) {
    return "La contraseña debe tener al menos 4 caracteres.";
  }
  return null;
}

export async function register(req, res) {
  const { username, password } = req.body || {};

  const error = validateCredentials(username, password);
  if (error) return res.status(400).json({ error });

  const name = username.trim();
  if (userExists(name)) {
    return res.status(409).json({ error: "Ese usuario ya está registrado." });
  }

  const passwordHash = await bcrypt.hash(password, SALT_ROUNDS);
  const user = createUser({ username: name, passwordHash });

  const token = signToken(user.username);
  return res.status(201).json({ token, username: user.username });
}


export async function login(req, res) {
  const { username, password } = req.body || {};

  if (!username || !password) {
    return res.status(400).json({ error: "Usuario y contraseña son obligatorios." });
  }

  const user = findUser(username.trim());
  if (!user) {
    return res.status(401).json({ error: "Usuario o contraseña incorrectos." });
  }

  const ok = await bcrypt.compare(password, user.passwordHash);
  if (!ok) {
    return res.status(401).json({ error: "Usuario o contraseña incorrectos." });
  }

  const token = signToken(user.username);
  return res.json({ token, username: user.username });
}
