import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';

export default function LoginPage() {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [remember, setRemember] = useState(false);
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();
  const { login, error, loadSavedCredentials } = useAuth();

  useEffect(() => {
    loadSavedCredentials().then((creds) => {
      if (creds.username) {
        setUsername(creds.username);
        setRemember(true);
      }
    });
  }, [loadSavedCredentials]);

  const handleLogin = async () => {
    if (!username.trim()) return;
    setLoading(true);
    const success = await login(username, password, remember);
    if (success) {
      navigate('/');
    }
    setLoading(false);
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') handleLogin();
  };

  return (
    <div className="h-screen flex items-center justify-center bg-github-bg">
      <div className="w-full max-w-md">
        {/* Logo */}
        <div className="text-center mb-8">
          <div className="w-14 h-14 rounded-2xl bg-github-accent mx-auto mb-4 flex items-center justify-center">
            <svg className="w-8 h-8 text-white" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <path d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253" strokeLinecap="round" strokeLinejoin="round" />
            </svg>
          </div>
          <h1 className="text-2xl font-bold text-github-text-primary">TeachAssist Desktop</h1>
          <p className="text-sm text-github-text-secondary mt-1">Sign in to view your grades</p>
        </div>

        {/* Login Form */}
        <div className="card p-6 space-y-4">
          <div>
            <label className="block text-sm font-medium text-github-text-secondary mb-1.5">
              Student Number
            </label>
            <input
              type="text"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              onKeyDown={handleKeyDown}
              placeholder="Enter your student number"
              className="w-full bg-github-bg border border-github-border rounded-md px-3 py-2 text-sm
                         text-github-text-primary placeholder-github-text-muted
                         focus:outline-none focus:border-github-accent focus:ring-1 focus:ring-github-accent"
              autoFocus
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-github-text-secondary mb-1.5">
              Password
            </label>
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              onKeyDown={handleKeyDown}
              placeholder="Enter your password"
              className="w-full bg-github-bg border border-github-border rounded-md px-3 py-2 text-sm
                         text-github-text-primary placeholder-github-text-muted
                         focus:outline-none focus:border-github-accent focus:ring-1 focus:ring-github-accent"
            />
          </div>

          <label className="flex items-center gap-2 cursor-pointer">
            <input
              type="checkbox"
              checked={remember}
              onChange={(e) => setRemember(e.target.checked)}
              className="w-4 h-4 rounded border-github-border bg-github-bg text-github-accent
                         focus:ring-github-accent focus:ring-offset-0"
            />
            <span className="text-sm text-github-text-secondary">Remember credentials</span>
          </label>

          {error && (
            <div className="bg-github-danger/10 border border-github-danger/30 rounded-md px-3 py-2 text-sm text-github-danger">
              {error}
            </div>
          )}

          <button
            onClick={handleLogin}
            disabled={loading || !username.trim()}
            className="w-full btn-primary disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {loading ? (
              <span className="flex items-center justify-center gap-2">
                <svg className="animate-spin h-4 w-4" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" fill="none" />
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                </svg>
                Signing in...
              </span>
            ) : (
              'Sign In'
            )}
          </button>

          <p className="text-xs text-github-text-muted text-center pt-2">
            Enter "demo" as username to use demo mode
          </p>
        </div>
      </div>
    </div>
  );
}
