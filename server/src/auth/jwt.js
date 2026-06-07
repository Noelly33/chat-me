import jwt from "jsonwebtoken";

const JWT_SECRET = process.env.JWT_SECRET || "secreto-inseguro-por-defecto";
const JWT_EXPIRES_IN = process.env.JWT_EXPIRES_IN || "2h";


export function signToken(username) {
  return jwt.sign({ username }, JWT_SECRET, { expiresIn: JWT_EXPIRES_IN });
}

export function verifyToken(token) {
  return jwt.verify(token, JWT_SECRET);
}
