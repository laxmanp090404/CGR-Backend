using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using cgrbussinesslogic.Interfaces;
using cgrbussinesslogic.Services;
using cgrdataaccesslibrary.Context;
using cgrdataaccesslibrary.Interfaces;
using cgrdataaccesslibrary.Repositories;
using cgrmodellibrary.DTOs.Analytics;
using cgrmodellibrary.Exceptions;
using cgrmodellibrary.Models;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace cgrtest.Services;

[TestFixture]
public class AnalyticsServiceTests
{
    private const short ROLE_EMPLOYEE = 1;
    private const short ROLE_GRO = 2;
    private const short ROLE_DEPARTMENT_HEAD = 3;
    private const short ROLE_ADMIN = 4;

    private const short STATUS_SUBMITTED = 1;
    private const short STATUS_ASSIGNED = 2;
    private const short STATUS_IN_PROGRESS = 3;
    private const short STATUS_ESCALATED = 4;
    private const short STATUS_RESOLVED = 5;
    private const short STATUS_CLOSED = 6;
    private const short STATUS_REJECTED = 7;
    private const short STATUS_REOPENED = 8;
    private const short STATUS_EXTERNALLY_ESCALATED = 9;

    private Mock<IAnalyticsRepository> _mockRepo = null!;
    private Mock<ICurrentUserService> _mockUserService = null!;
    private AnalyticsService _serviceSut = null!;

    [SetUp]
    public void SetUp()
    {
        _mockRepo = new Mock<IAnalyticsRepository>();
        _mockUserService = new Mock<ICurrentUserService>();
        _serviceSut = new AnalyticsService(_mockRepo.Object, _mockUserService.Object);
    }

    private void SetupUser(short roleId, int employeeId = 1, int? departmentId = 10)
    {
        _mockUserService.Setup(u => u.RoleId).Returns(roleId);
        _mockUserService.Setup(u => u.EmployeeId).Returns(employeeId);
        _mockUserService.Setup(u => u.DepartmentId).Returns(departmentId);
    }

    #region Service Authorization Tests

    [Test]
    public void GetAdminDashboardAsync_NonAdmin_ThrowsForbiddenException()
    {
        SetupUser(ROLE_GRO);
        Assert.ThrowsAsync<ForbiddenException>(() => _serviceSut.GetAdminDashboardAsync());
    }

    [Test]
    public async Task GetAdminDashboardAsync_Admin_CallsRepository()
    {
        SetupUser(ROLE_ADMIN);
        _mockRepo.Setup(r => r.GetAdminDashboardAsync()).ReturnsAsync(new AdminDashboardDto());
        var result = await _serviceSut.GetAdminDashboardAsync();
        Assert.That(result, Is.Not.Null);
        _mockRepo.Verify(r => r.GetAdminDashboardAsync(), Times.Once);
    }

    [Test]
    public void GetMyDashboardAsync_Admin_ThrowsForbiddenException()
    {
        SetupUser(ROLE_ADMIN);
        Assert.ThrowsAsync<ForbiddenException>(() => _serviceSut.GetMyDashboardAsync());
    }

    [Test]
    public async Task GetMyDashboardAsync_NonAdmin_CallsRepository()
    {
        SetupUser(ROLE_EMPLOYEE, employeeId: 42);
        _mockRepo.Setup(r => r.GetMyDashboardAsync(42)).ReturnsAsync(new MyDashboardDto());
        var result = await _serviceSut.GetMyDashboardAsync();
        Assert.That(result, Is.Not.Null);
        _mockRepo.Verify(r => r.GetMyDashboardAsync(42), Times.Once);
    }

    [Test]
    public void GetGroDashboardAsync_NonGro_ThrowsForbiddenException()
    {
        SetupUser(ROLE_EMPLOYEE);
        Assert.ThrowsAsync<ForbiddenException>(() => _serviceSut.GetGroDashboardAsync());
    }

    [Test]
    public async Task GetGroDashboardAsync_Gro_CallsRepository()
    {
        SetupUser(ROLE_GRO, employeeId: 23);
        _mockRepo.Setup(r => r.GetGroDashboardAsync(23)).ReturnsAsync(new GroDashboardDto());
        var result = await _serviceSut.GetGroDashboardAsync();
        Assert.That(result, Is.Not.Null);
        _mockRepo.Verify(r => r.GetGroDashboardAsync(23), Times.Once);
    }

