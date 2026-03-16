import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  ReferenceLine,
} from 'recharts';
import type { GradeTimelinePoint } from '../../types';

interface CumulativeGradeChartProps {
  timeline: GradeTimelinePoint[];
}

export default function CumulativeGradeChart({ timeline }: CumulativeGradeChartProps) {
  if (timeline.length === 0) return null;

  const data = timeline.map((pt) => ({
    name: pt.assignmentName.length > 20 ? pt.assignmentName.substring(0, 18) + '...' : pt.assignmentName,
    grade: pt.cumulativeGrade,
    impact: pt.impact,
    isHighImpact: pt.isHighImpact,
  }));

  const avgGrade = timeline[timeline.length - 1]?.cumulativeGrade ?? 0;

  return (
    <div className="h-64">
      <ResponsiveContainer width="100%" height="100%">
        <LineChart data={data} margin={{ top: 5, right: 20, left: 0, bottom: 5 }}>
          <CartesianGrid strokeDasharray="3 3" stroke="#30363D" />
          <XAxis
            dataKey="name"
            tick={{ fill: '#8B949E', fontSize: 11 }}
            axisLine={{ stroke: '#30363D' }}
            tickLine={false}
          />
          <YAxis
            domain={[0, 100]}
            tick={{ fill: '#8B949E', fontSize: 11 }}
            axisLine={{ stroke: '#30363D' }}
            tickLine={false}
          />
          <Tooltip
            contentStyle={{
              backgroundColor: '#161B22',
              border: '1px solid #30363D',
              borderRadius: '6px',
              color: '#E6EDF3',
              fontSize: '12px',
            }}
            formatter={(value: number, name: string) => [
              `${value.toFixed(1)}%`,
              name === 'grade' ? 'Cumulative' : 'Impact',
            ]}
          />
          <ReferenceLine
            y={avgGrade}
            stroke="#58A6FF"
            strokeDasharray="5 5"
            label={{ value: `Avg: ${avgGrade.toFixed(1)}%`, fill: '#58A6FF', fontSize: 11 }}
          />
          <Line
            type="monotone"
            dataKey="grade"
            stroke="#58A6FF"
            strokeWidth={2}
            dot={(props: any) => {
              const { cx, cy, payload } = props;
              if (payload.isHighImpact) {
                return (
                  <circle
                    key={payload.name}
                    cx={cx}
                    cy={cy}
                    r={5}
                    fill={payload.impact >= 0 ? '#238636' : '#F85149'}
                    stroke="#0D1117"
                    strokeWidth={2}
                  />
                );
              }
              return (
                <circle
                  key={payload.name}
                  cx={cx}
                  cy={cy}
                  r={3}
                  fill="#58A6FF"
                  stroke="#0D1117"
                  strokeWidth={1.5}
                />
              );
            }}
            activeDot={{ r: 5, fill: '#58A6FF' }}
          />
        </LineChart>
      </ResponsiveContainer>
    </div>
  );
}
