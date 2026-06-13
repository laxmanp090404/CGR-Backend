#pragma warning disable CS8620, CS8625

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using cgrbussinesslogic.Interfaces;
using cgrbussinesslogic.Services;
using cgrdataaccesslibrary.Context;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.DTOs.Common;
using cgrmodellibrary.DTOs.RoleRequest;
using cgrmodellibrary.Exceptions;
using cgrmodellibrary.Models;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace cgrtest.Services;

[TestFixture]
public class RoleRequestServiceTests
{
    private Mock<IRoleRequestRepository> _mockRoleRequestRepo = null!;
    private Mock<IEmployeeRepository> _mockEmployeeRepo = null!;
    private Mock<IRepository<int, Department>> _mockDepartmentRepo = null!;
    private CGRContext _context = null!;
    private RoleRequestService _serviceSut = null!;

    private const short ROLE_EMPLOYEE = 1;
    private const short ROLE_GRO = 2;
    private const short ROLE_DEPT_HEAD = 3;
    private const short ROLE_ADMIN = 4;

    private const short REQUEST_STATUS_PENDING = 1;
    private const short REQUEST_STATUS_APPROVED = 2;
    private const short REQUEST_STATUS_REJECTED = 3;

    [SetUp]
    public void SetUp()
    {
        _mockRoleRequestRepo = new Mock<IRoleRequestRepository>();
        _mockEmployeeRepo = new Mock<IEmployeeRepository>();
        _mockDepartmentRepo = new Mock<IRepository<int, Department>>();

        var options = new DbContextOptionsBuilder<CGRContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w =>
                w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _context = new CGRContext(options);

        _serviceSut = new RoleRequestService(
            _mockRoleRequestRepo.Object,
            _mockEmployeeRepo.Object,
            _mockDepartmentRepo.Object,
            _context
        );
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    #region CreateAsync Tests

    [Test]
    public void CreateAsync_InvalidRoleUpgradePath_ThrowsBusinessRuleException()
    {
        // Arrange
        var employee = new Employee { EmployeeId = 1, RoleId = ROLE_EMPLOYEE };
        _mockEmployeeRepo.Setup(r => r.Get(1)).ReturnsAsync(employee);

        var dto = new CreateRoleRequestDto { RequestedRoleId = ROLE_DEPT_HEAD }; // Invalid path (EMPLOYEE -> DEPT_HEAD directly is invalid)

        // Act & Assert
        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            _serviceSut.CreateAsync(dto, 1, "EMPLOYEE"));
        Assert.That(ex!.Message, Is.EqualTo("Invalid role upgrade path. Only EMPLOYEE to GRO or GRO to DEPARTMENT_HEAD upgrades are allowed."));
    }

    [Test]
    public void CreateAsync_PendingRequestExists_ThrowsConflictException()
    {
        // Arrange
        var employee = new Employee { EmployeeId = 1, RoleId = ROLE_EMPLOYEE };
        _mockEmployeeRepo.Setup(r => r.Get(1)).ReturnsAsync(employee);

        _mockRoleRequestRepo.Setup(r => r.GetPendingByEmployeeIdAsync(1))
            .ReturnsAsync(new RoleRequest());

        var dto = new CreateRoleRequestDto { RequestedRoleId = ROLE_GRO };

        // Act & Assert
        var ex = Assert.ThrowsAsync<ConflictException>(() =>
            _serviceSut.CreateAsync(dto, 1, "EMPLOYEE"));
        Assert.That(ex!.Message, Is.EqualTo("You already have a pending role upgrade request."));
    }

