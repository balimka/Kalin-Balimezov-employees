using EmployeeAnalyzer.Models;

namespace EmployeeAnalyzer.Services;

public class OptimizedEmployeeAnalysisService
{
    public EmployeePairResult FindLongestCollaboratingPair(List<EmployeeRecord> records)
    {
        if (records == null || records.Count < 2)
            throw new ArgumentException("At least 2 employee records are required");

        var collaborations = new Dictionary<(int, int), List<ProjectCollaboration>>();
        var recordsByProject = records.GroupBy(r => r.ProjectId);

        foreach (var projectGroup in recordsByProject)
        {
            var projectRecords = projectGroup.ToList();
            var projectCollaborations = FindCollaborationsInProject(projectRecords);
            
            foreach (var collaboration in projectCollaborations)
            {
                var key = collaboration.Employee1Id < collaboration.Employee2Id 
                    ? (collaboration.Employee1Id, collaboration.Employee2Id)
                    : (collaboration.Employee2Id, collaboration.Employee1Id);
                
                if (!collaborations.ContainsKey(key))
                    collaborations[key] = new List<ProjectCollaboration>();
                
                collaborations[key].Add(collaboration);
            }
        }

        if (!collaborations.Any())
            throw new InvalidOperationException("No employee pairs found who worked together on common projects");

        return collaborations
            .Select(kvp => new EmployeePairResult
            {
                Employee1Id = kvp.Key.Item1,
                Employee2Id = kvp.Key.Item2,
                CommonProjects = kvp.Value,
                TotalDaysWorkedTogether = CalculateUniqueDaysWorkedTogether(kvp.Value)
            })
            .OrderByDescending(pair => pair.TotalDaysWorkedTogether)
            .ThenBy(pair => pair.Employee1Id)
            .ThenBy(pair => pair.Employee2Id)
            .First();
    }

    private List<ProjectCollaboration> FindCollaborationsInProject(List<EmployeeRecord> projectRecords)
    {
        var collaborations = new List<ProjectCollaboration>();
        
        for (int i = 0; i < projectRecords.Count; i++)
        {
            for (int j = i + 1; j < projectRecords.Count; j++)
            {
                var overlap = CalculateOverlap(projectRecords[i], projectRecords[j]);
                if (overlap != null && overlap.DaysWorkedTogether > 0)
                {
                    collaborations.Add(overlap);
                }
            }
        }

        return collaborations;
    }

    private ProjectCollaboration? CalculateOverlap(EmployeeRecord record1, EmployeeRecord record2)
    {
        if (record1.ProjectId != record2.ProjectId)
            return null;

        var emp1Start = record1.DateFrom;
        var emp1End = record1.EffectiveEndDate;
        var emp2Start = record2.DateFrom;
        var emp2End = record2.EffectiveEndDate;

        var overlapStart = emp1Start > emp2Start ? emp1Start : emp2Start;
        var overlapEnd = emp1End < emp2End ? emp1End : emp2End;

        if (overlapStart > overlapEnd)
            return null;

        var daysWorkedTogether = (overlapEnd - overlapStart).Days + 1;

        if (daysWorkedTogether <= 0)
            return null;

        return new ProjectCollaboration
        {
            ProjectId = record1.ProjectId,
            Employee1Id = record1.EmployeeId,
            Employee2Id = record2.EmployeeId,
            DaysWorkedTogether = daysWorkedTogether,
            OverlapStart = overlapStart,
            OverlapEnd = overlapEnd
        };
    }

    private int CalculateUniqueDaysWorkedTogether(List<ProjectCollaboration> collaborations)
    {
        if (collaborations == null || !collaborations.Any())
            return 0;

        var dateRanges = collaborations
            .Select(c => new DateRange { Start = c.OverlapStart, End = c.OverlapEnd })
            .OrderBy(r => r.Start)
            .ToList();

        var mergedRanges = new List<DateRange>();
        var currentRange = dateRanges[0];

        for (int i = 1; i < dateRanges.Count; i++)
        {
            var nextRange = dateRanges[i];

            if (nextRange.Start <= currentRange.End.AddDays(1))
            {
                currentRange.End = currentRange.End > nextRange.End ? currentRange.End : nextRange.End;
            }
            else
            {
                mergedRanges.Add(currentRange);
                currentRange = nextRange;
            }
        }

        mergedRanges.Add(currentRange);
        return mergedRanges.Max(r => (r.End - r.Start).Days + 1);
    }
}

internal class DateRange
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
}