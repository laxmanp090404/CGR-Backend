using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using cgrbussinesslogic.Interfaces;
using cgrbussinesslogic.Services;
using cgrdataaccesslibrary.Context;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.DTOs.Complaint;
using cgrmodellibrary.Exceptions;
using cgrmodellibrary.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace cgrtest.Services;

/// <summary>
/// Unit tests for <see cref="ComplaintService"/>.
/// Each test class focuses on one method for clear organisation.
/// All external dependencies are mocked via Moq.
/// The EF Core InMemory provider is used so that
/// <c>_context.Database.BeginTransactionAsync()</c> works without a real DB.
/// </summary>
[TestFixture]
public class ComplaintServiceTests
{
    // ──────────────────────────────────────────────────────────────────
    //  SHARED STATUS / ROLE CONSTANTS  (mirror the values in the SUT)
    // ──────────────────────────────────────────────────────────────────
    private const short STATUS_SUBMITTED   = 1;
    private const short STATUS_ASSIGNED    = 2;
    private const short STATUS_IN_PROGRESS = 3;
    private const short STATUS_ESCALATED   = 4;
    private const short STATUS_RESOLVED    = 5;
    private const short STATUS_CLOSED      = 6;
    private const short STATUS_REJECTED    = 7;
    private const short STATUS_EXTERNALLY_ESCALATED = 9;

    private const short ROLE_EMPLOYEE        = 1;
    private const short ROLE_GRO             = 2;
    private const short ROLE_DEPARTMENT_HEAD = 3;
    private const short ROLE_ADMIN           = 4;

    // ──────────────────────────────────────────────────────────────────
    //  MOCKS & SUT
    // ──────────────────────────────────────────────────────────────────
    private Mock<IComplaintRepository>           _complaintRepo   = null!;
    private Mock<IComplaintHistoryRepository>    _historyRepo     = null!;
    private Mock<IComplaintAssignmentRepository> _assignmentRepo  = null!;
    private Mock<ICategoryRepository>            _categoryRepo    = null!;
    private Mock<IEmployeeRepository>            _employeeRepo    = null!;
    private Mock<INotificationService>           _notifService    = null!;
    private Mock<ICurrentUserService>            _currentUser     = null!;
    private Mock<IComplaintAttachmentService>    _attachmentSvc   = null!;
    private Mock<IComplaintEscalationRepository> _escalationRepo  = null!;
    private Mock<IComplaintAssignmentEngine>     _assignEngine    = null!;
    private Mock<IEscalationRuleRepository>      _escalationRules = null!;
    private Mock<ILogger<ComplaintService>>      _logger          = null!;
    private CGRContext                           _context         = null!;
    private ComplaintService                     _sut             = null!;

