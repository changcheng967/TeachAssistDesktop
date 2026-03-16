import { useTheme } from '../../hooks/useTheme';
import { useAuth } from '../../hooks/useAuth';
import { useCourseStore } from '../../state/course-store';

export default function SettingsPage() {
  const { theme, setTheme } = useTheme();
  const { logout } = useAuth();
  const courses = useCourseStore((s) => s.courses);

  const handleExportCsv = async () => {
    const result = await window.electronAPI.exportCsv(courses);
    if (!result.success) {
      alert(result.error || 'Export failed');
    }
  };

  const handleExportHtml = async () => {
    const result = await window.electronAPI.exportHtmlReport(courses);
    if (!result.success) {
      alert(result.error || 'Export failed');
    }
  };

  return (
    <div className="space-y-6 animate-fade-in max-w-2xl">
      <div>
        <h1 className="text-xl font-semibold">Settings</h1>
        <p className="text-sm text-github-text-secondary mt-0.5">Configure your preferences</p>
      </div>

      {/* Appearance */}
      <div className="card p-4">
        <h2 className="text-sm font-semibold text-github-text-secondary uppercase tracking-wide mb-4">
          Appearance
        </h2>
        <div className="flex items-center justify-between">
          <div>
            <div className="text-sm font-medium">Dark Mode</div>
            <div className="text-xs text-github-text-muted mt-0.5">Toggle between dark and light theme</div>
          </div>
          <button
            onClick={() => setTheme(theme === 'dark' ? 'light' : 'dark')}
            className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors ${
              theme === 'dark' ? 'bg-github-accent' : 'bg-github-border'
            }`}
          >
            <span
              className={`inline-block h-4 w-4 transform rounded-full bg-white transition-transform ${
                theme === 'dark' ? 'translate-x-6' : 'translate-x-1'
              }`}
            />
          </button>
        </div>
      </div>

      {/* Export */}
      <div className="card p-4">
        <h2 className="text-sm font-semibold text-github-text-secondary uppercase tracking-wide mb-4">
          Export
        </h2>
        <div className="space-y-3">
          <button
            onClick={handleExportCsv}
            className="w-full flex items-center justify-between px-4 py-3 rounded-md border border-github-border
                       hover:bg-github-border/30 transition-colors text-left"
          >
            <div>
              <div className="text-sm font-medium">Export as CSV</div>
              <div className="text-xs text-github-text-muted">Download a spreadsheet of your grades</div>
            </div>
            <svg className="w-4 h-4 text-github-text-muted" viewBox="0 0 20 20" fill="currentColor">
              <path d="M10.75 2.75a.75.75 0 00-1.5 0v8.614L6.295 8.235a.75.75 0 10-1.09 1.03l4.25 4.5a.75.75 0 001.09 0l4.25-4.5a.75.75 0 00-1.09-1.03l-2.955 3.129V2.75z" />
              <path d="M3.5 12.75a.75.75 0 00-1.5 0v2.5A2.75 2.75 0 004.75 18h10.5A2.75 2.75 0 0018 15.25v-2.5a.75.75 0 00-1.5 0v2.5c0 .69-.56 1.25-1.25 1.25H4.75c-.69 0-1.25-.56-1.25-1.25v-2.5z" />
            </svg>
          </button>
          <button
            onClick={handleExportHtml}
            className="w-full flex items-center justify-between px-4 py-3 rounded-md border border-github-border
                       hover:bg-github-border/30 transition-colors text-left"
          >
            <div>
              <div className="text-sm font-medium">Export HTML Report</div>
              <div className="text-xs text-github-text-muted">Generate a printable grade report</div>
            </div>
            <svg className="w-4 h-4 text-github-text-muted" viewBox="0 0 20 20" fill="currentColor">
              <path d="M10.75 2.75a.75.75 0 00-1.5 0v8.614L6.295 8.235a.75.75 0 10-1.09 1.03l4.25 4.5a.75.75 0 001.09 0l4.25-4.5a.75.75 0 00-1.09-1.03l-2.955 3.129V2.75z" />
              <path d="M3.5 12.75a.75.75 0 00-1.5 0v2.5A2.75 2.75 0 004.75 18h10.5A2.75 2.75 0 0018 15.25v-2.5a.75.75 0 00-1.5 0v2.5c0 .69-.56 1.25-1.25 1.25H4.75c-.69 0-1.25-.56-1.25-1.25v-2.5z" />
            </svg>
          </button>
        </div>
      </div>

      {/* Account */}
      <div className="card p-4">
        <h2 className="text-sm font-semibold text-github-text-secondary uppercase tracking-wide mb-4">
          Account
        </h2>
        <button
          onClick={logout}
          className="text-sm text-github-danger hover:text-github-danger-emphasis font-medium transition-colors"
        >
          Sign Out
        </button>
      </div>

      {/* About */}
      <div className="card p-4">
        <h2 className="text-sm font-semibold text-github-text-secondary uppercase tracking-wide mb-4">
          About
        </h2>
        <div className="space-y-2 text-sm">
          <div className="flex justify-between">
            <span className="text-github-text-secondary">Version</span>
            <span className="font-mono">4.0.0</span>
          </div>
          <div className="flex justify-between">
            <span className="text-github-text-secondary">Stack</span>
            <span>Electron + React + TypeScript</span>
          </div>
          <div className="flex justify-between">
            <span className="text-github-text-secondary">Courses loaded</span>
            <span>{courses.length}</span>
          </div>
        </div>
      </div>
    </div>
  );
}
