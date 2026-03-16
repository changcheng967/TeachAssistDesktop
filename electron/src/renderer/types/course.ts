export interface Assignment {
  name: string;
  date?: string;
  markAchieved?: number;
  markPossible?: number;
  category: string;
  weight?: number;
  feedback?: string;
  percentage?: number;
  isMissing: boolean;
}

export interface AssignmentGroup {
  name: string;
  assignments: Assignment[];
  gradeColor: string;
  impact?: AssignmentImpact;
}

export interface WeightTable {
  weights: Record<string, number>;
  getWeight: (category: string) => number | undefined;
  setWeight: (category: string, weight: number) => void;
}

export interface AssignmentTrend {
  assignmentName: string;
  mark: number;
  weight: number;
  expectation: string;
  type: string;
}

export interface GradeTimelinePoint {
  index: number;
  assignmentName: string;
  date?: string;
  cumulativeGrade: number;
  impact: number;
  isHighImpact: boolean;
  firstPoint: boolean;
}

export interface AssignmentImpact {
  assignmentName: string;
  impactDelta: number;
  isPositive: boolean;
  isHighImpact: boolean;
  cumulativeAfter: number;
  cumulativeBefore: number;
  displayImpact: string;
  impactColor: string;
}

export interface Course {
  code: string;
  name: string;
  block: number;
  room: string;
  startTime?: string;
  endTime?: string;
  overallMark: number | string;
  assignments: Assignment[];
  weightTable: WeightTable;
  subjectId?: string;
  studentId?: string;
  reportUrl?: string;
  partiallyParsed: boolean;
  isCGCFormat: boolean;
  assignmentTrends: AssignmentTrend[];

  displayMark: string;
  hasValidMark: boolean;
  numericMark: number | null;
  gradeColor: string;
  gradeLevel: string;
  gradeLetter: string;
}

export interface CategoryPerformance {
  code: string;
  name: string;
  percentage: number;
  weight: number;
  assignmentCount: number;
  gradeColor: string;
}

export interface CourseTrendItem {
  code: string;
  name: string;
  mark: number;
  gradeColor: string;
}

export interface HypotheticalAssignment {
  name: string;
  mark: number;
  weight: number;
}

export interface PresetGoal {
  percent: number;
  label: string;
  color: string;
}
