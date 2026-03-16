import { net } from 'electron';
import log from 'electron-log';
import type { Course, Assignment } from '../../renderer/types/course';
import { createWeightTable } from '../../renderer/utils/grade-impact-calculator';
import { parseStudentPage } from '../parsers/student-page-parser';
import { parseCourseDetail } from '../parsers/index';

const TEACHASSIST_BASE = 'https://ta.yrdsb.ca/yrdsb/';

/** Perform an HTTP POST with form data (cookie-aware via session) */
function httpPost(url: string, formData: string): Promise<string> {
  return new Promise((resolve, reject) => {
    const request = net.request({
      url,
      method: 'POST',
    });

    request.setHeader('Content-Type', 'application/x-www-form-urlencoded');

    let data = '';
    request.on('response', (response) => {
      response.on('data', (chunk) => {
        data += chunk.toString();
      });
      response.on('end', () => resolve(data));
      response.on('error', reject);
    });
    request.on('error', reject);
    request.write(formData);
    request.end();
  });
}

/** Perform an HTTP GET (cookie-aware via session) */
function httpGet(url: string): Promise<string> {
  return new Promise((resolve, reject) => {
    const request = net.request(url);

    let data = '';
    request.on('response', (response) => {
      response.on('data', (chunk) => {
        data += chunk.toString();
      });
      response.on('end', () => resolve(data));
      response.on('error', reject);
    });
    request.on('error', reject);
    request.end();
  });
}

/** Login to TeachAssist and return course list */
export async function loginAsync(
  username: string,
  password: string
): Promise<{ success: boolean; courses?: Course[]; error?: string; demo?: boolean }> {
  // Demo mode
  if (username.toLowerCase() === 'demo') {
    return { success: true, courses: getMockCourses(), demo: true };
  }

  try {
    const formData = `username=${encodeURIComponent(username)}&password=${encodeURIComponent(password)}`;
    const html = await httpPost(TEACHASSIST_BASE, formData);

    // Check for login failure
    if (/invalid|failed|incorrect|not found/i.test(html)) {
      return { success: false, error: 'Invalid username or password' };
    }

    const courses = parseStudentPage(html);
    if (courses.length === 0) {
      return { success: false, error: 'No courses found. Please check your credentials.' };
    }

    return { success: true, courses };
  } catch (err) {
    log.error('Login error:', err);
    return { success: false, error: 'Network error. Please check your connection.' };
  }
}

/** Fetch course detail report */
export async function getCourseDetailAsync(reportUrl: string, existingCode?: string): Promise<Course | null> {
  try {
    let html = await httpGet(reportUrl);

    // If response is too small, try fallback URL
    if (html.length < 5000) {
      const fallbackUrl = reportUrl
        .replace('viewReportOE.php', 'viewReport.php')
        .replace('viewReport.php', 'viewReportOE.php');
      if (fallbackUrl !== reportUrl) {
        html = await httpGet(fallbackUrl);
      }
    }

    return parseCourseDetail(html, existingCode);
  } catch (err) {
    log.error(`Failed to fetch course detail: ${reportUrl}`, err);
    return null;
  }
}

