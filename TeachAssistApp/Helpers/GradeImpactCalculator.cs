using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TeachAssistApp.Models;

namespace TeachAssistApp.Helpers;

public static class GradeImpactCalculator
{
    /// <summary>
    /// One scored item per assignment group, with per-category percentage scores.
    /// E.g., "Unit 1 Test" might have KU: 75%, T: 80%, C: 90%.
    /// </summary>
    private record ScoredItem(
        string Name,
        string? Date,
        Dictionary<string, double> CategoryScores);

    public static (List<GradeTimelinePoint> Timeline, Dictionary<string, AssignmentImpact> Impacts)
        Calculate(List<AssignmentGroup> groups, WeightTable weightTable)
    {
        var timeline = new List<GradeTimelinePoint>();
        var impacts = new Dictionary<string, AssignmentImpact>();
        var hasWeights = weightTable.Weights.Count > 0;

        // Build scored items: one per assignment group, with per-category percentage scores
        var items = new List<ScoredItem>();

        foreach (var g in groups)
        {
            var catScoreLists = new Dictionary<string, List<double>>();
            foreach (var a in g.Assignments)
            {
                if (a.MarkAchieved.HasValue && a.MarkPossible.HasValue && a.MarkPossible.Value > 0)
                {
                    var pct = (a.MarkAchieved.Value / a.MarkPossible.Value) * 100;
                    var cat = string.IsNullOrEmpty(a.Category) ? "O" : a.Category;
                    if (!catScoreLists.ContainsKey(cat))
                        catScoreLists[cat] = new List<double>();
                    catScoreLists[cat].Add(pct);
                }
            }

            if (catScoreLists.Count > 0)
            {
                var catScores = catScoreLists.ToDictionary(kv => kv.Key, kv => kv.Value.Average());
                var date = g.Assignments
                    .Select(a => a.Date)
                    .FirstOrDefault(d => !string.IsNullOrWhiteSpace(d));
                items.Add(new ScoredItem(g.Name, date, catScores));
            }
        }

        if (items.Count == 0) return (timeline, impacts);

        // Sort by date
        items = items
            .OrderBy(x =>
            {
                if (x.Date == null) return double.MaxValue;
                if (DateTime.TryParseExact(x.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                    return dt.Ticks;
                if (DateTime.TryParse(x.Date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt2))
                    return dt2.Ticks;
                return double.MaxValue;
            })
            .ThenBy(x => x.Name)
            .ToList();

        // ---- TIMELINE: cumulative grade over time (for chart) ----
        for (int i = 0; i < items.Count; i++)
        {
            var subset = items.Take(i + 1).ToList();
            double cumulativeGrade = ComputeGrade(subset, weightTable, hasWeights);

            timeline.Add(new GradeTimelinePoint
            {
                Index = i,
                AssignmentName = items[i].Name,
                Date = items[i].Date,
                CumulativeGrade = cumulativeGrade,
                Impact = 0, // filled below
                IsHighImpact = false,
                FirstPoint = i == 0
            });
        }

        // ---- IMPACTS: leave-one-out ----
        // For each assignment, compute: final_grade - grade_without_this_assignment
        // This directly answers: "How much does this assignment affect my current grade?"
        //   Positive = this assignment helped your grade
        //   Negative = this assignment hurt your grade

        double finalGrade = ComputeGrade(items, weightTable, hasWeights);

        // Category state for contribution calculation
        var finalCatState = GetCategoryState(items);
        double totalActiveWeight = hasWeights
            ? finalCatState.Where(c => c.Value.Count > 0)
                .Sum(c => weightTable.GetWeight(c.Key) ?? 0)
            : 0;

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];

            // Compute grade WITHOUT this assignment
            var remaining = new List<ScoredItem>(items.Count - 1);
            for (int j = 0; j < items.Count; j++)
            {
                if (j != i) remaining.Add(items[j]);
            }
            double gradeWithout = remaining.Count > 0
                ? ComputeGrade(remaining, weightTable, hasWeights)
                : 0;

            double impact = finalGrade - gradeWithout;

            // Weighted contribution: portion of final grade from this assignment
            double contrib = 0;
            if (hasWeights && totalActiveWeight > 0)
            {
                foreach (var kv in item.CategoryScores)
                {
                    var w = weightTable.GetWeight(kv.Key) ?? 0;
                    var n = finalCatState.GetValueOrDefault(kv.Key)?.Count ?? 1;
                    if (n > 0 && w > 0)
                        contrib += kv.Value * w / (n * totalActiveWeight);
                }
            }
            else
            {
                contrib = items.Count > 0 ? item.CategoryScores.Values.Average() / items.Count : 0;
            }

            impacts[item.Name] = new AssignmentImpact
            {
                AssignmentName = item.Name,
                ImpactDelta = impact,
                WeightedContribution = contrib,
                IsHighImpact = false,
                CumulativeBefore = gradeWithout,
                CumulativeAfter = finalGrade
            };

            // Also set impact on timeline point
            timeline[i].Impact = impact;
        }

        // Mark high impacts: |impact| >= 3.0 OR top 3 by magnitude
        var byMagnitude = impacts.Values.OrderByDescending(v => Math.Abs(v.ImpactDelta)).ToList();
        var top3Set = new HashSet<string>(byMagnitude.Take(3).Select(v => v.AssignmentName));

        foreach (var entry in impacts)
        {
            bool isHigh = Math.Abs(entry.Value.ImpactDelta) >= 3.0 || top3Set.Contains(entry.Key);
            entry.Value.IsHighImpact = isHigh;
            var tl = timeline.FirstOrDefault(t => t.AssignmentName == entry.Key);
            if (tl != null) tl.IsHighImpact = isHigh;
        }

        return (timeline, impacts);
    }

    /// <summary>
    /// Compute the weighted grade for a set of items.
    /// Correct formula: group by category → average within each → weight across.
    /// </summary>
    private static double ComputeGrade(List<ScoredItem> items, WeightTable weightTable, bool hasWeights)
    {
        if (items.Count == 0) return 0;

        var catState = GetCategoryState(items);

        if (!hasWeights)
        {
            var allScores = catState.Values.SelectMany(x => x).ToList();
            return allScores.Count > 0 ? allScores.Average() : 0;
        }

        double weightedSum = 0;
        double totalWeight = 0;

        foreach (var cat in catState)
        {
            if (cat.Value.Count == 0) continue;
            var w = weightTable.GetWeight(cat.Key) ?? 0;
            if (w > 0)
            {
                weightedSum += cat.Value.Average() * w;
                totalWeight += w;
            }
        }

        return totalWeight > 0 ? weightedSum / totalWeight : 0;
    }

    /// <summary>
    /// Collect all per-category scores from items.
    /// </summary>
    private static Dictionary<string, List<double>> GetCategoryState(List<ScoredItem> items)
    {
        var state = new Dictionary<string, List<double>>();
        foreach (var item in items)
        {
            foreach (var kv in item.CategoryScores)
            {
                if (!state.ContainsKey(kv.Key))
                    state[kv.Key] = new List<double>();
                state[kv.Key].Add(kv.Value);
            }
        }
        return state;
    }
}
