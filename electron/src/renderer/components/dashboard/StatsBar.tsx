import type { Course } from '../../types';
import { markToGpa } from '../../utils/grade-colors';

interface StatsBarProps {
  courses: Course[];
}

export default function StatsBar({ courses }: StatsBarProps) {
  const validCourses = courses.filter((c) => c.hasValidMark);
  const average =
    validCourses.length > 0
      ? validCourses.reduce((sum, c) => sum + (c.numericMark || 0), 0) / validCourses.length
      : 0;
  const gpa =
    validCourses.length > 0
      ? validCourses.reduce((sum, c) => sum + markToGpa(c.numericMark || 0), 0) / validCourses.length
      : 0;

  const stats = [
    {
      label: 'Average',
      value: validCourses.length > 0 ? `${average.toFixed(1)}%` : '--',
      icon: (
        <svg className="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
          <path d="M3 13.125C3 12.504 3.504 12 4.125 12h2.25c.621 0 1.125.504 1.125 1.125v6.75C7.5 20.496 6.996 21 6.375 21h-2.25A1.125 1.125 0 013 19.875v-6.75zM9.75 8.625c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125v11.25c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 01-1.125-1.125V8.625zM16.5 4.125c0-.621.504-1.125 1.125-1.125h2.25C20.496 3 21 3.504 21 4.125v15.75c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 01-1.125-1.125V4.125z" strokeLinecap="round" strokeLinejoin="round" />
        </svg>
      ),
      color: average >= 80 ? '#238636' : average >= 70 ? '#D29922' : '#F85149',
    },
    {
      label: 'GPA',
      value: validCourses.length > 0 ? gpa.toFixed(2) : '--',
      icon: (
        <svg className="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
          <path d="M4.26 10.147a60.436 60.436 0 00-.491 6.347A48.627 48.627 0 0112 20.904a48.627 48.627 0 018.232-4.41 60.46 60.46 0 00-.491-6.347m-15.482 0a50.57 50.57 0 00-2.658-.813A59.905 59.905 0 0112 3.493a59.902 59.902 0 0110.399 5.84c-.896.248-1.783.52-2.658.814m-15.482 0A50.697 50.697 0 0112 13.489a50.702 50.702 0 017.74-3.342" strokeLinecap="round" strokeLinejoin="round" />
        </svg>
      ),
      color: gpa >= 3.5 ? '#238636' : gpa >= 2.5 ? '#D29922' : '#F85149',
    },
    {
      label: 'Courses',
      value: `${courses.length}`,
      icon: (
        <svg className="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
          <path d="M12 6.042A8.967 8.967 0 006 3.75c-1.052 0-2.062.18-3 .512v14.25A8.987 8.987 0 016 18c2.305 0 4.408.867 6 2.292m0-14.25a8.966 8.966 0 016-2.292c1.052 0 2.062.18 3 .512v14.25A8.987 8.987 0 0018 18a8.967 8.967 0 00-6 2.292m0-14.25v14.25" strokeLinecap="round" strokeLinejoin="round" />
        </svg>
      ),
      color: '#58A6FF',
    },
  ];

  return (
    <div className="grid grid-cols-3 gap-4">
      {stats.map((stat) => (
        <div key={stat.label} className="card p-4">
          <div className="flex items-center gap-3">
            <div className="text-github-text-secondary">{stat.icon}</div>
            <div>
              <div className="text-xs text-github-text-secondary font-medium uppercase tracking-wide">
                {stat.label}
              </div>
              <div className="text-2xl font-bold mt-0.5" style={{ color: stat.color }}>
                {stat.value}
              </div>
            </div>
          </div>
        </div>
      ))}
    </div>
  );
}
