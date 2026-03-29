using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TeachAssistApp.Models;

namespace TeachAssistApp.Helpers;

public static class GradeImpactCalculator
{
    public static (List<GradeTimelinePoint> Timeline, Dictionary<string, AssignmentImpact> Impacts)
        Calculate(List<AssignmentGroup> groups, WeightTable weightTable)
    {
        var timeline = new List<GradeTimelinePoint>();
        var impacts = new Dictionary<string, AssignmentImpact>();
        var hasWeights = weightTable.Weights.Count > 0;

        // Build scored list: groups with valid marks and computed score + weight
        var scored = new List<(AssignmentGroup Group, double Score, double Weight, string Category)>();

        foreach (var g in groups.Where(g => g.HasAnyMark))
        {
            var validMarks = g.Assignments
                .Where(a => a.MarkAchieved.HasValue && a.MarkPossible.HasValue && a.MarkPossible.Value > 0)
                .ToList();

            if (!validMarks.Any()) continue;

            double avgPercentage = validMarks.Average(a => a.Percentage ?? 0);

            double weight;
            string category;
            if (hasWeights)
            {
                // Use the max category weight to avoid inflating multi-category assignments
                var categories = g.Assignments.Select(a => a.Category).Distinct().ToList();
                category = categories.FirstOrDefault() ?? "O";
                weight = categories.Max(c => weightTable.GetWeight(c) ?? 0);
            }
            else
            {
                category = "O";
                weight = 1.0; // normalized to 1/N below
            }

            scored.Add((g, avgPercentage, weight, category));
        }

        // For equal-weight courses, normalize to 1/N
        if (!hasWeights && scored.Count > 0)
        {
            var eqWeight = 1.0 / scored.Count;
            scored = scored.Select(x => (x.Group, x.Score, eqWeight, x.Category)).ToList();
        }

        // Sort by date (best effort), fall back to list order
        scored = scored
            .OrderBy(x =>
            {
                var dateStr = x.Group.Assignments
                    .Select(a => a.Date)
                    .FirstOrDefault(d => !string.IsNullOrWhiteSpace(d));

                if (dateStr == null) return double.MaxValue;

                if (DateTime.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                    return dt.Ticks;
                if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt2))
                    return dt2.Ticks;

                return double.MaxValue;
            })
            .ThenBy(x => x.Group.Name)
            .ToList();

        // Pre-compute weighted contribution for each assignment.
        // Weighted contribution = score * (categoryWeight / numAssignmentsInCategory)
        var categoryAssignmentCounts = scored
            .GroupBy(x => x.Category)
            .ToDictionary(g => g.Key, g => g.Count());

        var contributions = new Dictionary<string, double>();
        foreach (var item in scored)
        {
            var count = categoryAssignmentCounts.GetValueOrDefault(item.Category, 1);
            // Contribution = percentage score * (category weight / assignments in that category)
            // E.g., KU=25%, 3 KU assignments, scored 80% → 80% * (25%/3) = 6.67%
            var contribution = item.Weight > 0
                ? item.Score * (item.Weight / count / 100.0) * 100.0
                : 0;
            contributions[item.Group.Name] = contribution;
        }

        // Build cumulative timeline and running impacts
        double weightedSum = 0;
        double totalWeight = 0;

        for (int i = 0; i < scored.Count; i++)
        {
            var item = scored[i];

            double cumulativeBefore = totalWeight > 0 ? weightedSum / totalWeight : 0;

            weightedSum += item.Score * item.Weight;
            totalWeight += item.Weight;

            double cumulativeAfter = weightedSum / totalWeight;
            double impact = cumulativeAfter - cumulativeBefore;

            timeline.Add(new GradeTimelinePoint
            {
                Index = i,
                AssignmentName = item.Group.Name,
                Date = item.Group.Assignments.Select(a => a.Date).FirstOrDefault(d => !string.IsNullOrWhiteSpace(d)),
                CumulativeGrade = cumulativeAfter,
                Impact = impact,
                IsHighImpact = false, // set below
                FirstPoint = i == 0
            });

            impacts[item.Group.Name] = new AssignmentImpact
            {
                AssignmentName = item.Group.Name,
                ImpactDelta = impact,
                WeightedContribution = contributions.GetValueOrDefault(item.Group.Name, 0),
                IsHighImpact = false, // set below
                CumulativeBefore = cumulativeBefore,
                CumulativeAfter = cumulativeAfter
            };
        }

        // Hybrid high-impact: |impact| >= 3.0 OR top 3 by |impact| magnitude
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
}
