import { useNavigate } from 'react-router-dom';
import { getCourseDisplayText } from '../../utils/course-code-parser';
import type { Course } from '../../types';

interface CourseCardProps {
  course: Course;
}

export default function CourseCard({ course }: CourseCardProps) {
  const navigate = useNavigate();
  const displayName = getCourseDisplayText(course.code);

  return (
    <button
      onClick={() => navigate(`/course/${encodeURIComponent(course.code)}`)}
      className="card p-4 hover:border-github-accent/50 transition-all text-left w-full group"
    >
      <div className="flex items-center justify-between">
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-3">
            <span className="text-sm font-mono font-semibold text-github-accent group-hover:text-github-accent-hover transition-colors">
              {course.code}
            </span>
            {course.hasValidMark && (
              <span
                className="badge text-white text-xs font-semibold"
                style={{ backgroundColor: course.gradeColor }}
              >
                {course.displayMark}
              </span>
            )}
            {!course.hasValidMark && (
              <span className="badge bg-github-border text-github-text-muted text-xs">
                N/A
              </span>
            )}
          </div>
          <p className="text-sm text-github-text-secondary mt-1 truncate">{displayName}</p>
          <div className="flex items-center gap-3 mt-2 text-xs text-github-text-muted">
            {course.block > 0 && (
              <span className="flex items-center gap-1">
                <svg className="w-3.5 h-3.5" viewBox="0 0 20 20" fill="currentColor">
                  <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm.75-13a.75.75 0 00-1.5 0v5c0 .414.336.75.75.75h4a.75.75 0 000-1.5h-3.25V5z" clipRule="evenodd" />
                </svg>
                Block {course.block}
              </span>
            )}
            {course.room && (
              <span className="flex items-center gap-1">
                <svg className="w-3.5 h-3.5" viewBox="0 0 20 20" fill="currentColor">
                  <path fillRule="evenodd" d="M4 2a1 1 0 011 1v2.101a7.002 7.002 0 0111.601 2.566 1 1 0 11-1.885.666A5.002 5.002 0 005.999 7H9a1 1 0 010 2H4a1 1 0 01-1-1V3a1 1 0 011-1zm.008 9.057a1 1 0 011.276.61A5.002 5.002 0 0014.001 13H11a1 1 0 110-2h5a1 1 0 011 1v5a1 1 0 11-2 0v-2.101a7.002 7.002 0 01-11.601-2.566 1 1 0 01.61-1.276z" clipRule="evenodd" />
                </svg>
                {course.room}
              </span>
            )}
            {course.partiallyParsed && (
              <span className="text-github-warning">Partial data</span>
            )}
          </div>
        </div>
        <svg className="w-4 h-4 text-github-text-muted group-hover:text-github-text-secondary transition-colors shrink-0" viewBox="0 0 20 20" fill="currentColor">
          <path fillRule="evenodd" d="M7.21 14.77a.75.75 0 01.02-1.06L11.168 10 7.23 6.29a.75.75 0 111.04-1.08l4.5 4.25a.75.75 0 010 1.08l-4.5 4.25a.75.75 0 01-1.06-.02z" clipRule="evenodd" />
        </svg>
      </div>
    </button>
  );
}
