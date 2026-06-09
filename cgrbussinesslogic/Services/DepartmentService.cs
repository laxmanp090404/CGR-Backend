    using cgrbussinesslogic.Interfaces;
    using cgrdataaccesslibrary.Interfaces;
    using cgrmodellibrary.DTOs.Department;
    using cgrmodellibrary.Exceptions;
    using cgrmodellibrary.Models;

    namespace cgrbussinesslogic.Services;

    public class DepartmentService : IDepartmentService
    {
        private const short ROLE_DEPARTMENT_HEAD = 3;
        private const short ROLE_ADMIN = 4;
        private const short ROLE_EMPLOYEE = 1;


        private readonly IDepartmentRepository _departmentRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ICurrentUserService _currentUserService;

        public DepartmentService(
            IDepartmentRepository departmentRepository,
            IEmployeeRepository employeeRepository,
            ICurrentUserService currentUserService)
        {
            _departmentRepository = departmentRepository;
            _employeeRepository = employeeRepository;
            _currentUserService = currentUserService;
        }

        public async Task<IEnumerable<DepartmentDto>> GetAllAsync(bool? isActive)
        {

            var departments = await _departmentRepository.GetAllAsync(isActive);

            return departments.Select(MapToDto);
        }

        public async Task<DepartmentDto> GetByIdAsync(int departmentId)
        {

            var department = await _departmentRepository.GetDetailByIdAsync(departmentId)
                ?? throw new NotFoundException($"Department {departmentId} not found.");

            return MapToDto(department);
        }

        public async Task<DepartmentDto> CreateAsync(CreateDepartmentDto dto)
        {
            ValidateAdmin();

            var existing = await _departmentRepository.GetByNameAsync(dto.DepartmentName);

            if (existing != null)
            {
                throw new ConflictException($"Department '{dto.DepartmentName}' already exists.");
            }

            await ValidateDepartmentHeadAsync(
                dto.DepartmentHeadEmployeeId);

            var department = new Department
            {
                DepartmentName = dto.DepartmentName.Trim(),
                DepartmentHeadEmployeeId = dto.DepartmentHeadEmployeeId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var created = await _departmentRepository.Create(department);

            if (dto.DepartmentHeadEmployeeId.HasValue)
            {
                var employee =
                    await _employeeRepository.Get(
                        dto.DepartmentHeadEmployeeId.Value)
                    ?? throw new NotFoundException(
                        $"Employee {dto.DepartmentHeadEmployeeId.Value}");

                employee.RoleId = ROLE_DEPARTMENT_HEAD;
                employee.UpdatedAt = DateTime.UtcNow;
                employee.DepartmentId = created.DepartmentId;

                await _employeeRepository.Update(
                    employee,
                    employee.EmployeeId);
            }

            var reloaded =
                await _departmentRepository.GetDetailByIdAsync(
                    created.DepartmentId)
                ?? throw new NotFoundException(
                    "Unable to reload created department.");

            return MapToDto(reloaded);
        }

        public async Task<DepartmentDto> UpdateAsync(int departmentId, UpdateDepartmentDto dto)
        {
            ValidateAdmin();

            var department = await _departmentRepository.Get(departmentId) ?? throw new NotFoundException($"Department {departmentId} not found.");

            var existing = await _departmentRepository.GetByNameAsync(dto.DepartmentName);

            if (existing != null && existing.DepartmentId != departmentId)
            {
                throw new ConflictException($"Department '{dto.DepartmentName}' already exists.");
            }

            if (!dto.IsActive)
            {
                var hasActiveCategories =
                    await _departmentRepository
                        .HasActiveCategoriesAsync(departmentId);

                if (hasActiveCategories)
                {
                    throw new BusinessRuleException("Cannot deactivate a department that contains active categories.");
                }
            }

            await ValidateDepartmentHeadAsync(dto.DepartmentHeadEmployeeId, departmentId);

            var previousHeadId = department.DepartmentHeadEmployeeId;
            department.DepartmentName = dto.DepartmentName.Trim();

            department.DepartmentHeadEmployeeId = dto.DepartmentHeadEmployeeId;

            if (!dto.IsActive && department.DepartmentHeadEmployeeId.HasValue)
            {
                throw new BusinessRuleException(
                    "Remove Department Head before deactivating department.");
            }
            department.IsActive = dto.IsActive;

            department.UpdatedAt =
                DateTime.UtcNow;

            await _departmentRepository.Update(
                department,
                departmentId);
            if (previousHeadId.HasValue &&
        previousHeadId != dto.DepartmentHeadEmployeeId)
            {
                var oldHead =
                    await _employeeRepository.Get(
                        previousHeadId.Value);

                if (oldHead != null)
                {
                    oldHead.RoleId = ROLE_EMPLOYEE;
                    oldHead.UpdatedAt = DateTime.UtcNow;
                    oldHead.DepartmentId = null;

                    await _employeeRepository.Update(
                        oldHead,
                        oldHead.EmployeeId);
                }
            }
            if (dto.DepartmentHeadEmployeeId.HasValue &&
        previousHeadId != dto.DepartmentHeadEmployeeId)
            {
                var newHead =
                    await _employeeRepository.Get(
                        dto.DepartmentHeadEmployeeId.Value)
                    ?? throw new NotFoundException(
                        $"Employee {dto.DepartmentHeadEmployeeId.Value}");

                newHead.RoleId = ROLE_DEPARTMENT_HEAD;
                newHead.UpdatedAt = DateTime.UtcNow;
                newHead.DepartmentId = departmentId;

                await _employeeRepository.Update(
                    newHead,
                    newHead.EmployeeId);
            }
            var reloaded =
                await _departmentRepository.GetDetailByIdAsync(
                    departmentId)
                ?? throw new NotFoundException(
                    $"Department {departmentId} not found.");

            return MapToDto(reloaded);
        }

        // whether he is active,whether he is employee and whether already assigned as department head of another department
        private async Task ValidateDepartmentHeadAsync(
        int? employeeId,
        int? departmentId = null)
        {
            if (!employeeId.HasValue)
            {
                return;
            }

            var employee = await _employeeRepository.GetByIdWithRoleAsync(employeeId.Value) ?? throw new NotFoundException($"Employee {employeeId.Value} not found.");
            bool isCurrentHead = employee.RoleId == ROLE_DEPARTMENT_HEAD && employee.DepartmentId == departmentId;


            if (!employee.IsActive)
            {
                throw new BusinessRuleException("Only active employees can be assigned as Department Heads.");
            }

            if (!isCurrentHead && employee.RoleId != ROLE_EMPLOYEE)
            {
                throw new BusinessRuleException("Only employees with EMPLOYEE role can be assigned as Department Heads.");
            }

            var alreadyAssigned = await _departmentRepository.IsDepartmentHeadAssignedAsync(
                        employeeId.Value,
                        departmentId);

            if (alreadyAssigned)
            {
                throw new BusinessRuleException("This employee is already assigned as Department Head of another department.");
            }
        }
        // check if the current user is admin
        private void ValidateAdmin()
        {
            if (!_currentUserService.IsAuthenticated)
            {
                throw new UnauthorizedAccessException();
            }

            if (_currentUserService.RoleId != ROLE_ADMIN)
            {
                throw new ForbiddenException(
                    "Only admins can manage departments.");
            }
        }
        //mapping func
        private static DepartmentDto MapToDto(
            Department department)
        {
            return new DepartmentDto
            {
                DepartmentId = department.DepartmentId,
                DepartmentName = department.DepartmentName,
                DepartmentHeadEmployeeId =
                    department.DepartmentHeadEmployeeId,
                DepartmentHeadEmployeeName =
                    department.DepartmentHeadEmployee?.EmployeeName,
                IsActive = department.IsActive,
                CreatedAt = department.CreatedAt
            };
        }
    }