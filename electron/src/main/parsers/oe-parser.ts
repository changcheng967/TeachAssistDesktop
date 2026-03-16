import * as cheerio from 'cheerio';
import type { Course, Assignment, AssignmentTrend } from '../../renderer/types/course';
import { createWeightTable } from '../../renderer/utils/grade-impact-calculator';
import { extractOverallMark, extractCourseCode, findBalancedBrace, findBalancedBracket } from './helpers';

export function parseOECourseDetail(html: string, existingCode?: string): Course {
  const assignments: Assignment[] = [];
  const trends: AssignmentTrend[] = [];

  // Try Chart.js data first
  const chartAssignments = parseOEChartJsData(html);
  if (chartAssignments.length > 0) {
    assignments.push(...chartAssignments);
  } else {
    // Fallback to table-based OE parsing
    const tableAssignments = parseOETables(html);
    assignments.push(...tableAssignments);
  }

  // Build trends from assignments
  for (const a of assignments) {
    if (a.markAchieved != null && a.markPossible != null && a.percentage != null) {
      trends.push({
        assignmentName: a.name,
        mark: a.percentage,
        weight: a.weight || 0,
        expectation: a.category,
        type: 'Product', // Default
      });
    }
  }

  const weightTable = parseOEWeightTable(html);
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
    weightTable,
    partiallyParsed: false,
    isCGCFormat: true,
    assignmentTrends: trends,
    displayMark: typeof overallMark === 'number' ? overallMark.toFixed(1) : 'N/A',
    hasValidMark: numMark !== null,
    numericMark: numMark,
    gradeColor: getGradeColor(overallMark),
    gradeLevel: getGradeLevel(overallMark),
    gradeLetter: getGradeLetter(overallMark),
  };
}

