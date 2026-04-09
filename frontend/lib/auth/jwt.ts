import type { AuthUser } from "@/types/auth";

export type SessionUser = AuthUser & {
  role: string;
};

type JwtPayload = {
  exp?: number;
  [key: string]: unknown;
};

const ROLE_CLAIM = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
const ID_CLAIM = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
const NAME_CLAIM = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
const EMAIL_CLAIM = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress";

const ID_CLAIM_CANDIDATES = [ID_CLAIM, "sub", "nameid"];
const NAME_CLAIM_CANDIDATES = [NAME_CLAIM, "unique_name", "name"];
const EMAIL_CLAIM_CANDIDATES = [EMAIL_CLAIM, "email"];
const ROLE_CLAIM_CANDIDATES = [ROLE_CLAIM, "role"];

function decodeBase64Url(value: string) {
  const normalized = value.replace(/-/g, "+").replace(/_/g, "/");
  const padded = normalized + "=".repeat((4 - (normalized.length % 4)) % 4);

  if (typeof window.atob !== "function") {
    throw new Error("Base64 decode is unavailable");
  }

  return window.atob(padded);
}

export function parseJwtPayload(token: string): JwtPayload | null {
  const parts = token.split(".");
  if (parts.length < 2) {
    return null;
  }

  try {
    const decoded = decodeBase64Url(parts[1]);
    return JSON.parse(decoded) as JwtPayload;
  } catch {
    return null;
  }
}

export function isTokenExpired(token: string) {
  const payload = parseJwtPayload(token);
  if (!payload?.exp) {
    return false;
  }

  return payload.exp * 1000 <= Date.now();
}

export function getSessionUserFromToken(token: string): SessionUser | null {
  const payload = parseJwtPayload(token);
  if (!payload) {
    return null;
  }

  const id = getFirstStringClaim(payload, ID_CLAIM_CANDIDATES);
  const name = getFirstStringClaim(payload, NAME_CLAIM_CANDIDATES);
  const email = getFirstStringClaim(payload, EMAIL_CLAIM_CANDIDATES);
  const role = getFirstStringClaim(payload, ROLE_CLAIM_CANDIDATES, "User") || "User";

  if (!id || !name || !email) {
    return null;
  }

  return { id, name, email, role };
}

function getFirstStringClaim(payload: JwtPayload, claimKeys: string[], fallback = "") {
  for (const claimKey of claimKeys) {
    const value = payload[claimKey];
    if (typeof value === "string" && value.trim()) {
      return value.trim();
    }
  }

  return fallback;
}
