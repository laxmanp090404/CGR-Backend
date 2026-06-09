public class ComplaintEscalationDto
{
    public int EscalationId { get; set; }

    public int ComplaintId { get; set; }

    public int EscalatedToEmployeeId { get; set; }

    public string EscalatedToEmployeeName { get; set; }
        = string.Empty;

    public short EscalationLevel { get; set; }

    public int? EscalationRuleId { get; set; }

    public int? EscalatedByEmployeeId { get; set; }

    public string? EscalatedByEmployeeName { get; set; }

    public string? Reason { get; set; }

    public DateTime EscalatedAt { get; set; }
}