namespace cgrmodellibrary.DTOs.Analytics;

public class ComplaintAnalyticsDto
{
    public int TotalComplaints { get; set; }
    public int OpenComplaints { get; set; }
    public int ResolvedComplaints { get; set; }
    public int ClosedComplaints { get; set; }
    public int RejectedComplaints { get; set; }
    public int EscalatedComplaints { get; set; }
    public int SlaBreachedComplaints { get; set; }
    public double AverageResolutionHours { get; set; }
    public List<CategoryBreakdownDto> ByCategory { get; set; } = new();
    public List<DepartmentBreakdownDto> ByDepartment { get; set; } = new();
    public List<PriorityBreakdownDto> ByPriority { get; set; } = new();
}

public class CategoryBreakdownDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public int Count { get; set; }
}

public class DepartmentBreakdownDto
{
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = null!;
    public int Count { get; set; }
}

public class PriorityBreakdownDto
{
    public short PriorityId { get; set; }
    public string PriorityName { get; set; } = null!;
    public int Count { get; set; }
}
