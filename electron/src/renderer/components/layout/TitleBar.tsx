import { useEffect, useState } from 'react';

export default function TitleBar() {
  const [isMaximized, setIsMaximized] = useState(false);

  useEffect(() => {
    window.electronAPI.onMaximizedChange(setIsMaximized);
    window.electronAPI.isMaximized().then(setIsMaximized);
  }, []);

  return (
    <div className="drag-region h-9 flex items-center justify-between px-3 bg-github-bg border-b border-github-border select-none shrink-0">
      <div className="flex items-center gap-2 text-xs text-github-text-muted">
        <svg className="w-4 h-4" viewBox="0 0 16 16" fill="currentColor">
          <path d="M8 0a8 8 0 1 1 0 16A8 8 0 0 1 8 0ZM1.5 8a6.5 6.5 0 1 0 13 0 6.5 6.5 0 0 0-13 0Zm4.879-2.773.05-.05a.75.75 0 0 0-1.06-1.06l-.05.05a2.122 2.122 0 0 0 0 3 2.122 2.122 0 0 0 0 3l.05.05a.75.75 0 1 0 1.06-1.06l-.05-.05a.622.622 0 0 1 0-.88l.05-.05a.622.622 0 0 1 0-.88Zm3.75 0a.75.75 0 0 1 1.06 0l.05.05a2.122 2.122 0 0 1 0 3 2.122 2.122 0 0 1 0 3l-.05.05a.75.75 0 1 1-1.06-1.06l.05-.05a.622.622 0 0 0 0-.88l-.05-.05a.622.622 0 0 1 0-.88l-.05-.05Zm-3.28 3.28a.75.75 0 0 0-1.06 0l-.05.05a.75.75 0 0 0 1.06 1.06l.05-.05a.75.75 0 0 0 0-1.06Zm4.44 0a.75.75 0 0 0-1.06 0l-.05.05a.75.75 0 1 0 1.06 1.06l.05-.05a.75.75 0 0 0 0-1.06ZM6.25 4.75a.75.75 0 0 0 0 1.5h3.5a.75.75 0 0 0 0-1.5h-3.5Zm-.5 6.75h4.5a.75.75 0 0 0 0-1.5h-4.5a.75.75 0 0 0 0 1.5Z" />
        </svg>
        <span>TeachAssist Desktop</span>
      </div>

      <div className="no-drag flex items-center">
        <button
          onClick={() => window.electronAPI.minimize()}
          className="w-12 h-9 flex items-center justify-center hover:bg-github-border/50 transition-colors"
        >
          <svg className="w-3.5 h-3.5" viewBox="0 0 12 12" fill="currentColor">
            <rect y="5" width="12" height="1.5" rx="0.5" />
          </svg>
        </button>
        <button
          onClick={() => window.electronAPI.maximize()}
          className="w-12 h-9 flex items-center justify-center hover:bg-github-border/50 transition-colors"
        >
          {isMaximized ? (
            <svg className="w-3.5 h-3.5" viewBox="0 0 12 12" fill="none" stroke="currentColor" strokeWidth="1.2">
              <rect x="2.5" y="3.5" width="7" height="7" rx="1" />
              <path d="M3.5 3.5V2.5a1 1 0 0 1 1-1h5a1 1 0 0 1 1 1v5a1 1 0 0 1-1 1h-1" />
            </svg>
          ) : (
            <svg className="w-3.5 h-3.5" viewBox="0 0 12 12" fill="none" stroke="currentColor" strokeWidth="1.2">
              <rect x="1.5" y="1.5" width="9" height="9" rx="1" />
            </svg>
          )}
        </button>
        <button
          onClick={() => window.electronAPI.close()}
          className="w-12 h-9 flex items-center justify-center hover:bg-github-danger/80 hover:text-white transition-colors"
        >
          <svg className="w-3.5 h-3.5" viewBox="0 0 12 12" fill="none" stroke="currentColor" strokeWidth="1.4">
            <path d="M2 2l8 8M10 2l-8 8" />
          </svg>
        </button>
      </div>
    </div>
  );
}