    [SetUp]
    public void SetUp()
    {
        _complaintRepo   = new Mock<IComplaintRepository>();
        _historyRepo     = new Mock<IComplaintHistoryRepository>();
        _assignmentRepo  = new Mock<IComplaintAssignmentRepository>();
        _categoryRepo    = new Mock<ICategoryRepository>();
        _employeeRepo    = new Mock<IEmployeeRepository>();
        _notifService    = new Mock<INotificationService>();
        _currentUser     = new Mock<ICurrentUserService>();
        _attachmentSvc   = new Mock<IComplaintAttachmentService>();
        _escalationRepo  = new Mock<IComplaintEscalationRepository>();
        _assignEngine    = new Mock<IComplaintAssignmentEngine>();
        _escalationRules = new Mock<IEscalationRuleRepository>();
        _logger          = new Mock<ILogger<ComplaintService>>();

        // InMemory DB – gives us a real DatabaseFacade so
        // BeginTransactionAsync returns a real (no-op) transaction.
        // We suppress the transaction warning because InMemory doesn't
        // truly support transactions; we just need the call to not throw.
        var options = new DbContextOptionsBuilder<CGRContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w =>
                w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _context = new CGRContext(options);

        _sut = new ComplaintService(
            _complaintRepo.Object,
            _historyRepo.Object,
            _assignmentRepo.Object,
            _categoryRepo.Object,
            _employeeRepo.Object,
            _assignEngine.Object,
            _notifService.Object,
            _context,
            _attachmentSvc.Object,
            _currentUser.Object,
            _escalationRepo.Object,
            _escalationRules.Object,
            _logger.Object);

        // ── default stubs used by most tests ──────────────────────────
        _employeeRepo
            .Setup(r => r.Get(It.IsAny<int>()))
            .ReturnsAsync((int id) => new Employee { EmployeeId = id, IsActive = true });

        _historyRepo
            .Setup(r => r.Create(It.IsAny<ComplaintHistory>()))
            .ReturnsAsync((ComplaintHistory h) => h);

        _assignmentRepo
            .Setup(r => r.Create(It.IsAny<ComplaintAssignmentHistory>()))
            .ReturnsAsync((ComplaintAssignmentHistory a) => a);

        _notifService
            .Setup(n => n.SendAsync(
                It.IsAny<int>(), It.IsAny<short>(),
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int?>()))
            .Returns(Task.CompletedTask);

        _complaintRepo
            .Setup(r => r.Update(It.IsAny<Complaint>(), It.IsAny<int>()))
            .ReturnsAsync((Complaint c, int _) => c);

        _assignEngine
            .Setup(e => e.CalculateEscalationDueAtAsync(It.IsAny<Complaint>()))
            .ReturnsAsync(DateTime.UtcNow.AddHours(24));
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    // ═══════════════════════════════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════════════════════════════
    private static Complaint BuildComplaint(
        int    id               = 1,
        short  statusId         = STATUS_ASSIGNED,
        int    raisedBy         = 10,
        int?   currentHandler   = 20,
        int    categoryId       = 100,
        int    departmentId     = 5,
        short  escalationLevel  = 0,
        short  reopenedCount    = 0) =>
        new()
        {
            ComplaintId              = id,
            ComplaintTitle           = "Test complaint",
            ComplaintDescription     = "Description",
            StatusId                 = statusId,
            RaisedByEmployeeId       = raisedBy,
            CurrentHandlerEmployeeId = currentHandler,
            CategoryId               = categoryId,
            PriorityId               = 1,
            EscalationLevel          = escalationLevel,
            ReopenedCount            = reopenedCount,
            CreatedAt                = DateTime.UtcNow,
            UpdatedAt                = DateTime.UtcNow,
            Category = new Category
            {
                CategoryId        = categoryId,
                DepartmentId      = departmentId,
                DefaultPriorityId = 1,
                IsActive          = true,
                CategoryName      = "IT",
                SlaHours          = 48,
                CreatedAt         = DateTime.UtcNow,
                UpdatedAt         = DateTime.UtcNow
            },
            RaisedByEmployee = new Employee { EmployeeId = raisedBy, EmployeeName = "Raiser" }
        };

    private static Employee BuildGro(
        int   id           = 30,
        bool  isActive     = true,
        int?  departmentId = 5) =>
        new()
        {
            EmployeeId   = id,
            EmployeeName = "GRO User",
            Email        = "gro@test.com",
            MobileNumber = "1234567890",
            PasswordHash = "hash",
            RoleId       = ROLE_GRO,
            DepartmentId = departmentId,
            IsActive     = isActive,
            CreatedAt    = DateTime.UtcNow,
            UpdatedAt    = DateTime.UtcNow,
            Role         = new Role { RoleId = ROLE_GRO, RoleName = "GRO" }
        };

    private void SetupCurrentUser(
        int    employeeId    = 20,
        short  roleId        = ROLE_GRO,
        string role          = "GRO",
        int?   departmentId  = 5)
    {
        _currentUser.Setup(u => u.EmployeeId)   .Returns(employeeId);
        _currentUser.Setup(u => u.RoleId)       .Returns(roleId);
        _currentUser.Setup(u => u.Role)         .Returns(role);
        _currentUser.Setup(u => u.DepartmentId) .Returns(departmentId);
    }


    // ═══════════════════════════════════════════════════════════════════
    //  CreateAsync
    // ═══════════════════════════════════════════════════════════════════
    [TestFixture]
    public class CreateAsync_Tests : ComplaintServiceTests
    {
        private static CreateComplaintDto DefaultDto() => new()
        {
            ComplaintTitle       = "My complaint",
            ComplaintDescription = "Details",
            CategoryId           = 100,
            Attachments          = null
        };

        [Test]
        public async Task CreateAsync_AdminUser_ThrowsBusinessRuleException()
        {
            SetupCurrentUser(roleId: ROLE_ADMIN, role: "ADMIN");

            Assert.ThrowsAsync<BusinessRuleException>(
                () => _sut.CreateAsync(DefaultDto()));
        }

        [Test]
        public async Task CreateAsync_InactiveEmployee_ThrowsBusinessRuleException()
        {
            SetupCurrentUser(employeeId: 10, roleId: ROLE_EMPLOYEE, role: "EMPLOYEE");
            _employeeRepo
                .Setup(r => r.Get(10))
                .ReturnsAsync(new Employee { EmployeeId = 10, IsActive = false });

            var ex = Assert.ThrowsAsync<BusinessRuleException>(
                () => _sut.CreateAsync(DefaultDto()));
            Assert.That(ex!.Message, Is.EqualTo("Employee is inactive."));
        }

        [Test]
        public async Task CreateAsync_InactiveCategory_ThrowsBusinessRuleException()
        {
            SetupCurrentUser(roleId: ROLE_EMPLOYEE, role: "EMPLOYEE");

            _categoryRepo
                .Setup(r => r.Get(100))
                .ReturnsAsync(new Category
                {
                    CategoryId        = 100,
                    IsActive          = false,
                    DefaultPriorityId = 1,
                    DepartmentId      = 5,
                    CategoryName      = "IT",
                    SlaHours          = 48,
                    CreatedAt         = DateTime.UtcNow,
                    UpdatedAt         = DateTime.UtcNow
                });

            Assert.ThrowsAsync<BusinessRuleException>(
                () => _sut.CreateAsync(DefaultDto()));
        }

        [Test]
        public async Task CreateAsync_DuplicateComplaint_ThrowsConflictException()
        {
            SetupCurrentUser(roleId: ROLE_EMPLOYEE, role: "EMPLOYEE");

            _categoryRepo
                .Setup(r => r.Get(100))
                .ReturnsAsync(new Category
                {
                    CategoryId        = 100,
                    IsActive          = true,
                    DefaultPriorityId = 1,
                    DepartmentId      = 5,
                    CategoryName      = "IT",
                    SlaHours          = 48,
                    CreatedAt         = DateTime.UtcNow,
                    UpdatedAt         = DateTime.UtcNow
                });

            _complaintRepo
                .Setup(r => r.ExistsRecentDuplicateAsync(
                    It.IsAny<int>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(true);

            Assert.ThrowsAsync<ConflictException>(
                () => _sut.CreateAsync(DefaultDto()));
        }

        [Test]
        public async Task CreateAsync_ValidRequest_CreatesComplaintAndReturnsDto()
        {
            SetupCurrentUser(employeeId: 10, roleId: ROLE_EMPLOYEE, role: "EMPLOYEE");

            var category = new Category
            {
                CategoryId        = 100,
                IsActive          = true,
                DefaultPriorityId = 1,
                DepartmentId      = 5,
                CategoryName      = "IT",
                SlaHours          = 48,
                CreatedAt         = DateTime.UtcNow,
                UpdatedAt         = DateTime.UtcNow
            };

            _categoryRepo.Setup(r => r.Get(100)).ReturnsAsync(category);
            _complaintRepo
                .Setup(r => r.ExistsRecentDuplicateAsync(
                    It.IsAny<int>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(false);

            var created = BuildComplaint(statusId: STATUS_SUBMITTED);
            _complaintRepo.Setup(r => r.Create(It.IsAny<Complaint>())).ReturnsAsync(created);

            _assignEngine
                .Setup(e => e.DetermineInitialAssignmentAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<short>()))
                .ReturnsAsync((20, (short)0));

            // After transaction.CommitAsync, service reloads the complaint
            var reloaded = BuildComplaint(statusId: STATUS_ASSIGNED);
            _complaintRepo
                .Setup(r => r.GetDetailByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(reloaded);

            var result = await _sut.CreateAsync(DefaultDto());

            Assert.That(result,                Is.Not.Null);
            Assert.That(result.StatusId,       Is.EqualTo(STATUS_ASSIGNED));

            _complaintRepo.Verify(r => r.Create(It.IsAny<Complaint>()), Times.Once);
            _historyRepo  .Verify(r => r.Create(It.IsAny<ComplaintHistory>()), Times.AtLeast(2));
            _notifService .Verify(n => n.SendAsync(
                It.IsAny<int>(), It.IsAny<short>(),
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int?>()), Times.AtLeast(2));
        }

        [Test]
        public async Task CreateAsync_RepositoryThrows_RollsBackAndRethrows()
        {
            SetupCurrentUser(roleId: ROLE_EMPLOYEE, role: "EMPLOYEE");

            _categoryRepo
                .Setup(r => r.Get(100))
                .ThrowsAsync(new Exception("DB error"));

            Assert.ThrowsAsync<Exception>(() => _sut.CreateAsync(DefaultDto()));
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  GetByIdAsync
    // ═══════════════════════════════════════════════════════════════════
    [TestFixture]
    public class GetByIdAsync_Tests : ComplaintServiceTests
    {
        [Test]
        public async Task GetByIdAsync_ComplaintNotFound_ThrowsNotFoundException()
        {
            SetupCurrentUser(roleId: ROLE_ADMIN, role: "ADMIN");
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(99)).ReturnsAsync((Complaint?)null);

            Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(99));
        }

        [Test]
        public async Task GetByIdAsync_AdminAlwaysSees_ReturnsDto()
        {
            SetupCurrentUser(employeeId: 99, roleId: ROLE_ADMIN, role: "ADMIN");
            var complaint = BuildComplaint();
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);

            var result = await _sut.GetByIdAsync(1);

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task GetByIdAsync_NonOwnerEmployee_ThrowsForbiddenException()
        {
            // Employee 99 is neither the raiser (10) nor the handler (20).
            SetupCurrentUser(employeeId: 99, roleId: ROLE_EMPLOYEE, role: "EMPLOYEE");
            var complaint = BuildComplaint(raisedBy: 10, currentHandler: 20);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);

            Assert.ThrowsAsync<ForbiddenException>(() => _sut.GetByIdAsync(1));
        }

        [Test]
        public async Task GetByIdAsync_RaiserSees_ReturnsDto()
        {
            SetupCurrentUser(employeeId: 10, roleId: ROLE_EMPLOYEE, role: "EMPLOYEE");
            var complaint = BuildComplaint(raisedBy: 10, currentHandler: 20);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);

            var result = await _sut.GetByIdAsync(1);

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task GetByIdAsync_CurrentHandlerSees_ReturnsDto()
        {
            SetupCurrentUser(employeeId: 20, roleId: ROLE_GRO, role: "GRO");
            var complaint = BuildComplaint(raisedBy: 10, currentHandler: 20);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);

            var result = await _sut.GetByIdAsync(1);

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task GetByIdAsync_DepartmentHeadSameDept_ReturnsDto()
        {
            SetupCurrentUser(employeeId: 50, roleId: ROLE_DEPARTMENT_HEAD,
                             role: "DEPARTMENT_HEAD", departmentId: 5);
            var complaint = BuildComplaint(raisedBy: 10, currentHandler: 20, departmentId: 5);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);

            var result = await _sut.GetByIdAsync(1);
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task GetByIdAsync_DepartmentHeadDifferentDept_ThrowsForbiddenException()
        {
            SetupCurrentUser(employeeId: 50, roleId: ROLE_DEPARTMENT_HEAD,
                             role: "DEPARTMENT_HEAD", departmentId: 99);
            var complaint = BuildComplaint(raisedBy: 10, currentHandler: 20, departmentId: 5);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);

            Assert.ThrowsAsync<ForbiddenException>(() => _sut.GetByIdAsync(1));
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  StartProgressAsync
    // ═══════════════════════════════════════════════════════════════════
    [TestFixture]
    public class StartProgressAsync_Tests : ComplaintServiceTests
    {
        [Test]
        public async Task StartProgressAsync_ComplaintNotFound_ThrowsNotFoundException()
        {
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync((Complaint?)null);

            Assert.ThrowsAsync<NotFoundException>(() => _sut.StartProgressAsync(1));
        }

        [Test]
        public async Task StartProgressAsync_NotCurrentHandler_ThrowsForbiddenException()
        {
            SetupCurrentUser(employeeId: 99); // handler is 20
            var complaint = BuildComplaint(statusId: STATUS_ASSIGNED, currentHandler: 20);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);

            Assert.ThrowsAsync<ForbiddenException>(() => _sut.StartProgressAsync(1));
        }

        [Test]
        public async Task StartProgressAsync_WrongStatus_ThrowsBusinessRuleException()
        {
            SetupCurrentUser(employeeId: 20);
            // Status is IN_PROGRESS, not ASSIGNED
            var complaint = BuildComplaint(statusId: STATUS_IN_PROGRESS, currentHandler: 20);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);

            Assert.ThrowsAsync<BusinessRuleException>(() => _sut.StartProgressAsync(1));
        }

        [Test]
        public async Task StartProgressAsync_StatusIsSubmitted_ThrowsBusinessRuleException()
        {
            SetupCurrentUser(employeeId: 20);
            var complaint = BuildComplaint(statusId: STATUS_SUBMITTED, currentHandler: 20);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);

            Assert.ThrowsAsync<BusinessRuleException>(() => _sut.StartProgressAsync(1));
        }

        [Test]
        public async Task StartProgressAsync_ValidRequest_UpdatesStatusAndCreatesHistory()
        {
            SetupCurrentUser(employeeId: 20, roleId: ROLE_GRO);
            var complaint = BuildComplaint(statusId: STATUS_ASSIGNED, currentHandler: 20);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);

            await _sut.StartProgressAsync(1);

            _complaintRepo.Verify(r => r.Update(
                It.Is<Complaint>(c => c.StatusId == STATUS_IN_PROGRESS),
                1), Times.Once);

            _historyRepo.Verify(r => r.Create(
                It.Is<ComplaintHistory>(h =>
                    h.OldStatusId == STATUS_ASSIGNED &&
                    h.NewStatusId == STATUS_IN_PROGRESS)),
                Times.Once);
        }

        [Test]
        public async Task StartProgressAsync_StatusIsEscalated_UpdatesStatusAndCreatesHistory()
        {
            SetupCurrentUser(employeeId: 20, roleId: ROLE_ADMIN);
            var complaint = BuildComplaint(statusId: STATUS_ESCALATED, currentHandler: 20);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);

            await _sut.StartProgressAsync(1);

            _complaintRepo.Verify(r => r.Update(
                It.Is<Complaint>(c => c.StatusId == STATUS_IN_PROGRESS),
                1), Times.Once);

            _historyRepo.Verify(r => r.Create(
                It.Is<ComplaintHistory>(h =>
                    h.OldStatusId == STATUS_ESCALATED &&
                    h.NewStatusId == STATUS_IN_PROGRESS)),
                Times.Once);
        }

        [Test]
        public async Task StartProgressAsync_RepositoryThrows_RollsBackAndRethrows()
        {
            SetupCurrentUser(employeeId: 20);
            var complaint = BuildComplaint(statusId: STATUS_ASSIGNED, currentHandler: 20);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);
            _complaintRepo
                .Setup(r => r.Update(It.IsAny<Complaint>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception("DB write failed"));

            Assert.ThrowsAsync<Exception>(() => _sut.StartProgressAsync(1));
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  ResolveAsync
    // ═══════════════════════════════════════════════════════════════════
    [TestFixture]
    public class ResolveAsync_Tests : ComplaintServiceTests
    {
        private static ResolveComplaintDto ResolveDto() =>
            new() { ResolutionRemarks = "Fixed!" };

        [Test]
        public async Task ResolveAsync_ComplaintNotFound_ThrowsNotFoundException()
        {
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync((Complaint?)null);

            Assert.ThrowsAsync<NotFoundException>(() => _sut.ResolveAsync(1, ResolveDto()));
        }

        [Test]
        public async Task ResolveAsync_NotCurrentHandler_ThrowsForbiddenException()
        {
            SetupCurrentUser(employeeId: 99);
            var complaint = BuildComplaint(statusId: STATUS_IN_PROGRESS, currentHandler: 20);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);

            Assert.ThrowsAsync<ForbiddenException>(() => _sut.ResolveAsync(1, ResolveDto()));
        }

        [Test]
        public async Task ResolveAsync_StatusNotInProgress_ThrowsBusinessRuleException()
        {
            SetupCurrentUser(employeeId: 20);
            var complaint = BuildComplaint(statusId: STATUS_ASSIGNED, currentHandler: 20);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);

            Assert.ThrowsAsync<BusinessRuleException>(() => _sut.ResolveAsync(1, ResolveDto()));
        }

        [Test]
        public async Task ResolveAsync_ValidRequest_SetsResolvedStatusAndNotifiesRaiser()
        {
            SetupCurrentUser(employeeId: 20, roleId: ROLE_GRO);
            var complaint = BuildComplaint(
                statusId: STATUS_IN_PROGRESS, currentHandler: 20, raisedBy: 10);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);

            await _sut.ResolveAsync(1, ResolveDto());

            _complaintRepo.Verify(r => r.Update(
                It.Is<Complaint>(c =>
                    c.StatusId   == STATUS_RESOLVED &&
                    c.ResolvedAt != null),
                1), Times.Once);

            _historyRepo.Verify(r => r.Create(
                It.Is<ComplaintHistory>(h =>
                    h.NewStatusId == STATUS_RESOLVED &&
                    h.Remarks     == "Fixed!")),
                Times.Once);

            _notifService.Verify(n => n.SendAsync(
                10,                   // raiser
                It.IsAny<short>(),
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int?>()), Times.Once);
        }