/** Mock courses for demo mode */
function getMockCourses(): Course[] {
  const createMock = (
    code: string,
    name: string,
    block: number,
    room: string,
    mark: number,
    assignments: Array<{
      name: string;
      category: string;
      achieved: number;
      possible: number;
      weight: number;
    }>
  ): Course => {
    const weightTable = createWeightTable();
    const asgns: Assignment[] = assignments.map((a) => ({
      name: a.name,
      category: a.category,
      markAchieved: a.achieved,
      markPossible: a.possible,
      weight: a.weight,
      percentage: (a.achieved / a.possible) * 100,
      isMissing: false,
    }));

    // Set weights from assignments
    for (const a of assignments) {
      if (a.weight > 0) {
        weightTable.setWeight(a.category, (weightTable.getWeight(a.category) || 0) + a.weight);
      }
    }

    return {
      code,
      name,
      block,
      room,
      overallMark: mark,
      assignments: asgns,
      weightTable,
      partiallyParsed: false,
      isCGCFormat: false,
      assignmentTrends: [],
      displayMark: mark.toFixed(1),
      hasValidMark: true,
      numericMark: mark,
      gradeColor: getGradeColor(mark),
      gradeLevel: getGradeLevel(mark),
      gradeLetter: getGradeLetter(mark),
    };
  };

  return [
    createMock('ICS4U1-03', 'Computer Science - Grade 12 University', 1, '234', 92, [
      { name: 'Unit 1 Test', category: 'KU', achieved: 48, possible: 50, weight: 10 },
      { name: 'Unit 1 Test', category: 'T', achieved: 22, possible: 25, weight: 0 },
      { name: 'Programming Assignment 1', category: 'A', achieved: 95, possible: 100, weight: 15 },
      { name: 'Programming Assignment 1', category: 'KU', achieved: 28, possible: 30, weight: 0 },
      { name: 'Final Project', category: 'C', achieved: 88, possible: 100, weight: 20 },
      { name: 'Final Project', category: 'A', achieved: 82, possible: 100, weight: 0 },
      { name: 'Final Project', category: 'T', achieved: 25, possible: 30, weight: 0 },
    ]),
    createMock('ENG4U1-01', 'English - Grade 12 University', 2, '312', 87, [
      { name: 'Essay 1', category: 'C', achieved: 82, possible: 100, weight: 10 },
      { name: 'Essay 1', category: 'A', achieved: 90, possible: 100, weight: 0 },
      { name: 'Novel Study Test', category: 'KU', achieved: 90, possible: 100, weight: 15 },
      { name: 'Novel Study Test', category: 'T', achieved: 85, possible: 100, weight: 0 },
      { name: 'Media Presentation', category: 'O', achieved: 88, possible: 100, weight: 10 },
    ]),
    createMock('MHF4U1-02', 'Advanced Functions - Grade 12 University', 3, '401', 78, [
      { name: 'Chapter 1 Quiz', category: 'KU', achieved: 72, possible: 100, weight: 5 },
      { name: 'Chapter 1 Quiz', category: 'A', achieved: 68, possible: 100, weight: 0 },
      { name: 'Midterm', category: 'T', achieved: 80, possible: 100, weight: 20 },
      { name: 'Midterm', category: 'KU', achieved: 75, possible: 100, weight: 0 },
      { name: 'Assignment 2', category: 'A', achieved: 85, possible: 100, weight: 10 },
    ]),
  ];
}

function getGradeColor(mark: number): string {
  if (mark >= 95) return '#2EA043';
  if (mark >= 90) return '#3FB950';
  if (mark >= 85) return '#238636';
  if (mark >= 80) return '#D29922';
  if (mark >= 75) return '#9A6700';
  if (mark >= 70) return '#DB6D28';
  if (mark >= 65) return '#A57104';
  if (mark >= 60) return '#F85149';
  return '#D73A49';
}

function getGradeLevel(mark: number): string {
  if (mark >= 95) return 'Level 4+ (Excellent!)';
  if (mark >= 90) return 'Level 4 (Very Good)';
  if (mark >= 85) return 'Level 4 (Good)';
  if (mark >= 80) return 'Level 3+ (Good)';
  if (mark >= 75) return 'Level 3 (Satisfactory)';
  if (mark >= 70) return 'Level 3 (Adequate)';
  if (mark >= 65) return 'Level 2+ (Passing)';
  if (mark >= 60) return 'Level 2 (Below Average)';
  return 'Level 1 (Below Expectations)';
}

function getGradeLetter(mark: number): string {
  if (mark >= 95) return 'A+';
  if (mark >= 90) return 'A';
  if (mark >= 85) return 'A-';
  if (mark >= 80) return 'B+';
  if (mark >= 75) return 'B';
  if (mark >= 70) return 'B-';
  if (mark >= 65) return 'C+';
  if (mark >= 60) return 'C';
  return 'D';
}
