import { contextBridge, ipcRenderer } from 'electron';

const api = {
  // Window controls
  minimize: () => ipcRenderer.invoke('window:minimize'),
  maximize: () => ipcRenderer.invoke('window:maximize'),
  close: () => ipcRenderer.invoke('window:close'),
  isMaximized: () => ipcRenderer.invoke('window:isMaximized'),
  onMaximizedChange: (callback: (maximized: boolean) => void) => {
    const handler = (_event: Electron.IpcRendererEvent, maximized: boolean) => callback(maximized);
    ipcRenderer.on('window:maximizedChanged', handler);
    return () => ipcRenderer.removeListener('window:maximizedChanged', handler);
  },

  // Auth
  login: (username: string, password: string, remember: boolean) =>
    ipcRenderer.invoke('auth:login', username, password, remember),
  logout: () => ipcRenderer.invoke('auth:logout'),
  getSavedCredentials: () => ipcRenderer.invoke('auth:getSavedCredentials'),
  clearCredentials: () => ipcRenderer.invoke('auth:clearCredentials'),

  // Courses
  getCourses: () => ipcRenderer.invoke('courses:getAll'),
  getCourseDetail: (reportUrl: string) =>
    ipcRenderer.invoke('courses:getDetail', reportUrl),
  refreshCourses: () => ipcRenderer.invoke('courses:refresh'),

  // Settings
  getSettings: () => ipcRenderer.invoke('settings:get'),
  setSettings: (settings: Record<string, unknown>) =>
    ipcRenderer.invoke('settings:set', settings),

  // Export
  exportCsv: (courses: unknown[]) => ipcRenderer.invoke('export:csv', courses),
  exportHtmlReport: (courses: unknown[]) =>
    ipcRenderer.invoke('export:htmlReport', courses),

  // Theme
  getTheme: () => ipcRenderer.invoke('theme:get'),
  setTheme: (theme: string) => ipcRenderer.invoke('theme:set'),
  onThemeChange: (callback: (theme: string) => void) => {
    const handler = (_event: Electron.IpcRendererEvent, theme: string) => callback(theme);
    ipcRenderer.on('theme:changed', handler);
    return () => ipcRenderer.removeListener('theme:changed', handler);
  },
};

contextBridge.exposeInMainWorld('electronAPI', api);
