import type { Course } from './course';

export interface ElectronAPI {
  // Window controls
  minimize: () => Promise<void>;
  maximize: () => Promise<void>;
  close: () => Promise<void>;
  isMaximized: () => Promise<boolean>;
  onMaximizedChange: (callback: (maximized: boolean) => void) => () => void;

  // Auth
  login: (username: string, password: string, remember: boolean) => Promise<LoginResult>;
  logout: () => Promise<{ success: boolean }>;
  getSavedCredentials: () => Promise<{ username: string | null; password: string | null }>;
  clearCredentials: () => Promise<{ success: boolean }>;

  // Courses
  getCourses: () => Promise<Course[]>;
  getCourseDetail: (reportUrl: string) => Promise<CourseDetailResult>;
  refreshCourses: () => Promise<Course[]>;

  // Settings
  getSettings: () => Promise<AppSettings>;
  setSettings: (settings: Partial<AppSettings>) => Promise<{ success: boolean }>;

  // Export
  exportCsv: (courses: Course[]) => Promise<{ success: boolean; error?: string }>;
  exportHtmlReport: (courses: Course[]) => Promise<{ success: boolean; error?: string }>;

  // Theme
  getTheme: () => Promise<string>;
  setTheme: (theme: string) => Promise<{ success: boolean }>;
  onThemeChange: (callback: (theme: string) => void) => () => void;
}

interface LoginResult {
  success: boolean;
  demo?: boolean;
  username?: string;
  error?: string;
  courses?: Course[];
}

interface CourseDetailResult {
  success: boolean;
  course?: Course;
  error?: string;
}

export interface AppSettings {
  theme: 'dark' | 'light';
  autoRefresh: boolean;
}

declare global {
  interface Window {
    electronAPI: ElectronAPI;
  }
}
