import { create } from 'zustand';

interface SettingsState {
  theme: 'dark' | 'light';
  autoRefresh: boolean;
  setTheme: (theme: 'dark' | 'light') => void;
  setAutoRefresh: (autoRefresh: boolean) => void;
}

export const useSettingsStore = create<SettingsState>((set) => ({
  theme: 'dark',
  autoRefresh: false,
  setTheme: (theme) => set({ theme }),
  setAutoRefresh: (autoRefresh) => set({ autoRefresh }),
}));