    [Test]
    public void GetDepartmentDashboardAsync_NonAdminNonDeptHead_ThrowsForbiddenException()
    {
        SetupUser(ROLE_GRO);
        Assert.ThrowsAsync<ForbiddenException>(() => _serviceSut.GetDepartmentDashboardAsync());
    }

    [Test]
    public async Task GetDepartmentDashboardAsync_AdminOrDeptHead_CallsRepository()
    {
        SetupUser(ROLE_DEPARTMENT_HEAD, departmentId: 12);
        _mockRepo.Setup(r => r.GetDepartmentDashboardsAsync(12, false))
            .ReturnsAsync(new List<DepartmentDashboardDto>());

        var result = await _serviceSut.GetDepartmentDashboardAsync();
        Assert.That(result, Is.Not.Null);
        _mockRepo.Verify(r => r.GetDepartmentDashboardsAsync(12, false), Times.Once);
    }

    [Test]
    public async Task GetStatusDistributionAsync_DelegatesToRepository()
    {
        SetupUser(ROLE_EMPLOYEE, employeeId: 10, departmentId: 20);
        _mockRepo.Setup(r => r.GetStatusDistributionAsync(ROLE_EMPLOYEE, 10, 20))
            .ReturnsAsync(new List<StatusDistributionDto>());

        var result = await _serviceSut.GetStatusDistributionAsync();
        Assert.That(result, Is.Not.Null);
        _mockRepo.Verify(r => r.GetStatusDistributionAsync(ROLE_EMPLOYEE, 10, 20), Times.Once);
    }

    [Test]
    public void GetTopCategoriesAsync_UnauthorizedRole_ThrowsForbiddenException()
    {
        SetupUser(ROLE_EMPLOYEE);
        Assert.ThrowsAsync<ForbiddenException>(() => _serviceSut.GetTopCategoriesAsync(5));
    }

    [Test]
    public async Task GetTopCategoriesAsync_AdminOrDeptHead_CallsRepository()
    {
        SetupUser(ROLE_ADMIN, departmentId: null);
        _mockRepo.Setup(r => r.GetTopCategoriesAsync(ROLE_ADMIN, null, 5))
            .ReturnsAsync(new List<TopCategoryDto>());

        var result = await _serviceSut.GetTopCategoriesAsync(5);
        Assert.That(result, Is.Not.Null);
        _mockRepo.Verify(r => r.GetTopCategoriesAsync(ROLE_ADMIN, null, 5), Times.Once);
    }

    [Test]
    public void GetComplaintAnalyticsAsync_UnauthorizedRole_ThrowsForbiddenException()
    {
        SetupUser(ROLE_GRO);
        Assert.ThrowsAsync<ForbiddenException>(() => _serviceSut.GetComplaintAnalyticsAsync());
    }

    [Test]
    public async Task GetComplaintAnalyticsAsync_AdminOrDeptHead_CallsRepository()
    {
        SetupUser(ROLE_DEPARTMENT_HEAD, departmentId: 3);
        _mockRepo.Setup(r => r.GetComplaintAnalyticsAsync(ROLE_DEPARTMENT_HEAD, 3))
            .ReturnsAsync(new ComplaintAnalyticsDto());

        var result = await _serviceSut.GetComplaintAnalyticsAsync();
        Assert.That(result, Is.Not.Null);
        _mockRepo.Verify(r => r.GetComplaintAnalyticsAsync(ROLE_DEPARTMENT_HEAD, 3), Times.Once);
    }

    #endregion
}

[TestFixture]
public class AnalyticsRepositoryTests
{
    private CGRContext _context = null!;
    private AnalyticsRepository _repoSut = null!;

    private const short ROLE_EMPLOYEE = 1;
    private const short ROLE_GRO = 2;
    private const short ROLE_DEPARTMENT_HEAD = 3;
    private const short ROLE_ADMIN = 4;

