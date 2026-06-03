namespace cgrmodellibrary.DTOs.Category;

public class CategoryDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = null!;
    public short DefaultPriorityId { get; set; }
    public string DefaultPriorityName { get; set; } = null!;
    public int SlaHours { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
