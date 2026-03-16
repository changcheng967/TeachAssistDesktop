import { useEffect, useState, useCallback } from 'react';
import { useSettingsStore } from '../state/settings-store';

export function useTheme() {
  const { theme, setTheme: setStoreTheme } = useSettingsStore();
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
    // Load saved theme
    window.electronAPI.getTheme().then((t) => {
      setStoreTheme(t as 'dark' | 'light');
    });

    // Listen for theme changes
    const unsubscribe = window.electronAPI.onThemeChange((t) => {
      setStoreTheme(t as 'dark' | 'light');
    });
    return unsubscribe;
  }, [setStoreTheme]);

  useEffect(() => {
    if (!mounted) return;
    const root = document.documentElement;
    if (theme === 'dark') {
      root.classList.add('dark');
    } else {
      root.classList.remove('dark');
    }
  }, [theme, mounted]);

  const setTheme = useCallback(async (newTheme: 'dark' | 'light') => {
    setStoreTheme(newTheme);
    await window.electronAPI.setTheme(newTheme);
  }, [setStoreTheme]);

  return { theme, setTheme, mounted };
}