    private const short STATUS_SUBMITTED = 1;
    private const short STATUS_ASSIGNED = 2;
    private const short STATUS_IN_PROGRESS = 3;
    private const short STATUS_ESCALATED = 4;
    private const short STATUS_RESOLVED = 5;
    private const short STATUS_CLOSED = 6;
    private const short STATUS_REJECTED = 7;
    private const short STATUS_REOPENED = 8;
    private const short STATUS_EXTERNALLY_ESCALATED = 9;

    [SetUp]
    public async Task SetUp()
    {
        var options = new DbContextOptionsBuilder<CGRContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w =>
                w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _context = new CGRContext(options);
        _repoSut = new AnalyticsRepository(_context);

        await SeedReferenceDataAsync();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    private async Task SeedReferenceDataAsync()
    {
        // Seed statuses (1 to 9)
        var statuses = new List<ComplaintStatus>
        {
            new() { StatusId = STATUS_SUBMITTED, StatusName = "Submitted", IsTerminal = false },
            new() { StatusId = STATUS_ASSIGNED, StatusName = "Assigned", IsTerminal = false },
            new() { StatusId = STATUS_IN_PROGRESS, StatusName = "In Progress", IsTerminal = false },
            new() { StatusId = STATUS_ESCALATED, StatusName = "Escalated", IsTerminal = false },
            new() { StatusId = STATUS_RESOLVED, StatusName = "Resolved", IsTerminal = false },
            new() { StatusId = STATUS_CLOSED, StatusName = "Closed", IsTerminal = true },
            new() { StatusId = STATUS_REJECTED, StatusName = "Rejected", IsTerminal = true },
            new() { StatusId = STATUS_REOPENED, StatusName = "Reopened", IsTerminal = false },
            new() { StatusId = STATUS_EXTERNALLY_ESCALATED, StatusName = "Externally Escalated", IsTerminal = true }
        };
        _context.ComplaintStatuses.AddRange(statuses);

        // Seed Roles
        var roles = new List<Role>
        {
            new() { RoleId = ROLE_EMPLOYEE, RoleName = "EMPLOYEE" },
            new() { RoleId = ROLE_GRO, RoleName = "GRO" },
            new() { RoleId = ROLE_DEPARTMENT_HEAD, RoleName = "DEPARTMENT_HEAD" },
            new() { RoleId = ROLE_ADMIN, RoleName = "ADMIN" }
        };
        _context.Roles.AddRange(roles);

        // Seed Priorities
        var priorities = new List<Priority>
        {
            new() { PriorityId = 1, PriorityName = "Low", EscalationOrder = 1 },
            new() { PriorityId = 2, PriorityName = "Medium", EscalationOrder = 2 },
            new() { PriorityId = 3, PriorityName = "High", EscalationOrder = 3 },
            new() { PriorityId = 4, PriorityName = "Critical", EscalationOrder = 4 }
        };
        _context.Priorities.AddRange(priorities);

        // Seed Department
        var dept = new Department { DepartmentId = 10, DepartmentName = "IT Support", IsActive = true };
        _context.Departments.Add(dept);

        // Seed Categories
        var cat = new Category
        {
            CategoryId = 100,
            CategoryName = "Hardware",
            DepartmentId = 10,
            DefaultPriorityId = 1,
            SlaHours = 24,
            IsActive = true
        };
        _context.Categories.Add(cat);

        // Seed Employees
        var emp1 = new Employee
        {
            EmployeeId = 1,
            EmployeeName = "John Doe",
            Email = "john@company.com",
            MobileNumber = "123",
            PasswordHash = "hash",
            RoleId = ROLE_EMPLOYEE,
            IsActive = true
        };
        var emp2 = new Employee
        {
            EmployeeId = 2,
            EmployeeName = "Jane Handler",
            Email = "jane@company.com",
            MobileNumber = "456",
            PasswordHash = "hash",
            RoleId = ROLE_GRO,
            DepartmentId = 10,
            IsActive = true
        };
        _context.Employees.AddRange(emp1, emp2);

        await _context.SaveChangesAsync();
    }

    #region Repository Tests

    [Test]
    public async Task GetMyDashboardAsync_ReturnsAllStatuses_EvenWithZeroCount()
    {
        // Arrange
        // Add 1 complaint raised by John Doe (EmployeeId = 1) in status Assigned
        var complaint = new Complaint
        {
            ComplaintId = 1,
            ComplaintTitle = "Broken Keyboard",
            ComplaintDescription = "Keys not working",
            RaisedByEmployeeId = 1,
            CurrentHandlerEmployeeId = 2,
            CategoryId = 100,
            PriorityId = 1,
            StatusId = STATUS_ASSIGNED,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Complaints.Add(complaint);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repoSut.GetMyDashboardAsync(1);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.TotalRaised, Is.EqualTo(1));
        Assert.That(result.StatusBreakdown, Is.Not.Null);
        var breakdownList = result.StatusBreakdown.ToList();
        
        // Should contain all 9 statuses
        Assert.That(breakdownList.Count, Is.EqualTo(9));
        
        // Assigned status should have count 1
        var assigned = breakdownList.FirstOrDefault(s => s.StatusId == STATUS_ASSIGNED);
        Assert.That(assigned, Is.Not.Null);
        Assert.That(assigned!.Count, Is.EqualTo(1));

        // Resolved status (which is empty) should have count 0
        var resolved = breakdownList.FirstOrDefault(s => s.StatusId == STATUS_RESOLVED);
        Assert.That(resolved, Is.Not.Null);
        Assert.That(resolved!.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task GetComplaintAnalyticsAsync_CorrectlyCalculates_SlaBreachedComplaints()
    {
        // Arrange:
        // 1. Complaint that is open and overdue (EscalationDueAt is in the past, status is ASSIGNED) -> Breached
        var complaint1 = new Complaint
        {
            ComplaintId = 10,
            ComplaintTitle = "Overdue Open",
            ComplaintDescription = "Desc",
            RaisedByEmployeeId = 1,
            CategoryId = 100,
            PriorityId = 1,
            StatusId = STATUS_ASSIGNED,
            EscalationDueAt = DateTime.UtcNow.AddDays(-2),
            CreatedAt = DateTime.UtcNow.AddDays(-3),
            UpdatedAt = DateTime.UtcNow
        };

        // 2. Complaint that is open and NOT overdue (EscalationDueAt is in the future, status is ASSIGNED) -> Not Breached
        var complaint2 = new Complaint
        {
            ComplaintId = 11,
            ComplaintTitle = "On Time Open",
            ComplaintDescription = "Desc",
            RaisedByEmployeeId = 1,
            CategoryId = 100,
            PriorityId = 1,
            StatusId = STATUS_ASSIGNED,
            EscalationDueAt = DateTime.UtcNow.AddDays(2),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // 3. Complaint that is resolved and breached (ResolvedAt > EscalationDueAt) -> Breached
        var complaint3 = new Complaint
        {
            ComplaintId = 12,
            ComplaintTitle = "Resolved Breached",
            ComplaintDescription = "Desc",
            RaisedByEmployeeId = 1,
            CategoryId = 100,
            PriorityId = 1,
            StatusId = STATUS_RESOLVED,
            EscalationDueAt = DateTime.UtcNow.AddDays(-2),
            ResolvedAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-3),
            UpdatedAt = DateTime.UtcNow
        };

        // 4. Complaint that is resolved and complied (ResolvedAt <= EscalationDueAt) -> Not Breached
        var complaint4 = new Complaint
        {
            ComplaintId = 13,
            ComplaintTitle = "Resolved Complied",
            ComplaintDescription = "Desc",
            RaisedByEmployeeId = 1,
            CategoryId = 100,
            PriorityId = 1,
            StatusId = STATUS_RESOLVED,
            EscalationDueAt = DateTime.UtcNow.AddDays(-1),
            ResolvedAt = DateTime.UtcNow.AddDays(-2),
            CreatedAt = DateTime.UtcNow.AddDays(-3),
            UpdatedAt = DateTime.UtcNow
        };

        _context.Complaints.AddRange(complaint1, complaint2, complaint3, complaint4);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repoSut.GetComplaintAnalyticsAsync(ROLE_ADMIN, null);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.TotalComplaints, Is.EqualTo(4));
        
        // Breached complaints should be: complaint1 and complaint3 -> total 2
        Assert.That(result.SlaBreachedComplaints, Is.EqualTo(2));
    }

    #endregion
}