        [Test]
        public async Task ResolveAsync_AlreadyResolved_ThrowsBusinessRuleException()
        {
            SetupCurrentUser(employeeId: 20);
            var complaint = BuildComplaint(statusId: STATUS_RESOLVED, currentHandler: 20);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);

            Assert.ThrowsAsync<BusinessRuleException>(() => _sut.ResolveAsync(1, ResolveDto()));
        }

        [Test]
        public async Task ResolveAsync_HistoryThrows_RollsBackAndRethrows()
        {
            SetupCurrentUser(employeeId: 20);
            var complaint = BuildComplaint(statusId: STATUS_IN_PROGRESS, currentHandler: 20);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);
            _historyRepo
                .Setup(r => r.Create(It.IsAny<ComplaintHistory>()))
                .ThrowsAsync(new Exception("History write failed"));

            Assert.ThrowsAsync<Exception>(() => _sut.ResolveAsync(1, ResolveDto()));
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  CloseAsync
    // ═══════════════════════════════════════════════════════════════════
    [TestFixture]
    public class CloseAsync_Tests : ComplaintServiceTests
    {
        private static CloseComplaintDto CloseDto() =>
            new() { Remarks = "Thank you." };

        [Test]
        public async Task CloseAsync_ComplaintNotFound_ThrowsNotFoundException()
        {
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync((Complaint?)null);

            Assert.ThrowsAsync<NotFoundException>(() => _sut.CloseAsync(1, CloseDto()));
        }

        [Test]
        public async Task CloseAsync_NotRaiser_ThrowsForbiddenException()
        {
            SetupCurrentUser(employeeId: 99); // raiser is 10
            var complaint = BuildComplaint(statusId: STATUS_RESOLVED, raisedBy: 10, currentHandler: 20);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);

            Assert.ThrowsAsync<ForbiddenException>(() => _sut.CloseAsync(1, CloseDto()));
        }

        [Test]
        public async Task CloseAsync_StatusNotResolved_ThrowsBusinessRuleException()
        {
            SetupCurrentUser(employeeId: 10); // raiser
            var complaint = BuildComplaint(statusId: STATUS_IN_PROGRESS, raisedBy: 10, currentHandler: 20);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);

            Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CloseAsync(1, CloseDto()));
        }

        [Test]
        public async Task CloseAsync_ValidRequest_SetsClosedStatusAndNotifiesHandler()
        {
            SetupCurrentUser(employeeId: 10, roleId: ROLE_EMPLOYEE);
            var complaint = BuildComplaint(
                statusId: STATUS_RESOLVED, raisedBy: 10, currentHandler: 20);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);

            await _sut.CloseAsync(1, CloseDto());

            _complaintRepo.Verify(r => r.Update(
                It.Is<Complaint>(c =>
                    c.StatusId  == STATUS_CLOSED &&
                    c.ClosedAt  != null),
                1), Times.Once);

            _historyRepo.Verify(r => r.Create(
                It.Is<ComplaintHistory>(h =>
                    h.NewStatusId == STATUS_CLOSED &&
                    h.Remarks     == "Thank you.")),
                Times.Once);

            _notifService.Verify(n => n.SendAsync(
                20,                   // handler
                It.IsAny<short>(),
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int?>()), Times.Once);
        }

        [Test]
        public async Task CloseAsync_AlreadyClosed_ThrowsBusinessRuleException()
        {
            SetupCurrentUser(employeeId: 10);
            var complaint = BuildComplaint(statusId: STATUS_CLOSED, raisedBy: 10, currentHandler: 20);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);

            Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CloseAsync(1, CloseDto()));
        }

        [Test]
        public async Task CloseAsync_RepositoryThrows_RollsBackAndRethrows()
        {
            SetupCurrentUser(employeeId: 10);
            var complaint = BuildComplaint(statusId: STATUS_RESOLVED, raisedBy: 10, currentHandler: 20);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);
            _complaintRepo
                .Setup(r => r.Update(It.IsAny<Complaint>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception("DB error"));

            Assert.ThrowsAsync<Exception>(() => _sut.CloseAsync(1, CloseDto()));
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  AssignAsync
    // ═══════════════════════════════════════════════════════════════════
    [TestFixture]
    public class AssignAsync_Tests : ComplaintServiceTests
    {
        private static AssignComplaintDto AssignDto(int groId = 30) =>
            new() { GroEmployeeId = groId, Remarks = "Manual assign" };

        [Test]
        public async Task AssignAsync_ComplaintNotFound_ThrowsNotFoundException()
        {
            SetupCurrentUser(roleId: ROLE_ADMIN, role: "ADMIN");
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync((Complaint?)null);
            _assignmentRepo.Setup(r => r.GetPreviousHandlersAsync(1)).ReturnsAsync(new List<int>());

            Assert.ThrowsAsync<NotFoundException>(() => _sut.AssignAsync(1, AssignDto()));
        }

        [Test]
        public async Task AssignAsync_NonAdmin_ThrowsForbiddenException()
        {
            SetupCurrentUser(employeeId: 20, roleId: ROLE_GRO, role: "GRO");
            var complaint = BuildComplaint(statusId: STATUS_ASSIGNED, currentHandler: 20);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);
            _assignmentRepo.Setup(r => r.GetPreviousHandlersAsync(1)).ReturnsAsync(new List<int>());

            Assert.ThrowsAsync<ForbiddenException>(() => _sut.AssignAsync(1, AssignDto()));
        }

        [TestCase(STATUS_RESOLVED)]
        [TestCase(STATUS_REJECTED)]
        [TestCase(STATUS_CLOSED)]
        [TestCase(STATUS_EXTERNALLY_ESCALATED)]
        public async Task AssignAsync_TerminalStatus_ThrowsBusinessRuleException(short statusId)
        {
            SetupCurrentUser(roleId: ROLE_ADMIN, role: "ADMIN");
            var complaint = BuildComplaint(statusId: statusId);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);
            _assignmentRepo.Setup(r => r.GetPreviousHandlersAsync(1)).ReturnsAsync(new List<int>());

            Assert.ThrowsAsync<BusinessRuleException>(() => _sut.AssignAsync(1, AssignDto()));
        }

        [Test]
        public async Task AssignAsync_EmployeeNotFound_ThrowsNotFoundException()
        {
            SetupCurrentUser(roleId: ROLE_ADMIN, role: "ADMIN");
            var complaint = BuildComplaint(statusId: STATUS_ASSIGNED);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);
            _assignmentRepo.Setup(r => r.GetPreviousHandlersAsync(1)).ReturnsAsync(new List<int>());
            _employeeRepo.Setup(r => r.GetByIdWithRoleAsync(30)).ReturnsAsync((Employee?)null);

            Assert.ThrowsAsync<NotFoundException>(() => _sut.AssignAsync(1, AssignDto(groId: 30)));
        }

        [Test]
        public async Task AssignAsync_InactiveEmployee_ThrowsBusinessRuleException()
        {
            SetupCurrentUser(roleId: ROLE_ADMIN, role: "ADMIN");
            var complaint = BuildComplaint(statusId: STATUS_ASSIGNED);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);
            _assignmentRepo.Setup(r => r.GetPreviousHandlersAsync(1)).ReturnsAsync(new List<int>());
            _employeeRepo.Setup(r => r.GetByIdWithRoleAsync(30))
                         .ReturnsAsync(BuildGro(isActive: false));

            Assert.ThrowsAsync<BusinessRuleException>(() => _sut.AssignAsync(1, AssignDto()));
        }

        [Test]
        public async Task AssignAsync_EmployeeNotGro_ThrowsBusinessRuleException()
        {
            SetupCurrentUser(roleId: ROLE_ADMIN, role: "ADMIN");
            var complaint = BuildComplaint(statusId: STATUS_ASSIGNED);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);
            _assignmentRepo.Setup(r => r.GetPreviousHandlersAsync(1)).ReturnsAsync(new List<int>());

            var nonGro = BuildGro();
            nonGro.Role = new Role { RoleId = ROLE_EMPLOYEE, RoleName = "EMPLOYEE" };
            _employeeRepo.Setup(r => r.GetByIdWithRoleAsync(30)).ReturnsAsync(nonGro);

            Assert.ThrowsAsync<BusinessRuleException>(() => _sut.AssignAsync(1, AssignDto()));
        }

        [Test]
        public async Task AssignAsync_DifferentDepartment_ThrowsBusinessRuleException()
        {
            SetupCurrentUser(roleId: ROLE_ADMIN, role: "ADMIN");
            var complaint = BuildComplaint(statusId: STATUS_ASSIGNED, departmentId: 5);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);
            _assignmentRepo.Setup(r => r.GetPreviousHandlersAsync(1)).ReturnsAsync(new List<int>());

            // GRO belongs to dept 99, but complaint is dept 5
            _employeeRepo
                .Setup(r => r.GetByIdWithRoleAsync(30))
                .ReturnsAsync(BuildGro(departmentId: 99));

            Assert.ThrowsAsync<BusinessRuleException>(() => _sut.AssignAsync(1, AssignDto()));
        }

        [Test]
        public async Task AssignAsync_PreviousHandler_ThrowsBusinessRuleException()
        {
            SetupCurrentUser(roleId: ROLE_ADMIN, role: "ADMIN");
            var complaint = BuildComplaint(statusId: STATUS_ASSIGNED, departmentId: 5);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);
            _assignmentRepo
                .Setup(r => r.GetPreviousHandlersAsync(1))
                .ReturnsAsync(new List<int> { 30 }); // 30 already handled it

            _employeeRepo
                .Setup(r => r.GetByIdWithRoleAsync(30))
                .ReturnsAsync(BuildGro(id: 30, departmentId: 5));

            Assert.ThrowsAsync<BusinessRuleException>(() => _sut.AssignAsync(1, AssignDto(groId: 30)));
        }

        [Test]
        public async Task AssignAsync_ValidRequest_AssignsAndNotifiesBothParties()
        {
            SetupCurrentUser(employeeId: 1, roleId: ROLE_ADMIN, role: "ADMIN");
            var complaint = BuildComplaint(
                statusId: STATUS_ASSIGNED, raisedBy: 10, currentHandler: 20, departmentId: 5);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);
            _assignmentRepo
                .Setup(r => r.GetPreviousHandlersAsync(1))
                .ReturnsAsync(new List<int>()); // no previous handlers

            _employeeRepo
                .Setup(r => r.GetByIdWithRoleAsync(30))
                .ReturnsAsync(BuildGro(id: 30, departmentId: 5));

            await _sut.AssignAsync(1, AssignDto(groId: 30));

            _complaintRepo.Verify(r => r.Update(
                It.Is<Complaint>(c =>
                    c.CurrentHandlerEmployeeId == 30 &&
                    c.StatusId                 == STATUS_ASSIGNED &&
                    c.EscalationLevel          == 0),
                1), Times.Once);

            _assignmentRepo.Verify(r => r.Create(
                It.Is<ComplaintAssignmentHistory>(a =>
                    a.NewHandlerEmployeeId == 30 &&
                    a.OldHandlerEmployeeId == 20 &&
                    a.AssignmentReason     == "MANUAL")),
                Times.Once);

            _historyRepo.Verify(r => r.Create(
                It.Is<ComplaintHistory>(h =>
                    h.NewHandlerEmployeeId == 30 &&
                    h.NewStatusId          == STATUS_ASSIGNED)),
                Times.Once);

            // Notification to new GRO
            _notifService.Verify(n => n.SendAsync(
                30, It.IsAny<short>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<int?>()), Times.Once);

            // Notification to raiser
            _notifService.Verify(n => n.SendAsync(
                10, It.IsAny<short>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<int?>()), Times.Once);
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  ReopenAsync
    // ═══════════════════════════════════════════════════════════════════
    [TestFixture]
    public class ReopenAsync_Tests : ComplaintServiceTests
    {
        private static ReopenComplaintDto ReopenDto() =>
            new() { ReopenRemarks = "Issue not fixed." };

        [Test]
        public async Task ReopenAsync_ComplaintNotFound_ThrowsNotFoundException()
        {
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync((Complaint?)null);

            Assert.ThrowsAsync<NotFoundException>(() => _sut.ReopenAsync(1, ReopenDto()));
        }

        [Test]
        public async Task ReopenAsync_NotRaiser_ThrowsForbiddenException()
        {
            SetupCurrentUser(employeeId: 99);
            var complaint = BuildComplaint(statusId: STATUS_RESOLVED, raisedBy: 10);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);

            Assert.ThrowsAsync<ForbiddenException>(() => _sut.ReopenAsync(1, ReopenDto()));
        }

        [Test]
        public async Task ReopenAsync_WrongStatus_ThrowsBusinessRuleException()
        {
            SetupCurrentUser(employeeId: 10);
            var complaint = BuildComplaint(statusId: STATUS_ASSIGNED, raisedBy: 10);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);

            Assert.ThrowsAsync<BusinessRuleException>(() => _sut.ReopenAsync(1, ReopenDto()));
        }

        [Test]
        public async Task ReopenAsync_MaxReopensReached_ThrowsBusinessRuleException()
        {
            SetupCurrentUser(employeeId: 10);
            var complaint = BuildComplaint(
                statusId: STATUS_RESOLVED, raisedBy: 10, reopenedCount: 3);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);

            Assert.ThrowsAsync<BusinessRuleException>(() => _sut.ReopenAsync(1, ReopenDto()));
        }

        [Test]
        public async Task ReopenAsync_ValidRequest_IncrementsReopenCountAndReassigns()
        {
            SetupCurrentUser(employeeId: 10, roleId: ROLE_EMPLOYEE);
            var complaint = BuildComplaint(
                statusId: STATUS_RESOLVED, raisedBy: 10,
                currentHandler: 20, reopenedCount: 1, departmentId: 5);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);

            _assignEngine
                .Setup(e => e.DetermineInitialAssignmentAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<short>()))
                .ReturnsAsync((25, (short)0));

            await _sut.ReopenAsync(1, ReopenDto());

            // Two updates: first REOPENED, then ASSIGNED
            _complaintRepo.Verify(r => r.Update(It.IsAny<Complaint>(), 1), Times.Exactly(2));

            // Two history records: REOPENED + ASSIGNED
            _historyRepo.Verify(r => r.Create(It.IsAny<ComplaintHistory>()), Times.Exactly(2));

            // Notifications: new handler + raiser
            _notifService.Verify(n => n.SendAsync(
                It.IsAny<int>(), It.IsAny<short>(),
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int?>()), Times.Exactly(2));
        }

        [Test]
        public async Task ReopenAsync_ExactlyThreeReopens_ThrowsBusinessRuleException()
        {
            // reopenedCount == 3 means limit reached
            SetupCurrentUser(employeeId: 10);
            var complaint = BuildComplaint(
                statusId: STATUS_RESOLVED, raisedBy: 10, reopenedCount: 3);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);

            Assert.ThrowsAsync<BusinessRuleException>(() => _sut.ReopenAsync(1, ReopenDto()));
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  EscalateAsync
    // ═══════════════════════════════════════════════════════════════════
    [TestFixture]
    public class EscalateAsync_Tests : ComplaintServiceTests
    {
        private static EscalateComplaintDto EscalateDto() =>
            new() { Remarks = "Need escalation." };

        [Test]
        public async Task EscalateAsync_ComplaintNotFound_ThrowsNotFoundException()
        {
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync((Complaint?)null);

            Assert.ThrowsAsync<NotFoundException>(() => _sut.EscalateAsync(1, EscalateDto()));
        }

        [Test]
        public async Task EscalateAsync_NotCurrentHandler_ThrowsForbiddenException()
        {
            SetupCurrentUser(employeeId: 99, roleId: ROLE_GRO);
            var complaint = BuildComplaint(statusId: STATUS_IN_PROGRESS, currentHandler: 20);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);

            Assert.ThrowsAsync<ForbiddenException>(() => _sut.EscalateAsync(1, EscalateDto()));
        }

        [Test]
        public async Task EscalateAsync_StatusNotInProgress_ThrowsBusinessRuleException()
        {
            SetupCurrentUser(employeeId: 20, roleId: ROLE_GRO);
            var complaint = BuildComplaint(statusId: STATUS_ASSIGNED, currentHandler: 20);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);

            Assert.ThrowsAsync<BusinessRuleException>(() => _sut.EscalateAsync(1, EscalateDto()));
        }

        [Test]
        public async Task EscalateAsync_AdminRole_ThrowsBusinessRuleException()
        {
            SetupCurrentUser(employeeId: 20, roleId: ROLE_ADMIN, role: "ADMIN");
            var complaint = BuildComplaint(statusId: STATUS_IN_PROGRESS, currentHandler: 20);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);

            Assert.ThrowsAsync<BusinessRuleException>(() => _sut.EscalateAsync(1, EscalateDto()));
        }

        [Test]
        public async Task EscalateAsync_GroNoDepartmentHead_ThrowsBusinessRuleException()
        {
            SetupCurrentUser(employeeId: 20, roleId: ROLE_GRO, role: "GRO");
            var complaint = BuildComplaint(statusId: STATUS_IN_PROGRESS, currentHandler: 20);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);
            _employeeRepo
                .Setup(r => r.GetDepartmentHeadAsync(It.IsAny<int>()))
                .ReturnsAsync((Employee?)null);

            Assert.ThrowsAsync<BusinessRuleException>(() => _sut.EscalateAsync(1, EscalateDto()));
        }

        [Test]
        public async Task EscalateAsync_GroToHead_EscalatesWithLevel1()
        {
            SetupCurrentUser(employeeId: 20, roleId: ROLE_GRO, role: "GRO");
            var complaint = BuildComplaint(
                statusId: STATUS_IN_PROGRESS, currentHandler: 20, departmentId: 5);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);

            var head = new Employee
            {
                EmployeeId   = 50,
                EmployeeName = "Dept Head",
                Email        = "head@test.com",
                MobileNumber = "0987654321",
                PasswordHash = "hash",
                RoleId       = ROLE_DEPARTMENT_HEAD,
                IsActive     = true,
                CreatedAt    = DateTime.UtcNow,
                UpdatedAt    = DateTime.UtcNow
            };
            _employeeRepo.Setup(r => r.GetDepartmentHeadAsync(5)).ReturnsAsync(head);
            _escalationRepo
                .Setup(r => r.Create(It.IsAny<ComplaintEscalation>()))
                .ReturnsAsync(new ComplaintEscalation());

            await _sut.EscalateAsync(1, EscalateDto());

            _complaintRepo.Verify(r => r.Update(
                It.Is<Complaint>(c =>
                    c.CurrentHandlerEmployeeId == 50 &&
                    c.EscalationLevel          == 1),
                1), Times.Once);
        }

        [Test]
        public async Task EscalateAsync_HeadToAdmin_EscalatesWithLevel2()
        {
            SetupCurrentUser(employeeId: 50, roleId: ROLE_DEPARTMENT_HEAD, role: "DEPARTMENT_HEAD");
            var complaint = BuildComplaint(
                statusId: STATUS_IN_PROGRESS, currentHandler: 50, departmentId: 5);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);

            var admin = new Employee
            {
                EmployeeId   = 1,
                EmployeeName = "Admin",
                Email        = "admin@test.com",
                MobileNumber = "1111111111",
                PasswordHash = "hash",
                RoleId       = ROLE_ADMIN,
                IsActive     = true,
                CreatedAt    = DateTime.UtcNow,
                UpdatedAt    = DateTime.UtcNow
            };
            _employeeRepo.Setup(r => r.GetAdminAsync()).ReturnsAsync(admin);
            _escalationRepo
                .Setup(r => r.Create(It.IsAny<ComplaintEscalation>()))
                .ReturnsAsync(new ComplaintEscalation());

            await _sut.EscalateAsync(1, EscalateDto());

            _complaintRepo.Verify(r => r.Update(
                It.Is<Complaint>(c =>
                    c.CurrentHandlerEmployeeId == 1 &&
                    c.EscalationLevel          == 2),
                1), Times.Once);
        }

        [Test]
        public async Task EscalateAsync_HeadNoAdmin_ThrowsBusinessRuleException()
        {
            SetupCurrentUser(employeeId: 50, roleId: ROLE_DEPARTMENT_HEAD, role: "DEPARTMENT_HEAD");
            var complaint = BuildComplaint(statusId: STATUS_IN_PROGRESS, currentHandler: 50);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);
            _employeeRepo.Setup(r => r.GetAdminAsync()).ReturnsAsync((Employee?)null);

            Assert.ThrowsAsync<BusinessRuleException>(() => _sut.EscalateAsync(1, EscalateDto()));
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  GetHistoryAsync
    // ═══════════════════════════════════════════════════════════════════
    [TestFixture]
    public class GetHistoryAsync_Tests : ComplaintServiceTests
    {
        [Test]
        public async Task GetHistoryAsync_ComplaintNotFound_ThrowsNotFoundException()
        {
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync((Complaint?)null);

            Assert.ThrowsAsync<NotFoundException>(() => _sut.GetHistoryAsync(1));
        }

        [Test]
        public async Task GetHistoryAsync_UnauthorisedUser_ThrowsForbiddenException()
        {
            SetupCurrentUser(employeeId: 99, roleId: ROLE_EMPLOYEE);
            var complaint = BuildComplaint(raisedBy: 10, currentHandler: 20);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);

            Assert.ThrowsAsync<ForbiddenException>(() => _sut.GetHistoryAsync(1));
        }

        [Test]
        public async Task GetHistoryAsync_AuthorisedUser_ReturnsHistoryDtos()
        {
            SetupCurrentUser(employeeId: 10, roleId: ROLE_EMPLOYEE);
            var complaint = BuildComplaint(raisedBy: 10, currentHandler: 20);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);

            _historyRepo
                .Setup(r => r.GetByComplaintIdAsync(1))
                .ReturnsAsync(new List<ComplaintHistory>
                {
                    new()
                    {
                        HistoryId    = 1,
                        ComplaintId  = 1,
                        OldStatusId  = STATUS_SUBMITTED,
                        NewStatusId  = STATUS_ASSIGNED,
                        CreatedAt    = DateTime.UtcNow
                    }
                });

            var result = (await _sut.GetHistoryAsync(1)).ToList();

            Assert.That(result, Has.Count.EqualTo(1));
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  GetAssignmentHistoryAsync
    // ═══════════════════════════════════════════════════════════════════
    [TestFixture]
    public class GetAssignmentHistoryAsync_Tests : ComplaintServiceTests
    {
        [Test]
        public async Task GetAssignmentHistoryAsync_ComplaintNotFound_ThrowsNotFoundException()
        {
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync((Complaint?)null);

            Assert.ThrowsAsync<NotFoundException>(() => _sut.GetAssignmentHistoryAsync(1));
        }

        [Test]
        public async Task GetAssignmentHistoryAsync_AdminUser_ReturnsDtos()
        {
            SetupCurrentUser(employeeId: 1, roleId: ROLE_ADMIN, role: "ADMIN");
            var complaint = BuildComplaint();
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);

            var assignment = new ComplaintAssignmentHistory
            {
                AssignmentHistoryId    = 1,
                ComplaintId            = 1,
                OldHandlerEmployeeId   = null,
                NewHandlerEmployeeId   = 20,
                AssignedBy             = null,
                AssignmentReason       = "INITIAL",
                AssignedAt             = DateTime.UtcNow,
                NewHandlerEmployee     = new Employee
                {
                    EmployeeId   = 20,
                    EmployeeName = "Handler",
                    Email        = "h@test.com",
                    MobileNumber = "0000000000",
                    PasswordHash = "x",
                    CreatedAt    = DateTime.UtcNow,
                    UpdatedAt    = DateTime.UtcNow
                }
            };

            _assignmentRepo
                .Setup(r => r.GetByComplaintIdAsync(1))
                .ReturnsAsync(new List<ComplaintAssignmentHistory> { assignment });

            var result = (await _sut.GetAssignmentHistoryAsync(1)).ToList();

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].NewHandlerEmployeeId, Is.EqualTo(20));
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  GetEscalationHistoryAsync
    // ═══════════════════════════════════════════════════════════════════
    [TestFixture]
    public class GetEscalationHistoryAsync_Tests : ComplaintServiceTests
    {
        [Test]
        public async Task GetEscalationHistoryAsync_ComplaintNotFound_ThrowsNotFoundException()
        {
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync((Complaint?)null);

            Assert.ThrowsAsync<NotFoundException>(() => _sut.GetEscalationHistoryAsync(1));
        }

        [Test]
        public async Task GetEscalationHistoryAsync_ReturnsEscalationDtos()
        {
            SetupCurrentUser(employeeId: 10, roleId: ROLE_EMPLOYEE);
            var complaint = BuildComplaint(raisedBy: 10, currentHandler: 20);
            _complaintRepo.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(complaint);

            _escalationRepo
                .Setup(r => r.GetByComplaintIdAsync(1))
                .ReturnsAsync(new List<ComplaintEscalation>
                {
                    new()
                    {
                        EscalationId            = 1,
                        ComplaintId             = 1,
                        EscalatedToEmployeeId   = 50,
                        EscalationLevel         = 1,
                        EscalatedByEmployeeId   = 20,
                        EscalatedAt             = DateTime.UtcNow,
                        EscalatedToEmployee = new Employee
                        {
                            EmployeeId   = 50,
                            EmployeeName = "Head",
                            Email        = "head@test.com",
                            MobileNumber = "0000000000",
                            PasswordHash = "x",
                            CreatedAt    = DateTime.UtcNow,
                            UpdatedAt    = DateTime.UtcNow
                        },
                        EscalatedByEmployee = new Employee
                        {
                            EmployeeId   = 20,
                            EmployeeName = "GRO",
                            Email        = "gro@test.com",
                            MobileNumber = "1111111111",
                            PasswordHash = "x",
                            CreatedAt    = DateTime.UtcNow,
                            UpdatedAt    = DateTime.UtcNow
                        }
                    }
                });

            var result = (await _sut.GetEscalationHistoryAsync(1)).ToList();

            Assert.That(result,               Has.Count.EqualTo(1));
            Assert.That(result[0].EscalationLevel, Is.EqualTo(1));
        }
    }
}
