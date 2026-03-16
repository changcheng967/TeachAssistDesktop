import { dialog, BrowserWindow } from 'electron';
import type { Course } from '../../renderer/types/course';

/** Export courses to CSV */
export async function exportCsv(courses: Course[]): Promise<{ success: boolean; error?: string }> {
  const win = BrowserWindow.getFocusedWindow();
  if (!win) return { success: false, error: 'No window' };

  const result = await dialog.showSaveDialog(win, {
    title: 'Export Grades as CSV',
    defaultPath: `TeachAssist_Grades_${Date.now()}.csv`,
    filters: [{ name: 'CSV Files', extensions: ['csv'] }],
  });

  if (result.canceled || !result.filePath) return { success: false, error: 'Cancelled' };

  try {
    const fs = await import('fs/promises');
    const header = 'Course Code,Course Name,Mark,Grade Letter,Level,Room,Block\n';
    const rows = courses
      .map((c) =>
        [
          c.code,
          `"${c.name}"`,
          typeof c.overallMark === 'number' ? c.overallMark.toFixed(1) : 'N/A',
          c.gradeLetter,
          `"${c.gradeLevel}"`,
          c.room,
          c.block,
        ].join(',')
      )
      .join('\n');

    await fs.writeFile(result.filePath, header + rows, 'utf-8');
    return { success: true };
  } catch (err) {
    return { success: false, error: String(err) };
  }
}

/** Export courses as HTML report */
export async function exportHtmlReport(courses: Course[]): Promise<{ success: boolean; error?: string }> {
  const win = BrowserWindow.getFocusedWindow();
  if (!win) return { success: false, error: 'No window' };

  const result = await dialog.showSaveDialog(win, {
    title: 'Export Grade Report',
    defaultPath: `TeachAssist_Report_${Date.now()}.html`,
    filters: [{ name: 'HTML Files', extensions: ['html'] }],
  });

  if (result.canceled || !result.filePath) return { success: false, error: 'Cancelled' };

  try {
    const fs = await import('fs/promises');
    const validCourses = courses.filter((c) => c.hasValidMark);
    const average =
      validCourses.length > 0
        ? validCourses.reduce((sum, c) => sum + (c.numericMark || 0), 0) / validCourses.length
        : 0;
    const highest = validCourses.length > 0 ? Math.max(...validCourses.map((c) => c.numericMark || 0)) : 0;
    const lowest = validCourses.length > 0 ? Math.min(...validCourses.map((c) => c.numericMark || 0)) : 0;

    const summaryColor = average >= 80 ? '#238636' : average >= 70 ? '#D29922' : '#F85149';

    const courseRows = courses
      .sort((a, b) => a.code.localeCompare(b.code))
      .map((c) => {
        const mark = typeof c.overallMark === 'number' ? c.overallMark.toFixed(1) : 'N/A';
        const color = c.gradeColor;
        return `<tr>
          <td style="padding:10px;border-bottom:1px solid #30363D;font-weight:600">${c.code}</td>
          <td style="padding:10px;border-bottom:1px solid #30363D">${c.name}</td>
          <td style="padding:10px;border-bottom:1px solid #30363D">
            <span style="background:${color};color:white;padding:4px 10px;border-radius:12px;font-weight:600;font-size:13px">${mark}</span>
          </td>
        </tr>`;
      })
      .join('\n');

    const html = `<!DOCTYPE html>
<html><head><meta charset="utf-8"><title>TeachAssist Grade Report</title></head>
<body style="font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Helvetica,Arial,sans-serif;background:#0D1117;color:#E6EDF3;max-width:800px;margin:0 auto;padding:40px 20px">
  <div style="text-align:center;margin-bottom:30px">
    <h1 style="margin:0;font-size:28px">TeachAssist Grade Report</h1>
    <p style="color:#8B949E;margin-top:8px">Generated on ${new Date().toLocaleDateString('en-CA')}</p>
  </div>
  <div style="display:flex;gap:16px;margin-bottom:30px">
    <div style="flex:1;background:#161B22;border:1px solid #30363D;border-radius:8px;padding:16px;text-align:center">
      <div style="font-size:24px;font-weight:bold;color:${summaryColor}">${average.toFixed(1)}%</div>
      <div style="font-size:13px;color:#8B949E;margin-top:4px">Overall Average</div>
    </div>
    <div style="flex:1;background:#161B22;border:1px solid #30363D;border-radius:8px;padding:16px;text-align:center">
      <div style="font-size:24px;font-weight:bold;color:#238636">${highest.toFixed(1)}%</div>
      <div style="font-size:13px;color:#8B949E;margin-top:4px">Highest Mark</div>
    </div>
    <div style="flex:1;background:#161B22;border:1px solid #30363D;border-radius:8px;padding:16px;text-align:center">
      <div style="font-size:24px;font-weight:bold;color:#F85149">${lowest.toFixed(1)}%</div>
      <div style="font-size:13px;color:#8B949E;margin-top:4px">Lowest Mark</div>
    </div>
    <div style="flex:1;background:#161B22;border:1px solid #30363D;border-radius:8px;padding:16px;text-align:center">
      <div style="font-size:24px;font-weight:bold">${courses.length}</div>
      <div style="font-size:13px;color:#8B949E;margin-top:4px">Total Courses</div>
    </div>
  </div>
  <h2 style="font-size:18px;margin-bottom:12px">Course Details</h2>
  <table style="width:100%;border-collapse:collapse">
    <thead>
      <tr style="border-bottom:2px solid #30363D">
        <th style="padding:10px;text-align:left;color:#8B949E;font-weight:600">Code</th>
        <th style="padding:10px;text-align:left;color:#8B949E;font-weight:600">Name</th>
        <th style="padding:10px;text-align:left;color:#8B949E;font-weight:600">Mark</th>
      </tr>
    </thead>
    <tbody>${courseRows}</tbody>
  </table>
  <p style="text-align:center;color:#6E7681;margin-top:30px;font-size:12px">Generated by TeachAssist Desktop App for YRDSB Students</p>
</body></html>`;

    await fs.writeFile(result.filePath, html, 'utf-8');
    return { success: true };
  } catch (err) {
    return { success: false, error: String(err) };
  }
}
