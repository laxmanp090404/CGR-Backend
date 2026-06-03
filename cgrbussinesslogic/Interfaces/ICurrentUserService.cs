namespace cgrbussinesslogic.Interfaces;

public interface ICurrentUserService
{
    int EmployeeId { get; }
    string Role { get; }
    int? DepartmentId { get; }
    bool IsAuthenticated { get; }
}