/** Parse Chart.js bubble chart data */
function parseOEChartJsData(html: string): Assignment[] {
  const assignments: Assignment[] = [];
  const chartInstances = html.match(/new Chart\(ctx,\s*\{/g);
  if (!chartInstances) return assignments;

  let searchFrom = 0;
  for (const _ of chartInstances) {
    const chartStart = html.indexOf('new Chart(ctx, {', searchFrom);
    if (chartStart === -1) break;

    const braceStart = html.indexOf('{', chartStart + 'new Chart(ctx, '.length);
    if (braceStart === -1) break;

    const braceEnd = findBalancedBrace(html, braceStart);
    if (braceEnd === -1) break;

    const chartBody = html.substring(braceStart, braceEnd + 1);

    // Extract expectation code from chart title
    const titleMatch = chartBody.match(/text:\s*'[^']*([A-Z]\d)\.?\s*'/);
    if (!titleMatch) {
      searchFrom = braceEnd + 1;
      continue;
    }
    const expectation = titleMatch[1];

    // Extract datasets
    const datasetsMatch = chartBody.match(/datasets:\s*\[/);
    if (!datasetsMatch) {
      searchFrom = braceEnd + 1;
      continue;
    }

    const datasetsStart = chartBody.indexOf('[', datasetsMatch.index! + 'datasets:'.length);
    const datasetsEnd = findBalancedBracket(chartBody, datasetsStart);
    if (datasetsEnd === -1) {
      searchFrom = braceEnd + 1;
      continue;
    }

    const datasetsStr = chartBody.substring(datasetsStart, datasetsEnd + 1);

    // Parse each dataset
    const labelMatch = datasetsStr.match(/label:\s*'([^']+)'/);
    const label = labelMatch ? labelMatch[1] : 'Product';

    // Extract labels array
    const labelsMatch = datasetsStr.match(/labels:\s*\[/);
    let labels: string[] = [];
    if (labelsMatch) {
      const labelsStart = datasetsStr.indexOf('[', labelsMatch.index!);
      const labelsEnd = findBalancedBracket(datasetsStr, labelsStart);
      if (labelsEnd !== -1) {
        const labelsStr = datasetsStr.substring(labelsStart + 1, labelsEnd);
        labels = labelsStr.match(/'([^']+)'/g)?.map((s) => s.replace(/'/g, '')) || [];
      }
    }

    // Extract data points {x, y, r}
    const dataMatch = datasetsStr.match(/data:\s*\[/);
    if (!dataMatch) {
      searchFrom = braceEnd + 1;
      continue;
    }

    const dataStart = datasetsStr.indexOf('[', dataMatch.index! + 'data:'.length);
    const dataEnd = findBalancedBracket(datasetsStr, dataStart);
    if (dataEnd === -1) {
      searchFrom = braceEnd + 1;
      continue;
    }

    const dataStr = datasetsStr.substring(dataStart, dataEnd + 1);
    const pointRegex = /\{x:\s*([\d.]+),\s*y:\s*([\d.]+),\s*r:\s*([\d.]+)\}/g;
    let pointMatch;
    let pointIndex = 0;

    while ((pointMatch = pointRegex.exec(dataStr)) !== null) {
      const x = parseFloat(pointMatch[1]);
      const y = parseFloat(pointMatch[2]);

      assignments.push({
        name: labels[pointIndex] || `Task ${pointIndex + 1}`,
        category: expectation,
        markAchieved: y,
        markPossible: 100,
        percentage: y,
        weight: undefined,
        isMissing: false,
      });

      pointIndex++;
    }

    searchFrom = braceEnd + 1;
  }

  return assignments;
}

/** Parse OE table-based layout */
function parseOETables(html: string): Assignment[] {
  const $ = cheerio.load(html);
  const assignments: Assignment[] = [];

  // Find sections by h2 headings
  const headings = $('h2');
  let currentSection = '';

  headings.each((_i: number, heading: any) => {
    currentSection = $(heading).text().trim();

    // Walk through subsequent siblings until next h2
    let sibling = $(heading).next();
    while (sibling.length > 0 && sibling.prop('tagName')?.toUpperCase() !== 'H2') {
      if (sibling.prop('tagName')?.toUpperCase() === 'TABLE') {
        parseOETableRows($, sibling, assignments, currentSection);
      }
      sibling = sibling.next();
    }
  });

  // If no h2-based sections found, try all tables
  if (assignments.length === 0) {
    $('table').each((_i: number, table: any) => {
      parseOETableRows($, $(table), assignments, '');
    });
  }

  return assignments;
}

function parseOETableRows(
  $: cheerio.CheerioAPI,
  table: cheerio.Cheerio<any>,
  assignments: Assignment[],
  section: string
) {
  const rows = table.find('tr');

  rows.each((_, row) => {
    const cells = $(row).find('td');
    if (cells.length < 2) return;

    let taskName = '';
    let expectation = '';
    let markAchieved: number | undefined;
    let markPossible: number | undefined;
    let weight: number | undefined;

    // Determine column layout
    if (cells.length >= 5) {
      // 5-col: Task | Expectation | Mark | OutOf | Weight
      taskName = $(cells[0]).text().trim();
      expectation = $(cells[1]).text().trim();
      const markText = $(cells[2]).text().trim();
      const outOfText = $(cells[3]).text().trim();
      const weightText = $(cells[4]).text().trim();

      const fracMatch = markText.match(/([\d.]+)/);
      const outMatch = outOfText.match(/([\d.]+)/);
      if (fracMatch) markAchieved = parseFloat(fracMatch[1]);
      if (outMatch) markPossible = parseFloat(outMatch[1]);
      const wtMatch = weightText.match(/([\d.]+)/);
      if (wtMatch) weight = parseFloat(wtMatch[1]);
    } else if (cells.length === 4) {
      // 4-col: Task | Mark/OutOf | Expectation | Weight
      taskName = $(cells[0]).text().trim();
      const markCell = $(cells[1]).text().trim();
      expectation = $(cells[2]).text().trim();
      const weightText = $(cells[3]).text().trim();

      const fracMatch = markCell.match(/([\d.]+)\s*\/\s*([\d.]+)/);
      if (fracMatch) {
        markAchieved = parseFloat(fracMatch[1]);
        markPossible = parseFloat(fracMatch[2]);
      } else {
        const numMatch = markCell.match(/([\d.]+)/);
        if (numMatch) markAchieved = parseFloat(numMatch[1]);
      }

      const wtMatch = weightText.match(/([\d.]+)/);
      if (wtMatch) weight = parseFloat(wtMatch[1]);
    } else if (cells.length === 3) {
      // 3-col: Task | Mark | Weight or Task | Expectation | Mark
      taskName = $(cells[0]).text().trim();
      const cell1 = $(cells[1]).text().trim();
      const cell2 = $(cells[2]).text().trim();

      // Check if cell1 is expectation code
      if (/^[A-Z]\d$/i.test(cell1)) {
        expectation = cell1;
        const markMatch = cell2.match(/([\d.]+)\s*\/\s*([\d.]+)/);
        if (markMatch) {
          markAchieved = parseFloat(markMatch[1]);
          markPossible = parseFloat(markMatch[2]);
        }
      } else {
        const fracMatch = cell1.match(/([\d.]+)\s*\/\s*([\d.]+)/);
        if (fracMatch) {
          markAchieved = parseFloat(fracMatch[1]);
          markPossible = parseFloat(fracMatch[2]);
        }
        const wtMatch = cell2.match(/([\d.]+)/);
        if (wtMatch) weight = parseFloat(wtMatch[1]);
      }
    } else if (cells.length === 2) {
      taskName = $(cells[0]).text().trim();
      const markCell = $(cells[1]).text().trim();
      const fracMatch = markCell.match(/([\d.]+)\s*\/\s*([\d.]+)/);
      if (fracMatch) {
        markAchieved = parseFloat(fracMatch[1]);
        markPossible = parseFloat(fracMatch[2]);
      }
    }

    // Validate expectation code
    if (!/^[A-Z]\d$/i.test(expectation)) expectation = '';

    // Skip rows without task name or mark
    if (!taskName || (markAchieved == null && markPossible == null)) return;
    if (markPossible === 0) return;

    assignments.push({
      name: taskName,
      category: expectation || 'O',
      markAchieved,
      markPossible,
      percentage:
        markAchieved != null && markPossible != null && markPossible > 0
          ? (markAchieved / markPossible) * 100
          : undefined,
      weight,
      isMissing: false,
    });
  });
}

/** Parse OE weight table */
function parseOEWeightTable(html: string): ReturnType<typeof createWeightTable> {
  const wt = createWeightTable();

  // Look for weight patterns in OE format
  const patterns = [
    /Knowledge.*?(\d+)\s*%/i,
    /Thinking.*?(\d+)\s*%/i,
    /Communication.*?(\d+)\s*%/i,
    /Application.*?(\d+)\s*%/i,
    /Product.*?(\d+)\s*%/i,
    /Conversation.*?(\d+)\s*%/i,
    /Observation.*?(\d+)\s*%/i,
  ];

  const catMap: Record<string, string> = {
    Knowledge: 'KU',
    Thinking: 'T',
    Communication: 'C',
    Application: 'A',
    Product: 'O',
    Conversation: 'O',
    Observation: 'O',
  };

  for (const pattern of patterns) {
    const match = html.match(pattern);
    if (match) {
      const key = pattern.source.split('.')[0].replace('\\', '');
      // Simple: extract the word from the pattern
      for (const [word, cat] of Object.entries(catMap)) {
        if (pattern.source.toLowerCase().includes(word.toLowerCase())) {
          wt.setWeight(cat, parseFloat(match[1]));
          break;
        }
      }
    }
  }

  return wt;
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
