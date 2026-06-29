using System;

namespace cgrmodellibrary.DTOs.Employee;

public class GroActiveWorkloadDto
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = null!;
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = null!;
    public int ActiveComplaintCount { get; set; }
    public long WeightedScore { get; set; }
    public int LowCount { get; set; }
    public int MediumCount { get; set; }
    public int HighCount { get; set; }
    public int CriticalCount { get; set; }
}
