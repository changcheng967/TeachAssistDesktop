import { useState, useMemo } from 'react';
import { useCourseStore } from '../../state/course-store';

const PRESET_GOALS = [
  { percent: 90, label: "90% - A", color: '#238636' },
  { percent: 85, label: "85% - A-", color: '#3FB950' },
  { percent: 80, label: "80% - B+", color: '#D29922' },
  { percent: 75, label: "75% - B", color: '#9A6700' },
  { percent: 70, label: "70% - B-", color: '#DB6D28' },
];

export default function GradeGoalsModal() {
  const courses = useCourseStore((s) => s.courses);
  const [customGoal, setCustomGoal] = useState(85);
  const [selectedPreset, setSelectedPreset] = useState<number | null>(85);

  const currentAvg = useMemo(() => {
    const valid = courses.filter((c) => c.hasValidMark);
    return valid.length > 0
      ? valid.reduce((sum, c) => sum + (c.numericMark || 0), 0) / valid.length
      : 0;
  }, [courses]);

  const goal = selectedPreset ?? customGoal;
  const progress = Math.min((currentAvg / goal) * 100, 150);
  const pointsNeeded = Math.max(goal - currentAvg, 0);
  const onTrack = currentAvg >= goal * 0.9;
  const circumference = 2 * Math.PI * 45;
  const strokeDashoffset = circumference - (Math.min(progress, 100) / 100) * circumference;

  return (
    <div className="space-y-5">
      {/* Progress Circle */}
      <div className="flex justify-center">
        <div className="relative">
          <svg width="120" height="120" viewBox="0 0 120 120">
            <circle
              cx="60" cy="60" r="45"
              fill="none"
              stroke="#30363D"
              strokeWidth="8"
            />
            <circle
              cx="60" cy="60" r="45"
              fill="none"
              stroke={onTrack ? '#238636' : '#D29922'}
              strokeWidth="8"
              strokeLinecap="round"
              strokeDasharray={circumference}
              strokeDashoffset={strokeDashoffset}
              transform="rotate(-90 60 60)"
              className="transition-all duration-700"
            />
          </svg>
          <div className="absolute inset-0 flex flex-col items-center justify-center">
            <span className="text-2xl font-bold">{currentAvg.toFixed(1)}%</span>
            <span className="text-xs text-github-text-muted">of {goal}%</span>
          </div>
        </div>
      </div>

      {/* Status */}
      <div className="text-center">
        {currentAvg >= goal ? (
          <div className="text-github-success font-medium">Goal achieved!</div>
        ) : onTrack ? (
          <div className="text-github-success font-medium">
            On track! {pointsNeeded.toFixed(1)}% to go
          </div>
        ) : (
          <div className="text-github-warning font-medium">
            {pointsNeeded.toFixed(1)}% needed to reach goal
          </div>
        )}
      </div>

      {/* Preset Goals */}
      <div>
        <div className="text-sm font-medium text-github-text-secondary mb-2">Preset Goals</div>
        <div className="flex flex-wrap gap-2">
          {PRESET_GOALS.map((g) => (
            <button
              key={g.percent}
              onClick={() => {
                setSelectedPreset(g.percent);
                setCustomGoal(g.percent);
              }}
              className={`badge text-xs transition-all ${
                selectedPreset === g.percent
                  ? 'ring-2 ring-offset-1 ring-offset-github-surface'
                  : 'opacity-70 hover:opacity-100'
              }`}
              style={{
                backgroundColor: g.color,
                color: 'white',
                '--tw-ring-color': g.color,
              } as React.CSSProperties}
            >
              {g.label}
            </button>
          ))}
        </div>
      </div>

      {/* Custom Goal */}
      <div>
        <div className="text-sm font-medium text-github-text-secondary mb-2">Custom Goal</div>
        <div className="flex items-center gap-3">
          <input
            type="range"
            min={50}
            max={100}
            value={customGoal}
            onChange={(e) => {
              setCustomGoal(parseInt(e.target.value));
              setSelectedPreset(null);
            }}
            className="flex-1 accent-github-accent"
          />
          <span className="text-sm font-mono font-semibold w-12 text-right">{customGoal}%</span>
        </div>
      </div>
    </div>
  );
}
