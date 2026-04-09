import { create } from "zustand";
import type { SessionUser } from "@/lib/auth/jwt";

type AuthStoreState = {
  token: string | null;
  user: SessionUser | null;
  isAuthenticated: boolean;
  setSessionFromToken: (token: string, user: SessionUser) => void;
  clearSession: () => void;
};

export const useAuthStore = create<AuthStoreState>((set) => ({
  token: null,
  user: null,
  isAuthenticated: false,
  setSessionFromToken: (token, user) => {
    set({ token, user, isAuthenticated: true });
  },
  clearSession: () => {
    set({ token: null, user: null, isAuthenticated: false });
  },
}));