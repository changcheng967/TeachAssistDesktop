using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TeachAssistApp.Models;

namespace TeachAssistApp.Helpers;

public static class GradeImpactCalculator
{
    /// <summary>
    /// Represents an assignment group with its per-category scores.
    /// E.g., "Unit 1 Test" might have KU: 75%, T: 80%, C: 85%.
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
            // Collect scores per category (average if multiple marks in same category)
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

        // Compute the FINAL grade's category state (all items) for contribution calculation
        var finalCatState = GetCategoryState(items, items.Count - 1);
        double totalActiveWeight = hasWeights
            ? finalCatState.Where(c => c.Value.Count > 0)
                .Sum(c => weightTable.GetWeight(c.Key) ?? 0)
            : 0;

        // Compute each assignment's contribution to the FINAL grade
        var contributions = new Dictionary<string, double>();
        foreach (var item in items)
        {
            double contrib = 0;
            if (hasWeights && totalActiveWeight > 0)
            {
                // contribution = sum over categories: score_c * weight_c / (n_c * total_active_weight)
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
                // Equal weight: each group contributes equally
                contrib = items.Count > 0 ? item.CategoryScores.Values.Average() / items.Count : 0;
            }
            contributions[item.Name] = contrib;
        }

        // Build timeline: compute running grade at each step
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            double before = i > 0 ? ComputeGrade(items, weightTable, hasWeights, i - 1) : 0;
            double after = ComputeGrade(items, weightTable, hasWeights, i);
            double impact = after - before;

            timeline.Add(new GradeTimelinePoint
            {
                Index = i,
                AssignmentName = item.Name,
                Date = item.Date,
                CumulativeGrade = after,
                Impact = impact,
                IsHighImpact = false,
                FirstPoint = i == 0
            });

            impacts[item.Name] = new AssignmentImpact
            {
                AssignmentName = item.Name,
                ImpactDelta = impact,
                WeightedContribution = contributions.GetValueOrDefault(item.Name, 0),
                IsHighImpact = false,
                CumulativeBefore = double.IsNaN(before) ? 0 : before,
                CumulativeAfter = after
            };
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
    /// Compute the weighted grade considering all items up to index upTo.
    /// Correct approach: group by category → average within each → weight across.
    /// </summary>
    private static double ComputeGrade(List<ScoredItem> items, WeightTable weightTable, bool hasWeights, int upTo)
    {
        if (upTo < 0 || items.Count == 0) return 0;

        var catState = GetCategoryState(items, upTo);

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
    /// Collect all per-category scores from items up to a given index.
    /// </summary>
    private static Dictionary<string, List<double>> GetCategoryState(List<ScoredItem> items, int upTo)
    {
        var state = new Dictionary<string, List<double>>();
        foreach (var item in items.Take(upTo + 1))
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
