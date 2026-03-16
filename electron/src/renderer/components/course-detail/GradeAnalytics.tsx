import CumulativeGradeChart from '../charts/CumulativeGradeChart';
import type { GradeTimelinePoint, AssignmentGroup } from '../../types';

interface GradeAnalyticsProps {
  timeline: GradeTimelinePoint[];
  assignmentGroups: AssignmentGroup[];
}

export default function GradeAnalytics({ timeline, assignmentGroups }: GradeAnalyticsProps) {
  // Calculate top impacts
  const topImpacts = timeline
    .filter((t) => !t.firstPoint && t.isHighImpact)
    .sort((a, b) => Math.abs(b.impact) - Math.abs(a.impact))
    .slice(0, 3);

  // Calculate overall grade
  const currentGrade = timeline.length > 0 ? timeline[timeline.length - 1].cumulativeGrade : 0;
  const highestImpact = timeline.length > 0
    ? timeline.reduce((best, pt) => (Math.abs(pt.impact) > Math.abs(best.impact) ? pt : best), timeline[0])
    : null;

  return (
    <div className="space-y-4">
      {/* Impact Summary Cards */}
      <div className="grid grid-cols-2 gap-3">
        <div className="card p-4 text-center">
          <div className="text-xs text-github-text-secondary uppercase tracking-wide font-medium">
            Current Grade
          </div>
          <div
            className="text-3xl font-bold mt-1"
            style={{ color: currentGrade >= 80 ? '#238636' : currentGrade >= 70 ? '#D29922' : '#F85149' }}
          >
            {timeline.length > 0 ? `${currentGrade.toFixed(1)}%` : '--'}
          </div>
        </div>
        <div className="card p-4 text-center">
          <div className="text-xs text-github-text-secondary uppercase tracking-wide font-medium">
            Biggest Impact
          </div>
          <div
            className="text-3xl font-bold mt-1"
            style={{ color: highestImpact && highestImpact.impact >= 0 ? '#238636' : '#F85149' }}
          >
            {highestImpact && !highestImpact.firstPoint
              ? `${highestImpact.impact >= 0 ? '+' : ''}${highestImpact.impact.toFixed(1)}%`
              : '--'}
          </div>
          {highestImpact && !highestImpact.firstPoint && (
            <div className="text-xs text-github-text-muted mt-1 truncate">
              {highestImpact.assignmentName}
            </div>
          )}
        </div>
      </div>

      {/* Impact Badges */}
      {topImpacts.length > 0 && (
        <div className="flex flex-wrap gap-2">
          <span className="text-xs text-github-text-muted font-medium uppercase tracking-wide self-center mr-1">
            Key Impacts:
          </span>
          {topImpacts.map((pt) => (
            <span
              key={pt.assignmentName}
              className="badge text-xs"
              style={{
                backgroundColor: pt.impact >= 0 ? '#238636' : '#F85149',
                color: 'white',
              }}
            >
              {pt.assignmentName}: {pt.impact >= 0 ? '+' : ''}{pt.impact.toFixed(1)}%
            </span>
          ))}
        </div>
      )}

      {/* Cumulative Chart */}
      {timeline.length > 1 && (
        <div className="card p-4">
          <h3 className="text-sm font-medium text-github-text-secondary mb-3">
            Cumulative Grade Trend
          </h3>
          <CumulativeGradeChart timeline={timeline} />
        </div>
      )}
    </div>
  );
}
