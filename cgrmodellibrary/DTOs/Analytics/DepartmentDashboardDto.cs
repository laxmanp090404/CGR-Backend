namespace cgrmodellibrary.DTOs.Analytics;

public class DepartmentDashboardDto
{
    public int DepartmentId { get; set; }

    public string DepartmentName { get; set; } = string.Empty;

    public int TotalComplaints { get; set; }

    public int OpenComplaints { get; set; }

    public int OverdueComplaints { get; set; }

    public double? AvgResolutionHours { get; set; }
    public double? SlaCompliancePercent { get; set; }
    public IEnumerable<StatusDistributionDto> StatusDistribution { get; set; }
        = [];
}