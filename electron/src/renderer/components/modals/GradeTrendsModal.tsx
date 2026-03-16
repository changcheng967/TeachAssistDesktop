import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';
import type { Course } from '../../types';
import { markToGpa } from '../../utils/grade-colors';

interface GradeTrendsModalProps {
  courses: Course[];
}

export default function GradeTrendsModal({ courses }: GradeTrendsModalProps) {
  const validCourses = courses.filter((c) => c.hasValidMark);
  const average =
    validCourses.length > 0
      ? validCourses.reduce((sum, c) => sum + (c.numericMark || 0), 0) / validCourses.length
      : 0;
  const gpa =
    validCourses.length > 0
      ? validCourses.reduce((sum, c) => sum + markToGpa(c.numericMark || 0), 0) / validCourses.length
      : 0;

  const data = validCourses.map((c) => ({
    code: c.code,
    mark: c.numericMark || 0,
    fill: c.gradeColor,
  }));

  const strongest = [...validCourses].sort((a, b) => (b.numericMark || 0) - (a.numericMark || 0))[0];
  const weakest = [...validCourses].sort((a, b) => (a.numericMark || 0) - (b.numericMark || 0))[0];

  return (
    <div className="space-y-4">
      {/* Insights */}
      <div className="grid grid-cols-2 gap-3">
        <div className="bg-github-bg rounded-md p-3 text-center">
          <div className="text-xs text-github-text-muted">Average</div>
          <div className="text-xl font-bold" style={{ color: average >= 80 ? '#238636' : average >= 70 ? '#D29922' : '#F85149' }}>
            {average.toFixed(1)}%
          </div>
        </div>
        <div className="bg-github-bg rounded-md p-3 text-center">
          <div className="text-xs text-github-text-muted">GPA</div>
          <div className="text-xl font-bold">{gpa.toFixed(2)}</div>
        </div>
      </div>

      {/* Insights text */}
      {strongest && (
        <div className="text-sm">
          <span className="text-github-success font-medium">Strongest:</span>{' '}
          {strongest.code} ({strongest.displayMark})
        </div>
      )}
      {weakest && (
        <div className="text-sm">
          <span className="text-github-danger font-medium">Needs improvement:</span>{' '}
          {weakest.code} ({weakest.displayMark})
        </div>
      )}

      {/* Chart */}
      {data.length > 0 && (
        <div className="h-56">
          <ResponsiveContainer width="100%" height="100%">
            <BarChart data={data} margin={{ top: 5, right: 20, left: 0, bottom: 5 }}>
              <CartesianGrid strokeDasharray="3 3" stroke="#30363D" />
              <XAxis dataKey="code" tick={{ fill: '#8B949E', fontSize: 11 }} axisLine={{ stroke: '#30363D' }} tickLine={false} />
              <YAxis domain={[0, 100]} tick={{ fill: '#8B949E', fontSize: 11 }} axisLine={{ stroke: '#30363D' }} tickLine={false} />
              <Tooltip
                contentStyle={{
                  backgroundColor: '#161B22',
                  border: '1px solid #30363D',
                  borderRadius: '6px',
                  color: '#E6EDF3',
                  fontSize: '12px',
                }}
                formatter={(value: number) => [`${value}%`, 'Mark']}
              />
              <Bar dataKey="mark" radius={[4, 4, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </div>
      )}
    </div>
  );
}
