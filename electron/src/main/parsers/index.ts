import type { Course } from '../../renderer/types/course';
import { parseStandardCourseDetail } from './standard-parser';
import { parseOECourseDetail } from './oe-parser';
import { parseFallbackCourseDetail } from './fallback-parser';

export function detectTemplate(html: string): 'standard' | 'oe' | 'fallback' {
  const isOe =
    html.includes('By Overall Expectation') ||
    html.includes('myChart') ||
    (html.includes('Assessment Tasks') && html.includes('Expectation'));

  const hasCategoryColors =
    html.includes('#ffffaa') ||
    html.includes('#c0fea4') ||
    html.includes('#afafff') ||
    html.includes('#ffd490') ||
    html.includes('ffffaa') ||
    html.includes('c0fea4') ||
    html.includes('afafff') ||
    html.includes('ffd490') ||
    /bgcolor\s*=\s*["']?(?:ffffaa|c0fea4|afafff|ffd490)/i.test(html);

  const hasStandardTable =
    html.includes('cellpadding="3"') && html.includes('cellspacing="0"');

  if (isOe && !hasCategoryColors) return 'oe';
  if (hasCategoryColors || hasStandardTable) return 'standard';
  if (isOe) return 'oe';
  return 'fallback';
}

export function parseCourseDetail(
  html: string,
  existingCode?: string
): Course {
  const template = detectTemplate(html);

  switch (template) {
    case 'standard':
      return parseStandardCourseDetail(html, existingCode);
    case 'oe':
      return parseOECourseDetail(html, existingCode);
    case 'fallback':
      return parseFallbackCourseDetail(html, existingCode);
  }
}
