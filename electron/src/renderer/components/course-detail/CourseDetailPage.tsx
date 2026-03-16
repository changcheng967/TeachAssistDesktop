import { useEffect, useMemo } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useCourseDetail } from '../../hooks/useCourseDetail';
import { useCourseStore } from '../../state/course-store';
import { getCourseDisplayText } from '../../utils/course-code-parser';
import GradeAnalytics from './GradeAnalytics';
import CategoryPerformance from './CategoryPerformance';
import AssignmentTable from './AssignmentTable';

export default function CourseDetailPage() {
  const { code } = useParams<{ code: string }>();
  const navigate = useNavigate();
  const courses = useCourseStore((s) => s.courses);
  const { detail, timeline, categoryPerf, assignmentGroups, isLoading, loadDetail, loadFromCache } = useCourseDetail();

  const courseCode = decodeURIComponent(code || '');

  // Load from cache on mount
  useEffect(() => {
    const cached = courses.find((c) => c.code === courseCode);
    if (cached) {
      loadFromCache(cached);
      // Also try to load full detail from server if reportUrl exists
      if (cached.reportUrl) {
        loadDetail(cached.reportUrl, cached.code);
      }
    }
  }, [courseCode, courses, loadFromCache, loadDetail]);

  const course = detail;

  const displayName = useMemo(() => getCourseDisplayText(courseCode), [courseCode]);

  if (isLoading && !course) {
    return (
      <div className="space-y-4 animate-fade-in">
        <div className="h-6 w-32 bg-github-border rounded animate-pulse" />
        <div className="h-8 w-64 bg-github-border rounded animate-pulse" />
        <div className="card p-6 animate-pulse">
          <div className="h-4 w-48 bg-github-border rounded" />
        </div>
      </div>
    );
  }

  if (!course) {
    return (
      <div className="space-y-4 animate-fade-in">
        <button onClick={() => navigate('/')} className="btn-ghost text-sm flex items-center gap-1">
          <svg className="w-4 h-4" viewBox="0 0 20 20" fill="currentColor">
            <path fillRule="evenodd" d="M17 10a.75.75 0 01-.75.75H5.612l4.158 3.96a.75.75 0 11-1.04 1.08l-5.5-5.25a.75.75 0 010-1.08l5.5-5.25a.75.75 0 111.04 1.08L5.612 9.25H16.25A.75.75 0 0117 10z" clipRule="evenodd" />
          </svg>
          Back to Dashboard
        </button>
        <div className="card p-8 text-center">
          <p className="text-github-text-secondary">Course not found.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6 animate-fade-in">
      {/* Back button */}
      <button
        onClick={() => navigate('/')}
        className="btn-ghost text-sm flex items-center gap-1 w-fit"
      >
        <svg className="w-4 h-4" viewBox="0 0 20 20" fill="currentColor">
          <path fillRule="evenodd" d="M17 10a.75.75 0 01-.75.75H5.612l4.158 3.96a.75.75 0 11-1.04 1.08l-5.5-5.25a.75.75 0 010-1.08l5.5-5.25a.75.75 0 111.04 1.08L5.612 9.25H16.25A.75.75 0 0117 10z" clipRule="evenodd" />
        </svg>
        Back to Dashboard
      </button>

      {/* Course Header */}
      <div className="flex items-start justify-between">
        <div>
          <div className="flex items-center gap-3">
            <h1 className="text-xl font-semibold">{courseCode}</h1>
            {course.hasValidMark && (
              <span
                className="badge text-white text-sm font-semibold px-3 py-1"
                style={{ backgroundColor: course.gradeColor }}
              >
                {course.displayMark}
              </span>
            )}
          </div>
          <p className="text-sm text-github-text-secondary mt-1">{displayName}</p>
          {course.isCGCFormat && (
            <span className="badge bg-github-accent/20 text-github-accent text-xs mt-2">
              Overall Expectations Format
            </span>
          )}
          {course.partiallyParsed && (
            <span className="badge bg-github-warning/20 text-github-warning text-xs mt-2">
              Partially Parsed
            </span>
          )}
        </div>
      </div>

      {/* Analytics */}
      {assignmentGroups.length > 0 && (
        <GradeAnalytics timeline={timeline} assignmentGroups={assignmentGroups} />
      )}

      {/* Category Performance */}
      {categoryPerf.length > 0 && <CategoryPerformance categories={categoryPerf} />}

      {/* Assignment Table */}
      {assignmentGroups.length > 0 && <AssignmentTable groups={assignmentGroups} />}

      {/* Empty state */}
      {course.assignments.length === 0 && (
        <div className="card p-8 text-center">
          <p className="text-github-text-secondary">No assignment data available.</p>
          {course.reportUrl && (
            <p className="text-xs text-github-text-muted mt-2">
              Click refresh on the dashboard to load assignment details.
            </p>
          )}
        </div>
      )}
    </div>
  );
}