    [Test]
    public async Task CreateAsync_ValidUpgradePath_CreatesAndReturnsDto()
    {
        // Arrange
        var employee = new Employee { EmployeeId = 1, RoleId = ROLE_EMPLOYEE, EmployeeName = "Test Emp" };
        _mockEmployeeRepo.Setup(r => r.Get(1)).ReturnsAsync(employee);

        _mockRoleRequestRepo.Setup(r => r.GetPendingByEmployeeIdAsync(1))
            .ReturnsAsync((RoleRequest?)null);

        var createdRequest = new RoleRequest
        {
            RoleRequestId = 10,
            EmployeeId = 1,
            CurrentRoleId = ROLE_EMPLOYEE,
            RequestedRoleId = ROLE_GRO,
            RequestStatusId = REQUEST_STATUS_PENDING
        };

        _mockRoleRequestRepo.Setup(r => r.Create(It.IsAny<RoleRequest>()))
            .ReturnsAsync(createdRequest);

        // MapToDto lookup
        var requestsList = new List<RoleRequest>
        {
            new()
            {
                RoleRequestId = 10,
                EmployeeId = 1,
                CurrentRoleId = ROLE_EMPLOYEE,
                RequestedRoleId = ROLE_GRO,
                RequestStatusId = REQUEST_STATUS_PENDING,
                Employee = employee,
                CurrentRole = new Role { RoleName = "EMPLOYEE" },
                RequestedRole = new Role { RoleName = "GRO" },
                RequestStatus = new RequestStatus { StatusName = "Pending" }
            }
        };
        _mockRoleRequestRepo.Setup(r => r.GetByEmployeeIdAsync(1))
            .ReturnsAsync(requestsList);

        var dto = new CreateRoleRequestDto { RequestedRoleId = ROLE_GRO };

        // Act
        var result = await _serviceSut.CreateAsync(dto, 1, "EMPLOYEE");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.RoleRequestId, Is.EqualTo(10));
        Assert.That(result.CurrentRoleName, Is.EqualTo("EMPLOYEE"));
        Assert.That(result.RequestedRoleName, Is.EqualTo("GRO"));
        Assert.That(result.RequestStatusName, Is.EqualTo("Pending"));
        Assert.That(result.EmployeeName, Is.EqualTo("Test Emp"));

