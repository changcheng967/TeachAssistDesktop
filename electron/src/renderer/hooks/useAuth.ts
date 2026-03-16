import { useCallback } from 'react';
import { useAuthStore } from '../state/auth-store';
import { useCourseStore } from '../state/course-store';

export function useAuth() {
  const { isLoggedIn, username, isDemo, error, setLoggedIn, setLoggedOut, setError } = useAuthStore();
  const { setCourses, setLoading } = useCourseStore();

  const login = useCallback(async (username: string, password: string, remember: boolean) => {
    setLoading(true);
    setError(null);
    try {
      const result = await window.electronAPI.login(username, password, remember);
      if (result.success) {
        setLoggedIn(result.username || username, !!result.demo);
        if (result.courses) {
          setCourses(result.courses);
        }
        return true;
      } else {
        setError(result.error || 'Login failed');
        return false;
      }
    } catch {
      setError('An unexpected error occurred');
      return false;
    } finally {
      setLoading(false);
    }
  }, [setLoggedIn, setCourses, setLoading, setError]);

  const logout = useCallback(async () => {
    await window.electronAPI.logout();
    setLoggedOut();
    setCourses([]);
  }, [setLoggedOut, setCourses]);

  const loadSavedCredentials = useCallback(async () => {
    try {
      return await window.electronAPI.getSavedCredentials();
    } catch {
      return { username: null, password: null };
    }
  }, []);

  return {
    isLoggedIn,
    username,
    isDemo,
    error,
    login,
    logout,
    loadSavedCredentials,
  };
}
