namespace EmployeeAnalyzer.Models;

public class EmployeeRecord
{
    public int EmployeeId { get; set; }
    public int ProjectId { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime? DateTo { get; set; }

    public DateTime EffectiveEndDate => DateTo ?? DateTime.Today;

    public int TotalDaysWorked => (EffectiveEndDate - DateFrom).Days + 1;
}