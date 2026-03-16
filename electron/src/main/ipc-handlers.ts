import { ipcMain, BrowserWindow } from 'electron';
import log from 'electron-log';
import { loginAsync, getCourseDetailAsync } from './services/teachassist-service';
import { saveCredentials, getCredentials, clearCredentials } from './services/credential-service';
import { exportCsv, exportHtmlReport } from './services/export-service';
import type { Course } from '../renderer/types/course';
import Store from 'electron-store';

const store = new Store({ name: 'settings' });

// In-memory course cache
let cachedCourses: Course[] = [];
let lastUsername = '';
let lastPassword = '';

export function registerIpcHandlers() {
  // Window controls
  ipcMain.handle('window:minimize', (event) => {
    BrowserWindow.fromWebContents(event.sender)?.minimize();
  });

  ipcMain.handle('window:maximize', (event) => {
    BrowserWindow.fromWebContents(event.sender)?.maximize();
  });

  ipcMain.handle('window:close', (event) => {
    BrowserWindow.fromWebContents(event.sender)?.close();
  });

  ipcMain.handle('window:isMaximized', (event) => {
    return BrowserWindow.fromWebContents(event.sender)?.isMaximized() ?? false;
  });

  ipcMain.on('window:trackMaximized', (event) => {
    const win = BrowserWindow.fromWebContents(event.sender);
    win?.on('maximize', () => event.sender.send('window:maximizedChanged', true));
    win?.on('unmaximize', () => event.sender.send('window:maximizedChanged', false));
  });

  // Auth
  ipcMain.handle('auth:login', async (_event, username: string, password: string, remember: boolean) => {
    log.info(`Login attempt for: ${username}`);
    const result = await loginAsync(username, password);

    if (result.success && result.courses) {
      lastUsername = username;
      lastPassword = password;
      cachedCourses = result.courses;

      if (remember && !result.demo) {
        await saveCredentials(username, password);
      }
    }

    return result;
  });

  ipcMain.handle('auth:logout', async () => {
    log.info('Logout');
    cachedCourses = [];
    lastUsername = '';
    lastPassword = '';
    return { success: true };
  });

  ipcMain.handle('auth:getSavedCredentials', async () => {
    return getCredentials();
  });

  ipcMain.handle('auth:clearCredentials', async () => {
    await clearCredentials();
    return { success: true };
  });

  // Courses
  ipcMain.handle('courses:getAll', async () => {
    return cachedCourses;
  });

  ipcMain.handle('courses:getDetail', async (_event, reportUrl: string, courseCode?: string) => {
    log.info(`Fetching course detail: ${reportUrl}`);
    const course = await getCourseDetailAsync(reportUrl, courseCode);
    return course ? { success: true, course } : { success: false, error: 'Failed to fetch course detail' };
  });

  ipcMain.handle('courses:refresh', async () => {
    if (!lastUsername) return [];
    const result = await loginAsync(lastUsername, lastPassword);
    if (result.success && result.courses) {
      cachedCourses = result.courses;
      return result.courses;
    }
    return [];
  });

  // Settings
  ipcMain.handle('settings:get', async () => {
    const theme = (store.get('theme', 'dark') as string);
    const autoRefresh = (store.get('autoRefresh', false) as boolean);
    return { theme, autoRefresh };
  });

  ipcMain.handle('settings:set', async (_event, settings: Record<string, unknown>) => {
    for (const [key, value] of Object.entries(settings)) {
      store.set(key, value);
    }
    log.info('Settings updated', settings);
    return { success: true };
  });

  // Export
  ipcMain.handle('export:csv', async (_event, courses: Course[]) => {
    return exportCsv(courses);
  });

  ipcMain.handle('export:htmlReport', async (_event, courses: Course[]) => {
    return exportHtmlReport(courses);
  });

  // Theme
  ipcMain.handle('theme:get', async () => {
    return store.get('theme', 'dark') as string;
  });

  ipcMain.handle('theme:set', async (_event, theme: string) => {
    store.set('theme', theme);
    return { success: true };
  });
}
