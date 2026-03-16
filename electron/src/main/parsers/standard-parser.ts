import * as cheerio from 'cheerio';
import type { Course, Assignment } from '../../renderer/types/course';
import { createWeightTable } from '../../renderer/utils/grade-impact-calculator';
import { categoryFromBgColor, categoryFromName, extractOverallMark, extractCourseCode } from './helpers';

export function parseStandardCourseDetail(html: string, existingCode?: string): Course {
  const $ = cheerio.load(html);
  const assignments: Assignment[] = [];

  // Find the assignment table
  const table = $("table[border='1'][cellpadding='3'][cellspacing='0'][width='100%']");
  if (table.length === 0) {
    return createFallbackCourse(html, existingCode);
  }

  const rows = table.find('tr');
  let currentAssignmentName = '';

  rows.each((_, row) => {
    const cells = $(row).find('td');
    if (cells.length === 0) return;

    // First cell of a rowspan=2 row is the assignment name
    const firstCell = $(cells[0]);
    if (firstCell.attr('rowspan') === '2' || firstCell.attr('rowspan') === '3') {
      currentAssignmentName = firstCell.text().trim();
    }

    // Process category cells
    cells.each((_, cell) => {
      const $cell = $(cell);
      const bgcolor = $cell.attr('bgcolor');
      if (!bgcolor) return;

      const category = categoryFromBgColor(bgcolor);
      if (!category) return;

      const cellText = $cell.text();

      // Check for "no mark"
      if (/no mark/i.test(cellText)) return;

      // Extract X / Y mark
      const markMatch = cellText.match(/([\d.]+)\s*\/\s*([\d.]+)/);
      if (!markMatch) return;

      const markAchieved = parseFloat(markMatch[1]);
      const markPossible = parseFloat(markMatch[2]);

      // Extract weight
      let weight: number | undefined;
      const weightMatch = cellText.match(/weight=(\d+)/i);
      if (weightMatch) weight = parseFloat(weightMatch[1]);

      // Extract feedback
      let feedback: string | undefined;
      const fbMatch = cellText.match(/feedback:\s*(.+)/i);
      if (fbMatch) feedback = fbMatch[1].trim();

      // Try to extract date from the cell or nearby
      let date: string | undefined;
      const dateMatch = cellText.match(/(\d{4}-\d{2}-\d{2})/);
      if (dateMatch) date = dateMatch[1];

      assignments.push({
        name: currentAssignmentName || 'Unknown',
        date,
        markAchieved,
        markPossible,
        category,
        weight,
        feedback,
        percentage: markPossible > 0 ? (markAchieved / markPossible) * 100 : undefined,
        isMissing: false,
      });
    });
  });

  // Parse weight table
  const weightTable = parseWeightTable($, html);

  const code = existingCode || extractCourseCode(html);
  const overallMark = extractOverallMark(html);

  return buildCourse(code, overallMark, assignments, weightTable, html, false);
}

function parseWeightTable($: cheerio.CheerioAPI, html: string): ReturnType<typeof createWeightTable> {
  const wt = createWeightTable();
  const weightTableEl = $("table[cellpadding='5']");

  if (weightTableEl.length > 0) {
    weightTableEl.find('td').each((_, td) => {
      const bgcolor = $(td).attr('bgcolor');
      const text = $(td).text();
      const cat = categoryFromBgColor(bgcolor || '');
      const pctMatch = text.match(/(\d+)\s*%/);
      if (cat && pctMatch) {
        wt.setWeight(cat, parseFloat(pctMatch[1]));
      }
    });
  } else {
    // Fallback: look for weight text
    const nameMap: Record<string, string> = {
      'knowledge/understanding': 'KU',
      'knowledge': 'KU',
      'thinking': 'T',
      'communication': 'C',
      'application': 'A',
      'final': 'F',
      'culminating': 'F',
      'other': 'O',
    };
    const wtRegex = /(\w+(?:\/\w+)?)\s*[:=]?\s*(\d+)\s*%/gi;
    let m;
    while ((m = wtRegex.exec(html)) !== null) {
      const name = m[1].toLowerCase();
      const weight = parseFloat(m[2]);
      const cat = nameMap[name] || categoryFromName(name);
      if (cat) wt.setWeight(cat, weight);
    }
  }

  return wt;
}

function createFallbackCourse(html: string, existingCode?: string): Course {
  const code = existingCode || extractCourseCode(html);
  return buildCourse(code, 'N/A', [], createWeightTable(), html, true);
}

function buildCourse(
  code: string,
  overallMark: number | string,
  assignments: Assignment[],
  weightTable: ReturnType<typeof createWeightTable>,
  html: string,
  partiallyParsed: boolean
): Course {
  const numMark = typeof overallMark === 'number' ? overallMark : null;

  return {
    code,
    name: code,
    block: 0,
    room: '',
    overallMark,
    assignments,
    weightTable,
    partiallyParsed,
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
