import { useCallback, useEffect } from 'react';
import { useCourseStore } from '../state/course-store';

export function useCourses() {
  const { courses, isLoading, error, setCourses, setLoading, setError } = useCourseStore();

  const loadCourses = useCallback(async () => {
    setLoading(true);
    try {
      const data = await window.electronAPI.getCourses();
      setCourses(data);
    } catch {
      setError('Failed to load courses');
    } finally {
      setLoading(false);
    }
  }, [setCourses, setLoading, setError]);

  const refreshCourses = useCallback(async () => {
    setLoading(true);
    try {
      const data = await window.electronAPI.refreshCourses();
      setCourses(data);
    } catch {
      setError('Failed to refresh courses');
    } finally {
      setLoading(false);
    }
  }, [setCourses, setLoading, setError]);

  return {
    courses,
    isLoading,
    error,
    loadCourses,
    refreshCourses,
  };
}
