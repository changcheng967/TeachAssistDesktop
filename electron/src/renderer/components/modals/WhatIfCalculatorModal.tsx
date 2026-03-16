import { useState } from 'react';
import { useCourseStore } from '../../state/course-store';
import { markToGpa } from '../../utils/grade-colors';
import type { HypotheticalAssignment } from '../../types';

export default function WhatIfCalculatorModal() {
  const courses = useCourseStore((s) => s.courses);
  const [assignments, setAssignments] = useState<HypotheticalAssignment[]>([
    { name: '', mark: 80, weight: 10 },
  ]);

  const addAssignment = () => {
    setAssignments([...assignments, { name: '', mark: 80, weight: 10 }]);
  };

  const removeAssignment = (index: number) => {
    setAssignments(assignments.filter((_, i) => i !== index));
  };

  const updateAssignment = (index: number, field: keyof HypotheticalAssignment, value: string | number) => {
    const updated = [...assignments];
    updated[index] = { ...updated[index], [field]: value };
    setAssignments(updated);
  };

  // Calculate current GPA
  const validCourses = courses.filter((c) => c.hasValidMark);
  const currentAvg =
    validCourses.length > 0
      ? validCourses.reduce((sum, c) => sum + (c.numericMark || 0), 0) / validCourses.length
      : 0;
  const currentGpa = markToGpa(currentAvg);

  // Calculate projected GPA with hypothetical assignments
  let projectedAvg = currentAvg;
  let projectedGpa = currentGpa;

  const hypotheticalsWithMarks = assignments.filter((a) => a.name.trim() && a.weight > 0);
  if (hypotheticalsWithMarks.length > 0 && validCourses.length > 0) {
    let weightedSum = currentAvg * validCourses.length;
    let totalWeight = validCourses.length;

    for (const a of hypotheticalsWithMarks) {
      weightedSum += a.mark * a.weight;
      totalWeight += a.weight;
    }

    projectedAvg = weightedSum / totalWeight;
    projectedGpa = markToGpa(projectedAvg);
  }

  const diff = projectedAvg - currentAvg;
  const gpaDiff = projectedGpa - currentGpa;

  return (
    <div className="space-y-4">
      {/* Results */}
      <div className="grid grid-cols-2 gap-3">
        <div className="bg-github-bg rounded-md p-3 text-center">
          <div className="text-xs text-github-text-muted">Current</div>
          <div className="text-xl font-bold">{currentAvg.toFixed(1)}%</div>
          <div className="text-xs text-github-text-muted">GPA: {currentGpa.toFixed(2)}</div>
        </div>
        <div className="bg-github-bg rounded-md p-3 text-center">
          <div className="text-xs text-github-text-muted">Projected</div>
          <div className="text-xl font-bold" style={{ color: diff >= 0 ? '#238636' : '#F85149' }}>
            {projectedAvg.toFixed(1)}%
          </div>
          <div className="text-xs">
            <span style={{ color: gpaDiff >= 0 ? '#238636' : '#F85149' }}>
              GPA: {projectedGpa.toFixed(2)} ({gpaDiff >= 0 ? '+' : ''}{gpaDiff.toFixed(2)})
            </span>
          </div>
        </div>
      </div>

      {hypotheticalsWithMarks.length > 0 && (
        <div className="text-sm text-center" style={{ color: diff >= 0 ? '#238636' : '#F85149' }}>
          {diff >= 0 ? '+' : ''}{diff.toFixed(1)}% from {hypotheticalsWithMarks.length} assignment(s)
        </div>
      )}

      {/* Assignment inputs */}
      <div className="space-y-2">
        <div className="text-sm font-medium text-github-text-secondary">Hypothetical Assignments</div>
        {assignments.map((a, i) => (
          <div key={i} className="flex gap-2 items-center">
            <input
              type="text"
              value={a.name}
              onChange={(e) => updateAssignment(i, 'name', e.target.value)}
              placeholder="Assignment name"
              className="flex-1 bg-github-bg border border-github-border rounded-md px-3 py-1.5 text-sm
                         text-github-text-primary placeholder-github-text-muted
                         focus:outline-none focus:border-github-accent"
            />
            <input
              type="number"
              value={a.mark}
              onChange={(e) => updateAssignment(i, 'mark', parseFloat(e.target.value) || 0)}
              className="w-20 bg-github-bg border border-github-border rounded-md px-3 py-1.5 text-sm text-center
                         text-github-text-primary focus:outline-none focus:border-github-accent"
              min={0}
              max={100}
            />
            <div className="flex items-center gap-1">
              <input
                type="number"
                value={a.weight}
                onChange={(e) => updateAssignment(i, 'weight', parseFloat(e.target.value) || 0)}
                className="w-16 bg-github-bg border border-github-border rounded-md px-2 py-1.5 text-sm text-center
                           text-github-text-primary focus:outline-none focus:border-github-accent"
                min={0}
              />
              <span className="text-xs text-github-text-muted">%</span>
            </div>
            <button
              onClick={() => removeAssignment(i)}
              className="p-1 rounded hover:bg-github-border/50 text-github-text-muted hover:text-github-danger transition-colors"
            >
              <svg className="w-4 h-4" viewBox="0 0 20 20" fill="currentColor">
                <path d="M6.28 5.22a.75.75 0 00-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 101.06 1.06L10 11.06l3.72 3.72a.75.75 0 101.06-1.06L11.06 10l3.72-3.72a.75.75 0 00-1.06-1.06L10 8.94 6.28 5.22z" />
              </svg>
            </button>
          </div>
        ))}
      </div>

      <button onClick={addAssignment} className="btn-ghost text-sm w-full">
        + Add Assignment
      </button>
    </div>
  );
}
