import type { AssignmentGroup } from '../../types';
import { getGradeColor } from '../../utils/grade-colors';

interface AssignmentTableProps {
  groups: AssignmentGroup[];
}

export default function AssignmentTable({ groups }: AssignmentTableProps) {
  if (groups.length === 0) return null;

  return (
    <div className="card overflow-hidden">
      <div className="px-4 py-3 border-b border-github-border">
        <h3 className="text-sm font-medium text-github-text-secondary">Assignments</h3>
      </div>
      <div className="divide-y divide-github-border/50">
        {groups.map((group, i) => {
          const avgPct = calcGroupAverage(group);
          return (
            <div key={i} className="px-4 py-3 hover:bg-github-border/20 transition-colors">
              {/* Assignment Name Row */}
              <div className="flex items-center justify-between mb-2">
                <span className="text-sm font-medium">{group.name}</span>
                <div className="flex items-center gap-2">
                  <span className="text-sm font-semibold" style={{ color: avgPct !== null ? getGradeColor(avgPct) : '#8B949E' }}>
                    {avgPct !== null ? `${avgPct.toFixed(1)}%` : '--'}
                  </span>
                </div>
              </div>
              {/* Category Marks */}
              <div className="flex flex-wrap gap-2">
                {group.assignments.map((a, j) => (
                  <div
                    key={j}
                    className="flex items-center gap-1.5 text-xs bg-github-bg rounded-md px-2 py-1 border border-github-border/50"
                  >
                    <span className="font-semibold text-github-accent">{a.category}</span>
                    {a.markAchieved != null && a.markPossible != null ? (
                      <>
                        <span style={{ color: a.percentage && a.percentage >= 80 ? '#238636' : a.percentage && a.percentage >= 70 ? '#D29922' : '#F85149' }}>
                          {a.markAchieved}
                        </span>
                        <span className="text-github-text-muted">/</span>
                        <span>{a.markPossible}</span>
                      </>
                    ) : (
                      <span className="text-github-text-muted">N/A</span>
                    )}
                    {a.weight && a.weight > 0 && (
                      <span className="text-github-text-muted ml-1">({a.weight}%)</span>
                    )}
                  </div>
                ))}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}

function calcGroupAverage(group: AssignmentGroup): number | null {
  let sum = 0;
  let count = 0;
  for (const a of group.assignments) {
    if (a.percentage != null) {
      sum += a.percentage;
      count++;
    }
  }
  return count > 0 ? sum / count : null;
}
