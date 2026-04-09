const TOKEN_KEY = "gdg_auth_token";

function canUseStorage() {
  return typeof window !== "undefined";
}

export function getAuthToken() {
  if (!canUseStorage()) {
    return null;
  }

  return window.localStorage.getItem(TOKEN_KEY);
}

export function setAuthToken(token: string) {
  if (!canUseStorage()) {
    return;
  }

  window.localStorage.setItem(TOKEN_KEY, token);
}

export function clearAuthToken() {
  if (!canUseStorage()) {
    return;
  }

  window.localStorage.removeItem(TOKEN_KEY);
}