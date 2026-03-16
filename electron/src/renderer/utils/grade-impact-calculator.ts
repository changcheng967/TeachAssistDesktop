import type { Assignment, AssignmentGroup, WeightTable, GradeTimelinePoint, AssignmentImpact } from '../types';

/** Create a WeightTable helper */
export function createWeightTable(weights?: Record<string, number>): WeightTable {
  return {
    weights: weights || {},
    getWeight(category: string) {
      return this.weights[category];
    },
    setWeight(category: string, weight: number) {
      this.weights[category] = weight;
    },
  };
}

/** Get assignment percentage */
function getAssignmentPercentage(a: Assignment): number | null {
  if (a.markAchieved == null || a.markPossible == null || a.markPossible === 0) return null;
  return (a.markAchieved / a.markPossible) * 100;
}

/** Group assignments by name */
export function groupAssignments(assignments: Assignment[]): AssignmentGroup[] {
  const map = new Map<string, Assignment[]>();
  for (const a of assignments) {
    const list = map.get(a.name) || [];
    list.push(a);
    map.set(a.name, list);
  }
  return Array.from(map.entries()).map(([name, group]) => ({
    name,
    assignments: group,
    gradeColor: getGroupAverageColor(group),
    impact: undefined,
  }));
}

function getGroupAverageColor(assignments: Assignment[]): string {
  const { getGradeColor } = require('./grade-colors');
  let sum = 0;
  let count = 0;
  for (const a of assignments) {
    const pct = getAssignmentPercentage(a);
    if (pct !== null) {
      sum += pct;
      count++;
    }
  }
  return count > 0 ? getGradeColor(sum / count) : '#30363D';
}

/** Calculate weighted cumulative grade timeline */
export function calculateGradeTimeline(
  groups: AssignmentGroup[],
  weightTable: WeightTable
): GradeTimelinePoint[] {
  if (groups.length === 0) return [];

  // Sort groups by date
  const sorted = [...groups].sort((a, b) => {
    const dateA = a.assignments[0]?.date;
    const dateB = b.assignments[0]?.date;
    if (!dateA && !dateB) return 0;
    if (!dateA) return 1;
    if (!dateB) return -1;
    return dateA.localeCompare(dateB);
  });

  let weightedSum = 0;
  let totalWeight = 0;
  const timeline: GradeTimelinePoint[] = [];
  const impacts: Array<{ delta: number; abs: number }> = [];

  sorted.forEach((group, i) => {
    // Score the group
    let scoreSum = 0;
    let scoreCount = 0;
    for (const a of group.assignments) {
      const pct = getAssignmentPercentage(a);
      if (pct !== null) {
        scoreSum += pct;
        scoreCount++;
      }
    }
    const score = scoreCount > 0 ? scoreSum / scoreCount : 0;

    // Weight for this group
    let groupWeight = 0;
    for (const a of group.assignments) {
      const w = weightTable.getWeight(a.category);
      if (w !== undefined) {
        groupWeight = Math.max(groupWeight, w);
      }
    }
    if (groupWeight === 0) {
      // Equal weight
      groupWeight = 1;
    }

    const cumulativeBefore = totalWeight > 0 ? weightedSum / totalWeight : 0;
    weightedSum += score * groupWeight;
    totalWeight += groupWeight;
    const cumulativeAfter = weightedSum / totalWeight;
    const impact = i === 0 ? 0 : cumulativeAfter - cumulativeBefore;

    impacts.push({ delta: impact, abs: Math.abs(impact) });

    timeline.push({
      index: i,
      assignmentName: group.name,
      date: group.assignments[0]?.date,
      cumulativeGrade: Math.round(cumulativeAfter * 10) / 10,
      impact: Math.round(impact * 10) / 10,
      isHighImpact: false, // set below
      firstPoint: i === 0,
    });
  });

  // Top 3 by |impact| magnitude are high impact, or |impact| >= 3
  const threshold = 3.0;
  const sortedByImpact = impacts
    .map((imp, i) => ({ ...imp, i }))
    .sort((a, b) => b.abs - a.abs);

  const highImpactSet = new Set<number>();
  for (let i = 0; i < Math.min(3, sortedByImpact.length); i++) {
    highImpactSet.add(sortedByImpact[i].i);
  }
  for (let i = 0; i < timeline.length; i++) {
    if (Math.abs(timeline[i].impact) >= threshold) {
      highImpactSet.add(i);
    }
  }
  for (const idx of highImpactSet) {
    timeline[idx].isHighImpact = true;
  }

  return timeline;
}

/** Calculate impacts per group */
export function calculateImpacts(
  groups: AssignmentGroup[],
  weightTable: WeightTable
): AssignmentImpact[] {
  const timeline = calculateGradeTimeline(groups, weightTable);
  return timeline.map((pt, i) => ({
    assignmentName: pt.assignmentName,
    impactDelta: pt.impact,
    isPositive: pt.impact >= 0,
    isHighImpact: pt.isHighImpact,
    cumulativeAfter: pt.cumulativeGrade,
    cumulativeBefore: i === 0 ? 0 : timeline[i - 1].cumulativeGrade,
    displayImpact: `${pt.impact >= 0 ? '+' : ''}${pt.impact.toFixed(1)}%`,
    impactColor: pt.impact >= 0 ? '#238636' : '#F85149',
  }));
}

/** Calculate category performance stats */
export function calculateCategoryPerformance(
  assignments: Assignment[],
  weightTable: WeightTable
): Array<{ code: string; name: string; percentage: number; weight: number; assignmentCount: number; gradeColor: string }> {
  const categoryNames: Record<string, string> = {
    KU: 'Knowledge',
    T: 'Thinking',
    C: 'Communication',
    A: 'Application',
    F: 'Final',
    O: 'Other',
  };

  const categories = new Map<string, { achieved: number; possible: number; count: number }>();

  for (const a of assignments) {
    if (a.markAchieved == null || a.markPossible == null) continue;
    const cat = a.category.toUpperCase();
    const data = categories.get(cat) || { achieved: 0, possible: 0, count: 0 };
    data.achieved += a.markAchieved;
    data.possible += a.markPossible;
    data.count++;
    categories.set(cat, data);
  }

  const { getGradeColor } = require('./grade-colors');
  return Array.from(categories.entries()).map(([code, data]) => ({
    code,
    name: categoryNames[code] || code,
    percentage: data.possible > 0 ? Math.round((data.achieved / data.possible) * 1000) / 10 : 0,
    weight: weightTable.getWeight(code) ?? 0,
    assignmentCount: data.count,
    gradeColor: data.possible > 0 ? getGradeColor((data.achieved / data.possible) * 100) : '#30363D',
  }));
}
