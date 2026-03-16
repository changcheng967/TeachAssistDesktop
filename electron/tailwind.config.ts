import type { Config } from 'tailwindcss';

const config: Config = {
  content: ['./src/renderer/**/*.{ts,tsx,html}'],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        github: {
          bg: '#0D1117',
          surface: '#161B22',
          border: '#30363D',
          'border-muted': '#21262D',
          accent: '#58A6FF',
          'accent-hover': '#79C0FF',
          success: '#238636',
          'success-emphasis': '#2EA043',
          warning: '#D29922',
          'warning-emphasis': '#9A6700',
          danger: '#F85149',
          'danger-emphasis': '#DA3633',
          text: {
            primary: '#E6EDF3',
            secondary: '#8B949E',
            muted: '#6E7681',
          },
        },
      },
      fontFamily: {
        sans: [
          '-apple-system',
          'BlinkMacSystemFont',
          'Segoe UI',
          'Noto Sans',
          'Helvetica',
          'Arial',
          'sans-serif',
        ],
        mono: [
          'SFMono-Regular',
          'Consolas',
          'Liberation Mono',
          'Menlo',
          'monospace',
        ],
      },
    },
  },
  plugins: [],
};

export default config;
