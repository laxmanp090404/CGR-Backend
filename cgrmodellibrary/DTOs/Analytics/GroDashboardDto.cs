namespace cgrmodellibrary.DTOs.Analytics;

public class GroDashboardDto
{
    public int AssignedToMe { get; set; }

    public int InProgressByMe { get; set; }

    public int ResolvedByMe { get; set; }

    public int EscalatedByMe { get; set; }

    public int OverdueAssignedToMe { get; set; }

    public double? AvgResolutionHours { get; set; }
}