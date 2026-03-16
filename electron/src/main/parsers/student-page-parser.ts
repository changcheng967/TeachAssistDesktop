import * as cheerio from 'cheerio';
import type { Course } from '../../renderer/types/course';
import { createWeightTable } from '../../renderer/utils/grade-impact-calculator';
import { parseLevelMark } from './helpers';

export function parseStudentPage(html: string): Course[] {
  const $ = cheerio.load(html);
  const courses: Course[] = [];

  // Find the main course table
  const table = $("table[width='85%']");
  if (table.length === 0) return courses;

  const rows = table.find('tr');
  rows.each((_, row) => {
    const cells = $(row).find('td');
    if (cells.length < 3) return;

    const cellText = $(row).text();

    // Skip header rows
    if (/block|course|room|mark/i.test(cellText.substring(0, 60))) return;

    // Extract mark from anchor text or cell text
    let mark: number | string = 'N/A';
    const markMatch = cellText.match(/current mark\s*=\s*(\d+\.?\d*)\s*%?/i);
    if (markMatch) {
      mark = parseFloat(markMatch[1]);
    } else {
      const levelMark = parseLevelMark(cellText);
      if (levelMark !== null) mark = levelMark;
    }

    // Extract subject_id and student_id from hrefs
    const anchor = $(row).find('a[href*="subject_id"]');
    const href = anchor.attr('href') || '';
    const subjectIdMatch = href.match(/subject_id=(\d+)/);
    const studentIdMatch = href.match(/student_id=(\d+)/);
    const subjectId = subjectIdMatch ? subjectIdMatch[1] : undefined;
    const studentId = studentIdMatch ? studentIdMatch[1] : undefined;

    // Build report URL
    const baseMatch = href.match(/^(https?:\/\/[^?]+)/);
    const baseUrl = baseMatch ? baseMatch[1] : '';
    const reportUrl = href || undefined;

    // Extract block
    let block = 0;
    const blockMatch = cellText.match(/Block:\s*(?:P)?(\d+)/i);
    if (blockMatch) block = parseInt(blockMatch[1], 10);

    // Extract room
    let room = '';
    const roomMatch = cellText.match(/rm\.\s*(.+?)(?:\s*$|\s*<)/im);
    if (roomMatch) room = roomMatch[1].trim();

    // Extract course code
    let code = '';
    const standardMatch = cellText.match(/([A-Z]{3}\d[A-Z]\d-\d{1,2})/);
    if (standardMatch) {
      code = standardMatch[1];
    } else {
      const eslMatch = cellText.match(/([A-Z]{4,5}\d-\d{1,2})/);
      if (eslMatch) code = eslMatch[1];
    }

    if (!code) return;

    // Clean up code (extract just the code portion)
    const cleanCodeMatch = code.match(/([A-Z]{2,5}\d?[A-Z]?\d*-\d+)/);
    if (cleanCodeMatch) code = cleanCodeMatch[1];

    courses.push({
      code,
      name: code, // Will be resolved by course-code-parser in UI
      block,
      room,
      overallMark: mark,
      assignments: [],
      weightTable: createWeightTable(),
      subjectId,
      studentId,
      reportUrl,
      partiallyParsed: false,
      isCGCFormat: false,
      assignmentTrends: [],
      displayMark: typeof mark === 'number' ? mark.toFixed(1) : 'N/A',
      hasValidMark: typeof mark === 'number',
      numericMark: typeof mark === 'number' ? mark : null,
      gradeColor: getGradeColor(mark),
      gradeLevel: getGradeLevel(mark),
      gradeLetter: getGradeLetter(mark),
    });
  });

  return courses;
}

// Inline grade helpers to avoid circular deps with renderer
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
