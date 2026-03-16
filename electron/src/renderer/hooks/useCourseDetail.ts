import { useState, useCallback } from 'react';
import type { Course, GradeTimelinePoint, CategoryPerformance, AssignmentGroup } from '../types';
import { calculateGradeTimeline, calculateCategoryPerformance, groupAssignments } from '../utils/grade-impact-calculator';

export function useCourseDetail() {
  const [detail, setDetail] = useState<Course | null>(null);
  const [timeline, setTimeline] = useState<GradeTimelinePoint[]>([]);
  const [categoryPerf, setCategoryPerf] = useState<CategoryPerformance[]>([]);
  const [assignmentGroups, setAssignmentGroups] = useState<AssignmentGroup[]>([]);
  const [isLoading, setIsLoading] = useState(false);

  const loadDetail = useCallback(async (reportUrl: string, courseCode?: string) => {
    setIsLoading(true);
    try {
      const result = await window.electronAPI.getCourseDetail(reportUrl);
      if (result.success && result.course) {
        setDetail(result.course);
        setTimeline(calculateGradeTimeline(
          groupAssignments(result.course.assignments),
          result.course.weightTable
        ));
        setCategoryPerf(calculateCategoryPerformance(
          result.course.assignments,
          result.course.weightTable
        ));
        setAssignmentGroups(groupAssignments(result.course.assignments));
      }
    } catch {
      // ignore
    } finally {
      setIsLoading(false);
    }
  }, []);

  const loadFromCache = useCallback((course: Course) => {
    setDetail(course);
    const groups = groupAssignments(course.assignments);
    setAssignmentGroups(groups);
    setTimeline(calculateGradeTimeline(groups, course.weightTable));
    setCategoryPerf(calculateCategoryPerformance(course.assignments, course.weightTable));
  }, []);

  return { detail, timeline, categoryPerf, assignmentGroups, isLoading, loadDetail, loadFromCache };
}
