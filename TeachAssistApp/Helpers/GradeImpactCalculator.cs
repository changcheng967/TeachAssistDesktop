using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TeachAssistApp.Models;

namespace TeachAssistApp.Helpers;

public static class GradeImpactCalculator
{
    private record ScoredItem(
        string Name,
        string? Date,
        Dictionary<string, (double pct, double weight)> CategoryScores);

    public static (List<GradeTimelinePoint> Timeline, Dictionary<string, AssignmentImpact> Impacts)
        Calculate(List<AssignmentGroup> groups, WeightTable weightTable)
    {
        var timeline = new List<GradeTimelinePoint>();
        var impacts = new Dictionary<string, AssignmentImpact>();
        var hasWeights = weightTable.Weights.Count > 0;

        var items = new List<ScoredItem>();

        foreach (var g in groups)
        {
            var catScoreLists = new Dictionary<string, List<(double pct, double weight)>>();
            foreach (var a in g.Assignments)
            {
                if (a.MarkAchieved.HasValue && a.MarkPossible.HasValue && a.MarkPossible.Value > 0)
                {
                    var pct = (a.MarkAchieved.Value / a.MarkPossible.Value) * 100;
                    var w = a.Weight ?? 0;
                    // If no per-assignment weight, use MarkPossible as weight basis
                    if (w <= 0) w = a.MarkPossible.Value;
                    var cat = string.IsNullOrEmpty(a.Category) ? "O" : a.Category;
                    if (!catScoreLists.ContainsKey(cat))
                        catScoreLists[cat] = new List<(double, double)>();
                    catScoreLists[cat].Add((pct, w));
                }
            }

            if (catScoreLists.Count > 0)
            {
                var catScores = catScoreLists.ToDictionary(
                    kv => kv.Key,
                    kv => WeightedAverage(kv.Value));
                var date = g.Assignments
                    .Select(a => a.Date)
                    .FirstOrDefault(d => !string.IsNullOrWhiteSpace(d));
                items.Add(new ScoredItem(g.Name, date, catScores));
            }
        }

        if (items.Count == 0) return (timeline, impacts);

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

        // ---- TIMELINE ----
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
                Impact = 0,
                IsHighImpact = false,
                FirstPoint = i == 0
            });
        }

        // ---- IMPACTS: leave-one-out ----
        double finalGrade = ComputeGrade(items, weightTable, hasWeights);

        var finalCatState = GetCategoryState(items);
        double totalActiveWeight = hasWeights
            ? finalCatState.Where(c => c.Value.Count > 0)
                .Sum(c => weightTable.GetWeight(c.Key) ?? 0)
            : 0;

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];

            var remaining = new List<ScoredItem>(items.Count - 1);
            for (int j = 0; j < items.Count; j++)
            {
                if (j != i) remaining.Add(items[j]);
            }
            double gradeWithout = remaining.Count > 0
                ? ComputeGrade(remaining, weightTable, hasWeights)
                : 0;

            double impact = finalGrade - gradeWithout;

            double contrib = 0;
            if (hasWeights && totalActiveWeight > 0)
            {
                foreach (var kv in item.CategoryScores)
                {
                    var w = weightTable.GetWeight(kv.Key) ?? 0;
                    var n = finalCatState.GetValueOrDefault(kv.Key)?.Count ?? 1;
                    if (n > 0 && w > 0)
                        contrib += kv.Value.pct * w / (n * totalActiveWeight);
                }
            }
            else
            {
                contrib = items.Count > 0 ? item.CategoryScores.Values.Average(v => v.pct) / items.Count : 0;
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

            timeline[i].Impact = impact;
        }

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

    private static double ComputeGrade(List<ScoredItem> items, WeightTable weightTable, bool hasWeights)
    {
        if (items.Count == 0) return 0;

        var catState = GetCategoryState(items);

        if (!hasWeights)
        {
            var allScores = catState.Values.SelectMany(x => x).ToList();
            return allScores.Count > 0 ? allScores.Average(v => v.pct) : 0;
        }

        double weightedSum = 0;
        double totalWeight = 0;

        foreach (var cat in catState)
        {
            if (cat.Value.Count == 0) continue;
            var w = weightTable.GetWeight(cat.Key) ?? 0;
            if (w > 0)
            {
                var catAvg = WeightedAverage(cat.Value).pct;
                weightedSum += catAvg * w;
                totalWeight += w;
            }
        }

        return totalWeight > 0 ? weightedSum / totalWeight : 0;
    }

    private static Dictionary<string, List<(double pct, double weight)>> GetCategoryState(List<ScoredItem> items)
    {
        var state = new Dictionary<string, List<(double pct, double weight)>>();
        foreach (var item in items)
        {
            foreach (var kv in item.CategoryScores)
            {
                if (!state.ContainsKey(kv.Key))
                    state[kv.Key] = new List<(double, double)>();
                state[kv.Key].Add(kv.Value);
            }
        }
        return state;
    }

    private static (double pct, double weight) WeightedAverage(List<(double pct, double weight)> scores)
    {
        if (scores.Count == 0) return (0, 0);
        var totalW = scores.Sum(s => s.weight);
        if (totalW <= 0) return (scores.Average(s => s.pct), 0);
        return (scores.Sum(s => s.pct * s.weight) / totalW, totalW);
    }
}
