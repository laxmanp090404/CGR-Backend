namespace cgrmodellibrary.DTOs.Analytics;

public class MyDashboardDto
{
    public int TotalRaised { get; set; }

    public int OpenComplaints { get; set; }

    public int ResolvedComplaints { get; set; }

    public int ClosedComplaints { get; set; }

    public int RejectedComplaints { get; set; }

    public int ExternallyEscalatedComplaints { get; set; }

    public double? AvgResolutionHours { get; set; }
    public IEnumerable<StatusDistributionDto> StatusBreakdown { get; set; }
        = [];
}