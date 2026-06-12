namespace cgrmodellibrary.DTOs.Analytics;

public class StatusDistributionDto
{
    public short StatusId { get; set; }

    public string StatusName { get; set; } = string.Empty;

    public int Count { get; set; }
}