using cgrbussinesslogic.Interfaces;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.Exceptions;
using cgrmodellibrary.Models;
namespace cgrbussinesslogic.Services;

public class ComplaintAssignmentEngine : IComplaintAssignmentEngine
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IComplaintAssignmentRepository _assignmentRepository;

    private const short ROLE_EMPLOYEE = 1;
    private const short ROLE_GRO = 2;
    private const short ROLE_DEPARTMENT_HEAD = 3;
    private const short ROLE_ADMIN = 4;
    public ComplaintAssignmentEngine(
     IEmployeeRepository employeeRepository,
     IComplaintAssignmentRepository assignmentRepository)
    {
        _employeeRepository = employeeRepository;
        _assignmentRepository = assignmentRepository;
    }
    public async Task<(int HandlerId, short EscalationLevel)> DetermineInitialAssignmentAsync(int complaintId, int categoryDepartmentId, short creatorRole)
    {
        switch (creatorRole)
        {
            // complaints raised by employee ->  requires assignment algorithm
            case ROLE_EMPLOYEE:
                return await AssignComplaintAsync(complaintId, categoryDepartmentId);
            // complaints raised by GRO -> handle by dept head
            case ROLE_GRO:
                {
                    var departmentHead =
                        await _employeeRepository.GetDepartmentHeadAsync(categoryDepartmentId);
                    // esclation level 1 as dept head
                    if (departmentHead != null)
                    {
                        return (departmentHead.EmployeeId, 1);
                    }
                    // no dept head so admin take care
                    var admin = await _employeeRepository.GetAdminAsync() ?? throw new BusinessRuleException("No active admin found.");
                    //escalation level 2 as admin
                    return (admin.EmployeeId, 2);
                }

            case ROLE_DEPARTMENT_HEAD:
                {
                    var admin =
                        await _employeeRepository.GetAdminAsync()
                        ?? throw new BusinessRuleException("No Admin found.");

                    return (
                        admin.EmployeeId,
                        2);
                }

            default:
                throw new BusinessRuleException("Invalid complaint creator role.");
        }
    }
    
    // assignment algorithm for complaints raised by employees
    private async Task<(int HandlerId, short EscalationLevel)> AssignComplaintAsync(int complaintId, int departmentId)
    {
        // get previous GRO handlers to avoid reassigning to them
        var previoushandlers = await _assignmentRepository.GetPreviousHandlersAsync(complaintId);
        // get active GROs in the department and choose who has not previously handled the complaint
        var validgros = await _employeeRepository.GetGroActiveWorkloadAsync(departmentId);
        var selectedGro = validgros.FirstOrDefault((gro) => gro.EmployeeId.HasValue && !previoushandlers.Contains(gro.EmployeeId.Value));

        // gro found so escalation level is 0
        if (selectedGro != null)
        {
            return (selectedGro.EmployeeId!.Value, 0);
        }
        //dept head next level  ie level 1
        var departmentHead = await _employeeRepository.GetDepartmentHeadAsync(departmentId);
        if (departmentHead != null)
        {
            return (departmentHead.EmployeeId, 1);
        }
        // admin next level is level 2
        var admin = await _employeeRepository.GetAdminAsync();
        if (admin == null)
        {
            throw new BusinessRuleException("No active admin in the system");
        }
        return (admin.EmployeeId, 2);

    }



}