namespace EmployeeAnalyzer.Models;

public class EmployeePairResult
{
    public int Employee1Id { get; set; }
    public int Employee2Id { get; set; }
    public int TotalDaysWorkedTogether { get; set; }
    public List<ProjectCollaboration> CommonProjects { get; set; } = new();
}

public class ProjectCollaboration
{
    public int ProjectId { get; set; }
    public int Employee1Id { get; set; }
    public int Employee2Id { get; set; }
    public int DaysWorkedTogether { get; set; }
    public DateTime OverlapStart { get; set; }
    public DateTime OverlapEnd { get; set; }
}