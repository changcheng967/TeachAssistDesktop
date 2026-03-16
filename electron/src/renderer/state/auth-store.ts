import { create } from 'zustand';

interface AuthState {
  isLoggedIn: boolean;
  username: string | null;
  error: string | null;
  isDemo: boolean;
  setLoggedIn: (username: string, isDemo: boolean) => void;
  setLoggedOut: () => void;
  setError: (error: string | null) => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  isLoggedIn: false,
  username: null,
  error: null,
  isDemo: false,
  setLoggedIn: (username, isDemo) => set({ isLoggedIn: true, username, error: null, isDemo }),
  setLoggedOut: () => set({ isLoggedIn: false, username: null, error: null, isDemo: false }),
  setError: (error) => set({ error }),
}));
