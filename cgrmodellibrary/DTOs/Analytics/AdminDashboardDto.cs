namespace cgrmodellibrary.DTOs.Analytics;

public class AdminDashboardDto
{
    public int TotalComplaints { get; set; }

    public int OpenComplaints { get; set; }

    public int ResolvedComplaints { get; set; }

    public int ClosedComplaints { get; set; }

    public int RejectedComplaints { get; set; }

    public int ExternallyEscalatedComplaints { get; set; }

    public int PendingComplaintRequests { get; set; }

    public int PendingRoleRequests { get; set; }

    public int ActiveEmployees { get; set; }

    public int ActiveDepartments { get; set; }

    public int OverdueComplaints { get; set; }

    public double? AvgResolutionHours { get; set; }
    public double? SlaCompliancePercent { get; set; }
}