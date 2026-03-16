import * as cheerio from 'cheerio';

/** Map bgcolor to category code */
export function categoryFromBgColor(bgcolor: string | undefined): string {
  if (!bgcolor) return '';
  const bg = bgcolor.toLowerCase().replace('#', '');
  const map: Record<string, string> = {
    'ffffaa': 'KU',
    'c0fea4': 'T',
    'afafff': 'C',
    'ffd490': 'A',
    'eeeeee': 'O',
    'dedede': 'F',
    'cccccc': 'F',
  };
  return map[bg] || '';
}

/** Map category name text to code */
export function categoryFromName(name: string): string {
  const n = name.toLowerCase().trim();
  if (n.includes('knowledge') || n.includes('understanding')) return 'KU';
  if (n.includes('thinking')) return 'T';
  if (n.includes('communication')) return 'C';
  if (n.includes('application')) return 'A';
  if (n.includes('final') || n.includes('culminating') || n.includes('exam')) return 'F';
  return 'O';
}

/** Extract course code from h2 tags */
export function extractCourseCode(html: string): string {
  const $ = cheerio.load(html);
  const codeRegex = /([A-Z]{2,5}\d?[A-Z]*\d*-\d+)/;
  for (const el of $('h2').toArray()) {
    const text = $(el).text();
    const match = text.match(codeRegex);
    if (match) return match[1];
  }
  return '';
}

/** Extract overall mark from HTML */
export function extractOverallMark(html: string): number | string {
  // Pattern 1: font-size:64pt
  let match = html.match(/font-size:64pt[^>]*>\s*([\d.]+)%/);
  if (match) return parseFloat(match[1]);

  // Pattern 2: overall/final/course mark <b>XX%</b>
  match = html.match(
    /(?:overall|final|course)\s*(?:mark)?\s*:?\s*<b>\s*(\d{1,3}\.?\d*)\s*%<\/b>/i
  );
  if (match) return parseFloat(match[1]);

  // Pattern 3: large font mark
  match = html.match(/<font\s+size=['""]?[45678]['""]?[^>]*>\s*(\d{1,3}\.?\d*)\s*%/i);
  if (match) return parseFloat(match[1]);

  // Pattern 4: OE format "Term Work {92} = Level 4+"
  match = html.match(/Term Work\s*\{(\d+)\}\s*=\s*Level\s*(\d)([+-])?/i);
  if (match) {
    const pct = parseInt(match[1], 10);
    const level = parseInt(match[2], 10);
    let pctFromLevel: number;
    switch (level) {
      case 4: pctFromLevel = 83; break;
      case 3: pctFromLevel = 73; break;
      case 2: pctFromLevel = 63; break;
      case 1: pctFromLevel = 53; break;
      default: pctFromLevel = pct; break;
    }
    if (match[3] === '+') pctFromLevel += 7;
    if (match[3] === '-') pctFromLevel -= 5;
    return pct;
  }

  return 'N/A';
}

/** Parse level mark "Level 4+" to percentage */
export function parseLevelMark(text: string): number | null {
  const match = text.match(/Level\s*(\d)([+-])?/i);
  if (!match) return null;
  const level = parseInt(match[1], 10);
  let pct: number;
  switch (level) {
    case 4: pct = 83; break;
    case 3: pct = 73; break;
    case 2: pct = 63; break;
    case 1: pct = 53; break;
    default: return null;
  }
  if (match[2] === '+') pct += 7;
  if (match[2] === '-') pct -= 5;
  return pct;
}

/** Balanced brace matching for Chart.js parsing */
export function findBalancedBrace(text: string, startIndex: number): number {
  let depth = 0;
  for (let i = startIndex; i < text.length; i++) {
    if (text[i] === '{') depth++;
    if (text[i] === '}') {
      depth--;
      if (depth === 0) return i;
    }
  }
  return -1;
}

/** Balanced bracket matching */
export function findBalancedBracket(text: string, startIndex: number): number {
  let depth = 0;
  for (let i = startIndex; i < text.length; i++) {
    if (text[i] === '[') depth++;
    if (text[i] === ']') {
      depth--;
      if (depth === 0) return i;
    }
  }
  return -1;
}