        _mockRoleRequestRepo.Verify(r => r.Create(It.Is<RoleRequest>(req =>
            req.EmployeeId == 1 &&
            req.CurrentRoleId == ROLE_EMPLOYEE &&
            req.RequestedRoleId == ROLE_GRO &&
            req.RequestStatusId == REQUEST_STATUS_PENDING
        )), Times.Once);
    }

    #endregion

    #region RejectAsync Tests

    [Test]
    public void RejectAsync_RequestNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _mockRoleRequestRepo.Setup(r => r.Get(1)).ReturnsAsync((RoleRequest?)null);

        // Act & Assert
        Assert.ThrowsAsync<NotFoundException>(() =>
            _serviceSut.RejectAsync(1, "Remarks", 99));
    }

    [TestCase(REQUEST_STATUS_APPROVED)]
    [TestCase(REQUEST_STATUS_REJECTED)]
    public void RejectAsync_AlreadyReviewed_ThrowsBusinessRuleException(short statusId)
    {
        // Arrange
        var request = new RoleRequest { RoleRequestId = 1, RequestStatusId = statusId };
        _mockRoleRequestRepo.Setup(r => r.Get(1)).ReturnsAsync((RoleRequest?)request);

        // Act & Assert
        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            _serviceSut.RejectAsync(1, "Remarks", 99));
        Assert.That(ex!.Message, Is.EqualTo("This request has already been reviewed."));
    }

    [Test]
    public async Task RejectAsync_PendingRequest_RejectsAndReturnsDto()
    {
        // Arrange
        var request = new RoleRequest
        {
            RoleRequestId = 1,
            EmployeeId = 5,
            RequestStatusId = REQUEST_STATUS_PENDING
        };
        _mockRoleRequestRepo.Setup(r => r.Get(1)).ReturnsAsync((RoleRequest?)request);

        var employee = new Employee { EmployeeId = 5, EmployeeName = "Alice" };
        var list = new List<RoleRequest>
        {
            new()
            {
                RoleRequestId = 1,
                EmployeeId = 5,
                RequestStatusId = REQUEST_STATUS_REJECTED,
                Remarks = "Rejected remarks",
                Employee = employee,
                RequestStatus = new RequestStatus { StatusName = "Rejected" }
            }
        };
        _mockRoleRequestRepo.Setup(r => r.GetByEmployeeIdAsync(5)).ReturnsAsync(list);

        // Act
        var result = await _serviceSut.RejectAsync(1, "Rejected remarks", 99);

        // Assert
        Assert.That(request.RequestStatusId, Is.EqualTo(REQUEST_STATUS_REJECTED));
        Assert.That(request.ReviewedBy, Is.EqualTo(99));
        Assert.That(request.Remarks, Is.EqualTo("Rejected remarks"));

        _mockRoleRequestRepo.Verify(r => r.Update(request, 1), Times.Once);
        Assert.That(result.RequestStatusName, Is.EqualTo("Rejected"));
        Assert.That(result.Remarks, Is.EqualTo("Rejected remarks"));
    }

    #endregion

    #region GetPagedAsync and GetMyRequestsAsync Tests

    [Test]
    public async Task GetPagedAsync_CallsRepositoryAndMapsToDto()
    {
        // Arrange
        var list = new List<RoleRequest>
        {
            new()
            {
                RoleRequestId = 1,
                EmployeeId = 5,
                Employee = new Employee { EmployeeName = "Alice" },
                CurrentRole = new Role { RoleName = "EMPLOYEE" },
                RequestedRole = new Role { RoleName = "GRO" },
                RequestStatus = new RequestStatus { StatusName = "Pending" }
            }
        };
        _mockRoleRequestRepo.Setup(r => r.GetPagedAsync(1, 10, REQUEST_STATUS_PENDING))
            .ReturnsAsync((list, 1));

        // Act
        var result = await _serviceSut.GetPagedAsync(1, 10, REQUEST_STATUS_PENDING);

        // Assert
        var itemsList = result.Items.ToList();
        Assert.That(itemsList, Has.Count.EqualTo(1));
        Assert.That(result.TotalCount, Is.EqualTo(1));
        Assert.That(itemsList[0].EmployeeName, Is.EqualTo("Alice"));
        Assert.That(itemsList[0].CurrentRoleName, Is.EqualTo("EMPLOYEE"));
        Assert.That(itemsList[0].RequestedRoleName, Is.EqualTo("GRO"));
        Assert.That(itemsList[0].RequestStatusName, Is.EqualTo("Pending"));
    }

    [Test]
    public async Task GetMyRequestsAsync_CallsRepositoryAndMapsToDto()
    {
        // Arrange
        var list = new List<RoleRequest>
        {
            new()
            {
                RoleRequestId = 1,
                EmployeeId = 5,
                Employee = new Employee { EmployeeName = "Alice" },
                CurrentRole = new Role { RoleName = "EMPLOYEE" },
                RequestedRole = new Role { RoleName = "GRO" },
                RequestStatus = new RequestStatus { StatusName = "Pending" }
            }
        };
        _mockRoleRequestRepo.Setup(r => r.GetByEmployeeIdAsync(5)).ReturnsAsync(list);

        // Act
        var result = (await _serviceSut.GetMyRequestsAsync(5)).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].EmployeeName, Is.EqualTo("Alice"));
    }

    #endregion

    #region ApproveAsync Tests

    [Test]
    public void ApproveAsync_RequestNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _mockRoleRequestRepo.Setup(r => r.Get(1)).ReturnsAsync((RoleRequest?)null);

        // Act & Assert
        Assert.ThrowsAsync<NotFoundException>(() =>
            _serviceSut.ApproveAsync(1, new ApproveRoleRequestDto(), 99));
    }

    [TestCase(REQUEST_STATUS_APPROVED)]
    [TestCase(REQUEST_STATUS_REJECTED)]
    public void ApproveAsync_AlreadyReviewed_ThrowsBusinessRuleException(short statusId)
    {
        // Arrange
        var request = new RoleRequest { RoleRequestId = 1, RequestStatusId = statusId };
        _mockRoleRequestRepo.Setup(r => r.Get(1)).ReturnsAsync((RoleRequest?)request);

        // Act & Assert
        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            _serviceSut.ApproveAsync(1, new ApproveRoleRequestDto(), 99));
        Assert.That(ex!.Message, Is.EqualTo("This request has already been reviewed."));
    }

    [Test]
    public void ApproveAsync_EmployeeHasNoDepartmentAndAssignDepartmentIdNull_ThrowsValidationException()
    {
        // Arrange
        var request = new RoleRequest
        {
            RoleRequestId = 1,
            EmployeeId = 5,
            RequestStatusId = REQUEST_STATUS_PENDING
        };
        _mockRoleRequestRepo.Setup(r => r.Get(1)).ReturnsAsync((RoleRequest?)request);

        var employee = new Employee { EmployeeId = 5, DepartmentId = null };
        _mockEmployeeRepo.Setup(r => r.Get(5)).ReturnsAsync(employee);

        var dto = new ApproveRoleRequestDto { AssignDepartmentId = null }; // Missing department assignment

        // Act & Assert
        var ex = Assert.ThrowsAsync<ValidationException>(() =>
            _serviceSut.ApproveAsync(1, dto, 99));
        Assert.That(ex!.Message, Is.EqualTo("Department assignment is required."));
    }

    [Test]
    public void ApproveAsync_RequestedRoleDeptHeadAndDepartmentAlreadyHasHead_ThrowsBusinessRuleException()
    {
        // Arrange
        var request = new RoleRequest
        {
            RoleRequestId = 1,
            EmployeeId = 5,
            RequestedRoleId = ROLE_DEPT_HEAD,
            RequestStatusId = REQUEST_STATUS_PENDING
        };
        _mockRoleRequestRepo.Setup(r => r.Get(1)).ReturnsAsync((RoleRequest?)request);

        var employee = new Employee { EmployeeId = 5, DepartmentId = 3 };
        _mockEmployeeRepo.Setup(r => r.Get(5)).ReturnsAsync(employee);

        var department = new Department { DepartmentId = 3, DepartmentHeadEmployeeId = 10 }; // Already has head employee 10
        _mockDepartmentRepo.Setup(r => r.Get(3)).ReturnsAsync(department);

        var dto = new ApproveRoleRequestDto();

        // Act & Assert
        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            _serviceSut.ApproveAsync(1, dto, 99));
        Assert.That(ex!.Message, Is.EqualTo("Department already has a department head."));
    }

    [Test]
    public void ApproveAsync_EmployeeIsAlreadyHeadOfAnotherDepartment_ThrowsBusinessRuleException()
    {
        // Arrange
        var request = new RoleRequest
        {
            RoleRequestId = 1,
            EmployeeId = 5,
            RequestedRoleId = ROLE_DEPT_HEAD,
            RequestStatusId = REQUEST_STATUS_PENDING
        };
        _mockRoleRequestRepo.Setup(r => r.Get(1)).ReturnsAsync((RoleRequest?)request);

        var employee = new Employee { EmployeeId = 5, DepartmentId = 3 };
        _mockEmployeeRepo.Setup(r => r.Get(5)).ReturnsAsync(employee);

        var department = new Department { DepartmentId = 3, DepartmentHeadEmployeeId = null };
        _mockDepartmentRepo.Setup(r => r.Get(3)).ReturnsAsync(department);

        var departmentsList = new List<Department>
        {
            department,
            new() { DepartmentId = 4, DepartmentHeadEmployeeId = 5 } // Employee 5 is head of department 4
        };
        _mockDepartmentRepo.Setup(r => r.GetAll()).ReturnsAsync(departmentsList);

        var dto = new ApproveRoleRequestDto();

        // Act & Assert
        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            _serviceSut.ApproveAsync(1, dto, 99));
        Assert.That(ex!.Message, Is.EqualTo("Employee is already department head of another department."));
    }

    [Test]
    public async Task ApproveAsync_RequestRoleDeptHead_ApprovesAndUpdatesEmployeeAndDepartment()
    {
        // Arrange
        var request = new RoleRequest
        {
            RoleRequestId = 1,
            EmployeeId = 5,
            RequestedRoleId = ROLE_DEPT_HEAD,
            RequestStatusId = REQUEST_STATUS_PENDING
        };
        _mockRoleRequestRepo.Setup(r => r.Get(1)).ReturnsAsync((RoleRequest?)request);

        var employee = new Employee { EmployeeId = 5, DepartmentId = 3, RoleId = ROLE_GRO };
        _mockEmployeeRepo.Setup(r => r.Get(5)).ReturnsAsync(employee);

        var department = new Department { DepartmentId = 3, DepartmentHeadEmployeeId = null };
        _mockDepartmentRepo.Setup(r => r.Get(3)).ReturnsAsync(department);

        var departmentsList = new List<Department> { department };
        _mockDepartmentRepo.Setup(r => r.GetAll()).ReturnsAsync(departmentsList);

        var reloadedList = new List<RoleRequest>
        {
            new()
            {
                RoleRequestId = 1,
                EmployeeId = 5,
                RequestStatusId = REQUEST_STATUS_APPROVED,
                Remarks = "Approved dept head!",
                Employee = employee,
                RequestStatus = new RequestStatus { StatusName = "Approved" }
            }
        };
        _mockRoleRequestRepo.Setup(r => r.GetByEmployeeIdAsync(5)).ReturnsAsync(reloadedList);

        var dto = new ApproveRoleRequestDto { Remarks = "Approved dept head!" };

        // Act
        var result = await _serviceSut.ApproveAsync(1, dto, 99);

        // Assert
        Assert.That(employee.RoleId, Is.EqualTo(ROLE_DEPT_HEAD));
        Assert.That(department.DepartmentHeadEmployeeId, Is.EqualTo(5));
        Assert.That(request.RequestStatusId, Is.EqualTo(REQUEST_STATUS_APPROVED));
        Assert.That(request.ReviewedBy, Is.EqualTo(99));
        Assert.That(request.Remarks, Is.EqualTo("Approved dept head!"));

        _mockEmployeeRepo.Verify(r => r.Update(employee, 5), Times.Once);
        _mockDepartmentRepo.Verify(r => r.Update(department, 3), Times.Once);
        _mockRoleRequestRepo.Verify(r => r.Update(request, 1), Times.Once);
        Assert.That(result.RequestStatusName, Is.EqualTo("Approved"));
    }

    [Test]
    public async Task ApproveAsync_RequestRoleGroWithDepartmentAssignment_Succeeds()
    {
        // Arrange
        var request = new RoleRequest
        {
            RoleRequestId = 1,
            EmployeeId = 5,
            RequestedRoleId = ROLE_GRO,
            RequestStatusId = REQUEST_STATUS_PENDING
        };
        _mockRoleRequestRepo.Setup(r => r.Get(1)).ReturnsAsync((RoleRequest?)request);

        var employee = new Employee { EmployeeId = 5, DepartmentId = null, RoleId = ROLE_EMPLOYEE };
        _mockEmployeeRepo.Setup(r => r.Get(5)).ReturnsAsync(employee);

        var department = new Department { DepartmentId = 3 };
        _mockDepartmentRepo.Setup(r => r.Get(3)).ReturnsAsync(department);

        var reloadedList = new List<RoleRequest>
        {
            new()
            {
                RoleRequestId = 1,
                EmployeeId = 5,
                RequestStatusId = REQUEST_STATUS_APPROVED,
                Employee = employee,
                RequestStatus = new RequestStatus { StatusName = "Approved" }
            }
        };
        _mockRoleRequestRepo.Setup(r => r.GetByEmployeeIdAsync(5)).ReturnsAsync(reloadedList);

        var dto = new ApproveRoleRequestDto { AssignDepartmentId = 3 };

        // Act
        await _serviceSut.ApproveAsync(1, dto, 99);

        // Assert
        Assert.That(employee.RoleId, Is.EqualTo(ROLE_GRO));
        Assert.That(employee.DepartmentId, Is.EqualTo(3));
        _mockEmployeeRepo.Verify(r => r.Update(employee, 5), Times.Once);
        // Department head should not be modified
        _mockDepartmentRepo.Verify(r => r.Update(It.IsAny<Department>(), It.IsAny<int>()), Times.Never);
    }

    [Test]
    public void ApproveAsync_ExceptionThrown_RollsBackTransaction()
    {
        // Arrange
        var request = new RoleRequest
        {
            RoleRequestId = 1,
            EmployeeId = 5,
            RequestedRoleId = ROLE_GRO,
            RequestStatusId = REQUEST_STATUS_PENDING
        };
        _mockRoleRequestRepo.Setup(r => r.Get(1)).ReturnsAsync((RoleRequest?)request);

        var employee = new Employee { EmployeeId = 5, DepartmentId = 3 };
        _mockEmployeeRepo.Setup(r => r.Get(5)).ReturnsAsync(employee);

        // Force exception
        _mockDepartmentRepo.Setup(r => r.Get(3)).ThrowsAsync(new InvalidOperationException("DB Crash"));

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _serviceSut.ApproveAsync(1, new ApproveRoleRequestDto(), 99));
    }

    #endregion

    #region ManualRoleChangeAsync Tests

    [Test]
    public void ManualRoleChangeAsync_EmployeeIsAdmin_ThrowsBusinessRuleException()
    {
        // Arrange
        var employee = new Employee { EmployeeId = 5, RoleId = ROLE_ADMIN };
        _mockEmployeeRepo.Setup(r => r.Get(5)).ReturnsAsync(employee);

        var dto = new ManualRoleChangeDto { TargetRoleId = ROLE_GRO };

        // Act & Assert
        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            _serviceSut.ManualRoleChangeAsync(5, dto, 99));
        Assert.That(ex!.Message, Is.EqualTo("Admin role cannot be changed."));
    }

    [Test]
    public void ManualRoleChangeAsync_TargetIsAdmin_ThrowsBusinessRuleException()
    {
        // Arrange
        var employee = new Employee { EmployeeId = 5, RoleId = ROLE_EMPLOYEE };
        _mockEmployeeRepo.Setup(r => r.Get(5)).ReturnsAsync(employee);

        var dto = new ManualRoleChangeDto { TargetRoleId = ROLE_ADMIN };

        // Act & Assert
        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            _serviceSut.ManualRoleChangeAsync(5, dto, 99));
        Assert.That(ex!.Message, Is.EqualTo("Cannot assign ADMIN role."));
    }

    [Test]
    public void ManualRoleChangeAsync_TargetSameAsCurrentRole_ThrowsValidationException()
    {
        // Arrange
        var employee = new Employee { EmployeeId = 5, RoleId = ROLE_GRO };
        _mockEmployeeRepo.Setup(r => r.Get(5)).ReturnsAsync(employee);

        var dto = new ManualRoleChangeDto { TargetRoleId = ROLE_GRO };

        // Act & Assert
        var ex = Assert.ThrowsAsync<ValidationException>(() =>
            _serviceSut.ManualRoleChangeAsync(5, dto, 99));
        Assert.That(ex!.Message, Is.EqualTo("Employee already has this role."));
    }

    [Test]
    public void ManualRoleChangeAsync_InvalidTargetRole_ThrowsValidationException()
    {
        // Arrange
        var employee = new Employee { EmployeeId = 5, RoleId = ROLE_EMPLOYEE };
        _mockEmployeeRepo.Setup(r => r.Get(5)).ReturnsAsync(employee);

        var dto = new ManualRoleChangeDto { TargetRoleId = 999 }; // Invalid role ID

        // Act & Assert
        var ex = Assert.ThrowsAsync<ValidationException>(() =>
            _serviceSut.ManualRoleChangeAsync(5, dto, 99));
        Assert.That(ex!.Message, Is.EqualTo("Invalid target role."));
    }

    [Test]
    public void ManualRoleChangeAsync_DepartmentAssignmentRequiredButNull_ThrowsValidationException()
    {
        // Arrange
        var employee = new Employee { EmployeeId = 5, RoleId = ROLE_EMPLOYEE, DepartmentId = null };
        _mockEmployeeRepo.Setup(r => r.Get(5)).ReturnsAsync(employee);

        var dto = new ManualRoleChangeDto { TargetRoleId = ROLE_GRO, DepartmentId = null };

        // Act & Assert
        var ex = Assert.ThrowsAsync<ValidationException>(() =>
            _serviceSut.ManualRoleChangeAsync(5, dto, 99));
        Assert.That(ex!.Message, Is.EqualTo("Department assignment is required."));
    }

    [Test]
    public void ManualRoleChangeAsync_TargetRoleDeptHeadDepartmentNull_ThrowsValidationException()
    {
        // Arrange
        var employee = new Employee { EmployeeId = 5, RoleId = ROLE_EMPLOYEE, DepartmentId = null };
        _mockEmployeeRepo.Setup(r => r.Get(5)).ReturnsAsync(employee);

        var dto = new ManualRoleChangeDto { TargetRoleId = ROLE_DEPT_HEAD, DepartmentId = null };

        // Act & Assert
        var ex = Assert.ThrowsAsync<ValidationException>(() =>
            _serviceSut.ManualRoleChangeAsync(5, dto, 99));
        Assert.That(ex!.Message, Is.EqualTo("Department assignment is required."));
    }

    [Test]
    public void ManualRoleChangeAsync_TargetDeptHeadDepartmentAlreadyHasHead_ThrowsBusinessRuleException()
    {
        // Arrange
        var employee = new Employee { EmployeeId = 5, RoleId = ROLE_EMPLOYEE, DepartmentId = 3 };
        _mockEmployeeRepo.Setup(r => r.Get(5)).ReturnsAsync(employee);

        var department = new Department { DepartmentId = 3, DepartmentHeadEmployeeId = 10 }; // Already has head employee 10
        _mockDepartmentRepo.Setup(r => r.Get(3)).ReturnsAsync(department);

        var dto = new ManualRoleChangeDto { TargetRoleId = ROLE_DEPT_HEAD };

        // Act & Assert
        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            _serviceSut.ManualRoleChangeAsync(5, dto, 99));
        Assert.That(ex!.Message, Is.EqualTo("Department already has a department head."));
    }

    [Test]
    public void ManualRoleChangeAsync_TargetDeptHeadAlreadyHeadOfAnotherDepartment_ThrowsBusinessRuleException()
    {
        // Arrange
        var employee = new Employee { EmployeeId = 5, RoleId = ROLE_EMPLOYEE, DepartmentId = 3 };
        _mockEmployeeRepo.Setup(r => r.Get(5)).ReturnsAsync(employee);

        var department = new Department { DepartmentId = 3, DepartmentHeadEmployeeId = null };
        _mockDepartmentRepo.Setup(r => r.Get(3)).ReturnsAsync(department);

        var departmentsList = new List<Department>
        {
            department,
            new() { DepartmentId = 4, DepartmentHeadEmployeeId = 5 } // Employee 5 is already head of department 4
        };
        _mockDepartmentRepo.Setup(r => r.GetAll()).ReturnsAsync(departmentsList);

        var dto = new ManualRoleChangeDto { TargetRoleId = ROLE_DEPT_HEAD };

        // Act & Assert
        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            _serviceSut.ManualRoleChangeAsync(5, dto, 99));
        Assert.That(ex!.Message, Is.EqualTo("Employee is already department head of another department."));
    }

    [Test]
    public async Task ManualRoleChangeAsync_DemotingFromDeptHead_ClearsDepartmentHeadField()
    {
        // Arrange
        var employee = new Employee { EmployeeId = 5, RoleId = ROLE_DEPT_HEAD, DepartmentId = 3 };
        _mockEmployeeRepo.Setup(r => r.Get(5)).ReturnsAsync(employee);

        var department = new Department { DepartmentId = 3, DepartmentHeadEmployeeId = 5 };
        _mockDepartmentRepo.Setup(r => r.Get(3)).ReturnsAsync(department);

        var departmentsList = new List<Department> { department };
        _mockDepartmentRepo.Setup(r => r.GetAll()).ReturnsAsync(departmentsList);

        var createdRequest = new RoleRequest { RoleRequestId = 100 };
        _mockRoleRequestRepo.Setup(r => r.Create(It.IsAny<RoleRequest>())).ReturnsAsync(createdRequest);

        var reloadedList = new List<RoleRequest>
        {
            new()
            {
                RoleRequestId = 100,
                EmployeeId = 5,
                Employee = employee,
                RequestStatus = new RequestStatus { StatusName = "Approved" }
            }
        };
        _mockRoleRequestRepo.Setup(r => r.GetByEmployeeIdAsync(5)).ReturnsAsync(reloadedList);

        var dto = new ManualRoleChangeDto { TargetRoleId = ROLE_GRO };

        // Act
        var result = await _serviceSut.ManualRoleChangeAsync(5, dto, 99);

        // Assert
        Assert.That(employee.RoleId, Is.EqualTo(ROLE_GRO));
        Assert.That(department.DepartmentHeadEmployeeId, Is.Null); // Head cleared!

        _mockDepartmentRepo.Verify(r => r.Update(department, 3), Times.Once);
        _mockEmployeeRepo.Verify(r => r.Update(employee, 5), Times.Once);
        _mockRoleRequestRepo.Verify(r => r.Create(It.Is<RoleRequest>(req =>
            req.EmployeeId == 5 &&
            req.CurrentRoleId == ROLE_DEPT_HEAD &&
            req.RequestedRoleId == ROLE_GRO &&
            req.RequestStatusId == REQUEST_STATUS_APPROVED &&
            req.ReviewedBy == 99
        )), Times.Once);
    }

    [Test]
    public async Task ManualRoleChangeAsync_PromotingToDeptHead_UpdatesDepartmentHeadField()
    {
        // Arrange
        var employee = new Employee { EmployeeId = 5, RoleId = ROLE_GRO, DepartmentId = 3 };
        _mockEmployeeRepo.Setup(r => r.Get(5)).ReturnsAsync(employee);

        var department = new Department { DepartmentId = 3, DepartmentHeadEmployeeId = null };
        _mockDepartmentRepo.Setup(r => r.Get(3)).ReturnsAsync(department);

        var departmentsList = new List<Department> { department };
        _mockDepartmentRepo.Setup(r => r.GetAll()).ReturnsAsync(departmentsList);

        var createdRequest = new RoleRequest { RoleRequestId = 100 };
        _mockRoleRequestRepo.Setup(r => r.Create(It.IsAny<RoleRequest>())).ReturnsAsync(createdRequest);

        var reloadedList = new List<RoleRequest>
        {
            new()
            {
                RoleRequestId = 100,
                EmployeeId = 5,
                Employee = employee,
                RequestStatus = new RequestStatus { StatusName = "Approved" }
            }
        };
        _mockRoleRequestRepo.Setup(r => r.GetByEmployeeIdAsync(5)).ReturnsAsync(reloadedList);

        var dto = new ManualRoleChangeDto { TargetRoleId = ROLE_DEPT_HEAD };

        // Act
        await _serviceSut.ManualRoleChangeAsync(5, dto, 99);

        // Assert
        Assert.That(employee.RoleId, Is.EqualTo(ROLE_DEPT_HEAD));
        Assert.That(department.DepartmentHeadEmployeeId, Is.EqualTo(5));

        _mockDepartmentRepo.Verify(r => r.Update(department, 3), Times.Once);
        _mockEmployeeRepo.Verify(r => r.Update(employee, 5), Times.Once);
    }

    [Test]
    public async Task ManualRoleChangeAsync_EmployeeDepartmentNullButDtoDepartmentProvided_Succeeds()
    {
        // Arrange
        var employee = new Employee { EmployeeId = 5, RoleId = ROLE_EMPLOYEE, DepartmentId = null };
        _mockEmployeeRepo.Setup(r => r.Get(5)).ReturnsAsync(employee);

        var department = new Department { DepartmentId = 3 };
        _mockDepartmentRepo.Setup(r => r.Get(3)).ReturnsAsync(department);

        var createdRequest = new RoleRequest { RoleRequestId = 100 };
        _mockRoleRequestRepo.Setup(r => r.Create(It.IsAny<RoleRequest>())).ReturnsAsync(createdRequest);

        var reloadedList = new List<RoleRequest>
        {
            new()
            {
                RoleRequestId = 100,
                EmployeeId = 5,
                Employee = employee,
                RequestStatus = new RequestStatus { StatusName = "Approved" }
            }
        };
        _mockRoleRequestRepo.Setup(r => r.GetByEmployeeIdAsync(5)).ReturnsAsync(reloadedList);

        var dto = new ManualRoleChangeDto { TargetRoleId = ROLE_GRO, DepartmentId = 3 };

        // Act
        var result = await _serviceSut.ManualRoleChangeAsync(5, dto, 99);

        // Assert
        Assert.That(employee.RoleId, Is.EqualTo(ROLE_GRO));
        Assert.That(employee.DepartmentId, Is.EqualTo(3));
        _mockEmployeeRepo.Verify(r => r.Update(employee, 5), Times.Once);
    }

    [Test]
    public void ManualRoleChangeAsync_TargetDeptHeadButDepartmentNotFound_ThrowsValidationException()
    {
        // Arrange
        var employee = new Employee { EmployeeId = 5, RoleId = ROLE_EMPLOYEE, DepartmentId = 3 };
        _mockEmployeeRepo.Setup(r => r.Get(5)).ReturnsAsync(employee);

        // Department lookup returns null
        _mockDepartmentRepo.Setup(r => r.Get(3)).ReturnsAsync((Department?)null);

        var dto = new ManualRoleChangeDto { TargetRoleId = ROLE_DEPT_HEAD };

        // Act & Assert
        var ex = Assert.ThrowsAsync<ValidationException>(() =>
            _serviceSut.ManualRoleChangeAsync(5, dto, 99));
        Assert.That(ex!.Message, Is.EqualTo("Department assignment is required."));
    }

    [Test]
    public void ManualRoleChangeAsync_ExceptionThrown_RollsBackTransaction()
    {
        // Arrange
        var employee = new Employee { EmployeeId = 5, RoleId = ROLE_EMPLOYEE, DepartmentId = 3 };
        _mockEmployeeRepo.Setup(r => r.Get(5)).ReturnsAsync(employee);

        // Force exception
        _mockDepartmentRepo.Setup(r => r.Get(3)).ThrowsAsync(new InvalidOperationException("DB Crash"));

        var dto = new ManualRoleChangeDto { TargetRoleId = ROLE_GRO };

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _serviceSut.ManualRoleChangeAsync(5, dto, 99));
    }

    #endregion
}
