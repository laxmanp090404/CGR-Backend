using cgrmodellibrary.DTOs.Department;

namespace cgrbussinesslogic.Interfaces;

public interface IDepartmentService
{
    Task<IEnumerable<DepartmentDto>> GetAllAsync(bool? isActive);

    Task<DepartmentDto> GetByIdAsync(int departmentId);

    Task<DepartmentDto> CreateAsync(CreateDepartmentDto dto);

    Task<DepartmentDto> UpdateAsync(int departmentId,UpdateDepartmentDto dto);
}