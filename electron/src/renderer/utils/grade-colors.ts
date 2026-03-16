export interface GradeColorInfo {
  color: string;
  level: string;
  letter: string;
}

const GRADE_TIERS: Array<{ min: number; color: string; level: string; letter: string }> = [
  { min: 95, color: '#2EA043', level: 'Level 4+ (Excellent!)', letter: 'A+' },
  { min: 90, color: '#3FB950', level: 'Level 4 (Very Good)', letter: 'A' },
  { min: 85, color: '#238636', level: 'Level 4 (Good)', letter: 'A-' },
  { min: 80, color: '#D29922', level: 'Level 3+ (Good)', letter: 'B+' },
  { min: 75, color: '#9A6700', level: 'Level 3 (Satisfactory)', letter: 'B' },
  { min: 70, color: '#DB6D28', level: 'Level 3 (Adequate)', letter: 'B-' },
  { min: 65, color: '#A57104', level: 'Level 2+ (Passing)', letter: 'C+' },
  { min: 60, color: '#F85149', level: 'Level 2 (Below Average)', letter: 'C' },
  { min: 0,  color: '#D73A49', level: 'Level 1 (Below Expectations)', letter: 'D' },
];

export function getGradeColor(mark: number | string | null | undefined): string {
  if (mark === null || mark === undefined || mark === 'N/A') return '#30363D';
  const num = typeof mark === 'number' ? mark : parseFloat(String(mark));
  if (isNaN(num)) return '#30363D';
  return GRADE_TIERS.find((t) => num >= t.min)?.color ?? '#30363D';
}

export function getGradeLevel(mark: number | string | null | undefined): string {
  if (mark === null || mark === undefined || mark === 'N/A') return 'No Mark';
  const num = typeof mark === 'number' ? mark : parseFloat(String(mark));
  if (isNaN(num)) return 'No Mark';
  return GRADE_TIERS.find((t) => num >= t.min)?.level ?? 'No Mark';
}

export function getGradeLetter(mark: number | string | null | undefined): string {
  if (mark === null || mark === undefined || mark === 'N/A') return '';
  const num = typeof mark === 'number' ? mark : parseFloat(String(mark));
  if (isNaN(num)) return '';
  return GRADE_TIERS.find((t) => num >= t.min)?.letter ?? '';
}

export function getGradeColorInfo(mark: number | string | null | undefined): GradeColorInfo {
  return {
    color: getGradeColor(mark),
    level: getGradeLevel(mark),
    letter: getGradeLetter(mark),
  };
}

/** Simplified 3-tier trend color */
export function getTrendColor(mark: number): string {
  if (mark >= 90) return '#238636';
  if (mark >= 80) return '#D29922';
  return '#DB6D28';
}

/** Impact colors */
export const IMPACT_POSITIVE_COLOR = '#238636';
export const IMPACT_NEGATIVE_COLOR = '#F85149';

/** GPA on Ontario 4.0 scale */
export function markToGpa(mark: number): number {
  if (mark >= 90) return 4.0;
  if (mark >= 80) return 3.0;
  if (mark >= 70) return 2.0;
  if (mark >= 60) return 1.0;
  return 0.0;
}
