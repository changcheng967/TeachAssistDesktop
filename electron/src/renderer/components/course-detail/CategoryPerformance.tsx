import type { CategoryPerformance } from '../../types';

interface CategoryPerformanceProps {
  categories: CategoryPerformance[];
}

const CATEGORY_LABELS: Record<string, string> = {
  KU: 'Knowledge & Understanding',
  T: 'Thinking',
  C: 'Communication',
  A: 'Application',
  F: 'Final / Culminating',
  O: 'Other',
};

export default function CategoryPerformance({ categories }: CategoryPerformanceProps) {
  if (categories.length === 0) return null;

  return (
    <div className="card p-4">
      <h3 className="text-sm font-medium text-github-text-secondary mb-4">
        Category Performance
      </h3>
      <div className="space-y-4">
        {categories.map((cat) => (
          <div key={cat.code}>
            <div className="flex items-center justify-between mb-1">
              <span className="text-sm font-medium">{CATEGORY_LABELS[cat.code] || cat.code}</span>
              <div className="flex items-center gap-2">
                <span className="text-xs text-github-text-muted">{cat.assignmentCount} tasks</span>
                {cat.weight > 0 && <span className="text-xs text-github-text-muted">({cat.weight}%)</span>}
                <span className="text-sm font-semibold" style={{ color: cat.gradeColor }}>
                  {cat.percentage.toFixed(1)}%
                </span>
              </div>
            </div>
            <div className="h-2 bg-github-border/50 rounded-full overflow-hidden">
              <div
                className="h-full rounded-full transition-all duration-500"
                style={{
                  width: `${Math.min(cat.percentage, 100)}%`,
                  backgroundColor: cat.gradeColor,
                }}
              />
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
