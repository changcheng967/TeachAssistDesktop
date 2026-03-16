const SubjectCodes: Record<string, string> = {
  ADA: 'Drama',
  AMU: 'Music',
  AVI: 'Visual Arts',
  BAF: 'Financial Accounting',
  BAT: 'Accounting',
  BBI: 'Business',
  CGC: 'Geography of Canada',
  CHC: 'Canadian History',
  CHV: 'Civics',
  CHW: 'World History',
  CIA: 'Analyzing Current Issues',
  CLN: 'Law',
  ENG: 'English',
  ENL: 'English',
  ESL: 'English as a Second Language',
  FSF: 'Core French',
  FIF: 'French Immersion',
  GLC: 'Career Studies',
  GLS: 'Learning Strategies',
  GPP: 'Leadership',
  HFA: 'Food & Nutrition',
  HFN: 'Food & Nutrition',
  HHS: 'Human Services',
  HIF: 'Individual & Family',
  HIP: 'Psychology',
  HRE: 'Religion',
  HSB: 'Social Sciences',
  HSP: 'Anthropology & Sociology',
  ICS: 'Computer Science',
  ICD: 'Computer Science',
  MCR: 'Functions',
  MCT: 'Mathematics',
  MCV: 'Calculus & Vectors',
  MDM: 'Data Management',
  MEL: 'Mathematics for Work',
  MFM: 'Foundations of Mathematics',
  MHF: 'Advanced Functions',
  MPM: 'Principles of Mathematics',
  MTH: 'Mathematics',
  NBE: 'Indigenous Studies',
  OLC: 'Ontario Literacy Course',
  PPL: 'Healthy Active Living',
  PAD: 'Outdoor Activities',
  PAF: 'Personal Fitness',
  PAI: 'Physical Activities',
  PSK: 'Introductory Kinesiology',
  SBI: 'Biology',
  SCH: 'Chemistry',
  SES: 'Earth & Space Science',
  SNC: 'Science',
  SPH: 'Physics',
  SVN: 'Environmental Science',
  TEJ: 'Computer Engineering',
  TDJ: 'Technological Design',
  TIK: 'Computer Technology',
  TGJ: 'Communications Technology',
  TMJ: 'Manufacturing Technology',
  TTJ: 'Transportation Technology',
  TWJ: 'Construction Technology',
};

const Pathways: Record<string, string> = {
  D: 'Academic',
  P: 'Applied',
  O: 'Open',
  U: 'University',
  M: 'University/College',
  C: 'College',
  E: 'Workplace',
  W: 'Destreamed',
  L: 'Locally Developed',
};

export interface ParsedCourse {
  subjectName: string;
  gradeLevel: string;
  pathway: string;
}

export function parseCourseCode(courseCode: string): ParsedCourse {
  if (!courseCode || courseCode.length < 5) {
    return { subjectName: courseCode || '', gradeLevel: '', pathway: '' };
  }

  // ESL special handling
  if (courseCode.startsWith('ESL') && courseCode.length >= 5) {
    const eslLevel = courseCode[3];
    return { subjectName: 'English as a Second Language', gradeLevel: `Level ${eslLevel}`, pathway: 'Open' };
  }

  const subjectCode = courseCode.substring(0, 3);
  let gradeLevel = '';
  if (courseCode.length >= 4 && /\d/.test(courseCode[3])) {
    const gradeNum = parseInt(courseCode[3], 10);
    gradeLevel = `Grade ${gradeNum + 8}`;
  }

  let pathway = '';
  if (courseCode.length >= 5 && Pathways[courseCode[4]]) {
    pathway = Pathways[courseCode[4]];
  }

  const subjectName = SubjectCodes[subjectCode] || subjectCode;
  return { subjectName, gradeLevel, pathway };
}

export function getCourseDisplayText(courseCode: string): string {
  const { subjectName, gradeLevel, pathway } = parseCourseCode(courseCode);

  if (courseCode.startsWith('ESL') && courseCode.length >= 5) {
    const eslLevel = courseCode[3];
    return `ESL \u2022 Level ${eslLevel}`;
  }

  if (!gradeLevel && !pathway) return subjectName;
  if (gradeLevel && pathway) return `${subjectName} \u2022 ${gradeLevel} ${pathway}`;
  return `${subjectName} \u2022 ${gradeLevel}${pathway}`;
}
