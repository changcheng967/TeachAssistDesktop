import type { Course, Assignment } from '../../renderer/types/course';
import { createWeightTable } from '../../renderer/utils/grade-impact-calculator';
import { extractOverallMark, extractCourseCode } from './helpers';

export function parseFallbackCourseDetail(html: string, existingCode?: string): Course {
  const assignments: Assignment[] = [];
  const fracRegex = /([\d.]+)\s*\/\s*([\d.]+)/g;
  let match;

  // Get all text content to extract fractions
  while ((match = fracRegex.exec(html)) !== null) {
    const achieved = parseFloat(match[1]);
    const possible = parseFloat(match[2]);

    // Skip if denominator is 0 or achieved > possible
    if (possible === 0 || achieved > possible) continue;

    assignments.push({
      name: `Assignment ${assignments.length + 1}`,
      markAchieved: achieved,
      markPossible: possible,
      category: 'O',
      percentage: (achieved / possible) * 100,
      isMissing: false,
    });
  }

  const code = existingCode || extractCourseCode(html);
  const overallMark = extractOverallMark(html);
  const numMark = typeof overallMark === 'number' ? overallMark : null;

  return {
    code,
    name: code,
    block: 0,
    room: '',
    overallMark,
    assignments,
    weightTable: createWeightTable(),
    partiallyParsed: true,
    isCGCFormat: false,
    assignmentTrends: [],
    displayMark: typeof overallMark === 'number' ? overallMark.toFixed(1) : 'N/A',
    hasValidMark: numMark !== null,
    numericMark: numMark,
    gradeColor: getGradeColor(overallMark),
    gradeLevel: getGradeLevel(overallMark),
    gradeLetter: getGradeLetter(overallMark),
  };
}

function getGradeColor(mark: number | string): string {
  if (mark === 'N/A') return '#30363D';
  const num = typeof mark === 'number' ? mark : parseFloat(String(mark));
  if (isNaN(num)) return '#30363D';
  if (num >= 95) return '#2EA043';
  if (num >= 90) return '#3FB950';
  if (num >= 85) return '#238636';
  if (num >= 80) return '#D29922';
  if (num >= 75) return '#9A6700';
  if (num >= 70) return '#DB6D28';
  if (num >= 65) return '#A57104';
  if (num >= 60) return '#F85149';
  return '#D73A49';
}

function getGradeLevel(mark: number | string): string {
  if (mark === 'N/A') return 'No Mark';
  const num = typeof mark === 'number' ? mark : parseFloat(String(mark));
  if (isNaN(num)) return 'No Mark';
  if (num >= 95) return 'Level 4+ (Excellent!)';
  if (num >= 90) return 'Level 4 (Very Good)';
  if (num >= 85) return 'Level 4 (Good)';
  if (num >= 80) return 'Level 3+ (Good)';
  if (num >= 75) return 'Level 3 (Satisfactory)';
  if (num >= 70) return 'Level 3 (Adequate)';
  if (num >= 65) return 'Level 2+ (Passing)';
  if (num >= 60) return 'Level 2 (Below Average)';
  return 'Level 1 (Below Expectations)';
}

function getGradeLetter(mark: number | string): string {
  if (mark === 'N/A') return '';
  const num = typeof mark === 'number' ? mark : parseFloat(String(mark));
  if (isNaN(num)) return '';
  if (num >= 95) return 'A+';
  if (num >= 90) return 'A';
  if (num >= 85) return 'A-';
  if (num >= 80) return 'B+';
  if (num >= 75) return 'B';
  if (num >= 70) return 'B-';
  if (num >= 65) return 'C+';
  if (num >= 60) return 'C';
  return 'D';
}
