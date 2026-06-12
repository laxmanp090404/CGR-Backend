namespace cgrmodellibrary.DTOs.Analytics;

public class TopCategoryDto
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public string DepartmentName { get; set; } = string.Empty;

    public int ComplaintCount { get; set; }
}