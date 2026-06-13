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
using cgrmodellibrary.DTOs.ComplaintRequest;
using cgrmodellibrary.Exceptions;
using cgrmodellibrary.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace cgrtest.Services;

[TestFixture]
public class ComplaintRequestServiceTests
{
    private Mock<IComplaintRequestRepository> _mockRequestRepo = null!;
    private Mock<IComplaintRepository> _mockComplaintRepo = null!;
    private Mock<INotificationService> _mockNotificationService = null!;
    private Mock<ICurrentUserService> _mockUserService = null!;
    private Mock<IComplaintAssignmentRepository> _mockAssignmentRepo = null!;
    private Mock<IComplaintHistoryRepository> _mockHistoryRepo = null!;
    private Mock<IEmployeeRepository> _mockEmployeeRepo = null!;
    private Mock<IComplaintAssignmentEngine> _mockAssignmentEngine = null!;
    private CGRContext _context = null!;
    private Mock<ILogger<ComplaintRequestService>> _mockLogger = null!;
    private ComplaintRequestService _serviceSut = null!;

    private const short REQUEST_TYPE_REJECTION = 1;
    private const short ROLE_EMPLOYEE = 1;
    private const short ROLE_GRO = 2;
    private const short ROLE_DEPARTMENT_HEAD = 3;
    private const short ROLE_ADMIN = 4;

    private const short REQUEST_STATUS_PENDING = 1;
    private const short REQUEST_STATUS_APPROVED = 2;
    private const short REQUEST_STATUS_REJECTED = 3;

    private const short STATUS_ASSIGNED = 2;
    private const short STATUS_IN_PROGRESS = 3;
    private const short STATUS_ESCALATED = 4;
    private const short STATUS_RESOLVED = 5;
    private const short STATUS_REJECTED = 7;
    private const short STATUS_REOPENED = 8;

    private const short PRIORITY_CRITICAL = 4;

    [SetUp]
    public void SetUp()
    {
        _mockRequestRepo = new Mock<IComplaintRequestRepository>();
        _mockComplaintRepo = new Mock<IComplaintRepository>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockUserService = new Mock<ICurrentUserService>();
        _mockAssignmentRepo = new Mock<IComplaintAssignmentRepository>();
        _mockHistoryRepo = new Mock<IComplaintHistoryRepository>();
        _mockEmployeeRepo = new Mock<IEmployeeRepository>();
        _mockAssignmentEngine = new Mock<IComplaintAssignmentEngine>();
        _mockLogger = new Mock<ILogger<ComplaintRequestService>>();

        var options = new DbContextOptionsBuilder<CGRContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w =>
                w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _context = new CGRContext(options);

        _serviceSut = new ComplaintRequestService(
            _mockRequestRepo.Object,
            _mockComplaintRepo.Object,
            _mockNotificationService.Object,
            _mockUserService.Object,
            _mockAssignmentRepo.Object,
            _mockHistoryRepo.Object,
            _mockEmployeeRepo.Object,
            _mockAssignmentEngine.Object,
            _context,
            _mockLogger.Object
        );
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    #region CreateAsync Tests

    [Test]
    public void CreateAsync_NotAuthenticated_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _mockUserService.Setup(u => u.IsAuthenticated).Returns(false);

        // Act & Assert
        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _serviceSut.CreateAsync(1, new CreateComplaintRequestDto()));
    }

    [Test]
    public void CreateAsync_ComplaintNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _mockUserService.Setup(u => u.IsAuthenticated).Returns(true);
        _mockComplaintRepo.Setup(c => c.GetDetailByIdAsync(1)).ReturnsAsync((Complaint?)null);

        // Act & Assert
        Assert.ThrowsAsync<NotFoundException>(() =>
            _serviceSut.CreateAsync(1, new CreateComplaintRequestDto()));
    }

    [Test]
    public void CreateAsync_UserNotCurrentHandler_ThrowsForbiddenException()
    {
        // Arrange
        _mockUserService.Setup(u => u.IsAuthenticated).Returns(true);
        _mockUserService.Setup(u => u.EmployeeId).Returns(10);
        var complaint = new Complaint { ComplaintId = 1, CurrentHandlerEmployeeId = 20 };
        _mockComplaintRepo.Setup(c => c.GetDetailByIdAsync(1)).ReturnsAsync(complaint);

        // Act & Assert
        var ex = Assert.ThrowsAsync<ForbiddenException>(() =>
            _serviceSut.CreateAsync(1, new CreateComplaintRequestDto()));
        Assert.That(ex!.Message, Is.EqualTo("Only the current handler can raise a rejection request."));
    }

    [TestCase(ROLE_EMPLOYEE)]
    [TestCase(ROLE_ADMIN)]
    public void CreateAsync_UserNotGroOrDeptHead_ThrowsForbiddenException(short roleId)
    {
        // Arrange
        _mockUserService.Setup(u => u.IsAuthenticated).Returns(true);
        _mockUserService.Setup(u => u.EmployeeId).Returns(10);
        _mockUserService.Setup(u => u.RoleId).Returns(roleId);

        var complaint = new Complaint { ComplaintId = 1, CurrentHandlerEmployeeId = 10 };
        _mockComplaintRepo.Setup(c => c.GetDetailByIdAsync(1)).ReturnsAsync(complaint);

        // Act & Assert
        var ex = Assert.ThrowsAsync<ForbiddenException>(() =>
            _serviceSut.CreateAsync(1, new CreateComplaintRequestDto()));
        Assert.That(ex!.Message, Is.EqualTo("Only GROs and Department Heads can create rejection requests."));
    }

    [Test]
    public void CreateAsync_RequestTypeNotRejection_ThrowsBusinessRuleException()
    {
        // Arrange
        _mockUserService.Setup(u => u.IsAuthenticated).Returns(true);
        _mockUserService.Setup(u => u.EmployeeId).Returns(10);
        _mockUserService.Setup(u => u.RoleId).Returns(ROLE_GRO);

        var complaint = new Complaint { ComplaintId = 1, CurrentHandlerEmployeeId = 10 };
        _mockComplaintRepo.Setup(c => c.GetDetailByIdAsync(1)).ReturnsAsync(complaint);

        var dto = new CreateComplaintRequestDto { RequestTypeId = 2 }; // Not 1 (rejection)

        // Act & Assert
        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            _serviceSut.CreateAsync(1, dto));
        Assert.That(ex!.Message, Is.EqualTo("Only rejection requests are supported."));
    }

    [TestCase(STATUS_RESOLVED)]
    [TestCase(STATUS_REJECTED)]
    [TestCase(STATUS_REOPENED)]
    public void CreateAsync_InvalidComplaintStatus_ThrowsBusinessRuleException(short statusId)
    {
        // Arrange
        _mockUserService.Setup(u => u.IsAuthenticated).Returns(true);
        _mockUserService.Setup(u => u.EmployeeId).Returns(10);
        _mockUserService.Setup(u => u.RoleId).Returns(ROLE_GRO);

        var complaint = new Complaint { ComplaintId = 1, CurrentHandlerEmployeeId = 10, StatusId = statusId };
        _mockComplaintRepo.Setup(c => c.GetDetailByIdAsync(1)).ReturnsAsync(complaint);

        var dto = new CreateComplaintRequestDto { RequestTypeId = REQUEST_TYPE_REJECTION };

        // Act & Assert
        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            _serviceSut.CreateAsync(1, dto));
        Assert.That(ex!.Message, Is.EqualTo("Rejection request cannot be created for this complaint status."));
    }

    [Test]
    public void CreateAsync_PendingRequestAlreadyExists_ThrowsConflictException()
    {
        // Arrange
        _mockUserService.Setup(u => u.IsAuthenticated).Returns(true);
        _mockUserService.Setup(u => u.EmployeeId).Returns(10);
        _mockUserService.Setup(u => u.RoleId).Returns(ROLE_GRO);

        var complaint = new Complaint { ComplaintId = 1, CurrentHandlerEmployeeId = 10, StatusId = STATUS_ASSIGNED };
        _mockComplaintRepo.Setup(c => c.GetDetailByIdAsync(1)).ReturnsAsync(complaint);

        _mockRequestRepo.Setup(r => r.GetPendingByComplaintAndTypeAsync(1, REQUEST_TYPE_REJECTION))
            .ReturnsAsync(new ComplaintRequest());

        var dto = new CreateComplaintRequestDto { RequestTypeId = REQUEST_TYPE_REJECTION };

        // Act & Assert
        var ex = Assert.ThrowsAsync<ConflictException>(() =>
            _serviceSut.CreateAsync(1, dto));
        Assert.That(ex!.Message, Is.EqualTo("A pending rejection request already exists."));
    }

    [Test]
    public async Task CreateAsync_ValidRequest_CreatesAndReturnsDto()
    {
        // Arrange
        _mockUserService.Setup(u => u.IsAuthenticated).Returns(true);
        _mockUserService.Setup(u => u.EmployeeId).Returns(10);
        _mockUserService.Setup(u => u.RoleId).Returns(ROLE_GRO);

        var complaint = new Complaint { ComplaintId = 1, CurrentHandlerEmployeeId = 10, StatusId = STATUS_ASSIGNED };
        _mockComplaintRepo.Setup(c => c.GetDetailByIdAsync(1)).ReturnsAsync(complaint);

        _mockRequestRepo.Setup(r => r.GetPendingByComplaintAndTypeAsync(1, REQUEST_TYPE_REJECTION))
            .ReturnsAsync((ComplaintRequest?)null);

        var requestToCreate = new ComplaintRequest
        {
            RequestId = 100,
            ComplaintId = 1,
            RequestTypeId = REQUEST_TYPE_REJECTION,
            RequestedBy = 10,
            RequestStatusId = REQUEST_STATUS_PENDING,
            Remarks = "remarks test"
        };

        _mockRequestRepo.Setup(r => r.Create(It.IsAny<ComplaintRequest>()))
            .ReturnsAsync(requestToCreate);

        // For ReloadDto
        _mockRequestRepo.Setup(r => r.GetDetailAsync(100))
            .ReturnsAsync(requestToCreate);

        var dto = new CreateComplaintRequestDto { RequestTypeId = REQUEST_TYPE_REJECTION, Remarks = "remarks test" };

        // Act
        var result = await _serviceSut.CreateAsync(1, dto);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.RequestId, Is.EqualTo(100));
        Assert.That(result.ComplaintId, Is.EqualTo(1));
        Assert.That(result.Remarks, Is.EqualTo("remarks test"));

        _mockRequestRepo.Verify(r => r.Create(It.Is<ComplaintRequest>(req =>
            req.ComplaintId == 1 &&
            req.RequestTypeId == REQUEST_TYPE_REJECTION &&
            req.RequestedBy == 10 &&
            req.RequestStatusId == REQUEST_STATUS_PENDING &&
            req.Remarks == "remarks test"
        )), Times.Once);
    }

    #endregion

    #region ReviewAsync Tests

    [Test]
    public void ReviewAsync_NotAuthenticated_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _mockUserService.Setup(u => u.IsAuthenticated).Returns(false);

        // Act & Assert
        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _serviceSut.ReviewAsync(1, new ReviewComplaintRequestDto()));
    }

    [TestCase(ROLE_EMPLOYEE)]
    [TestCase(ROLE_GRO)]
    [TestCase(ROLE_DEPARTMENT_HEAD)]
    public void ReviewAsync_NotAdmin_ThrowsForbiddenException(short roleId)
    {
        // Arrange
        _mockUserService.Setup(u => u.IsAuthenticated).Returns(true);
        _mockUserService.Setup(u => u.RoleId).Returns(roleId);

        // Act & Assert
        var ex = Assert.ThrowsAsync<ForbiddenException>(() =>
            _serviceSut.ReviewAsync(1, new ReviewComplaintRequestDto()));
        Assert.That(ex!.Message, Is.EqualTo("Only admins can review complaint requests."));
    }

    [Test]
    public void ReviewAsync_RequestNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _mockUserService.Setup(u => u.IsAuthenticated).Returns(true);
        _mockUserService.Setup(u => u.RoleId).Returns(ROLE_ADMIN);
        _mockRequestRepo.Setup(r => r.GetDetailAsync(1)).ReturnsAsync((ComplaintRequest?)null);

        // Act & Assert
        Assert.ThrowsAsync<NotFoundException>(() =>
            _serviceSut.ReviewAsync(1, new ReviewComplaintRequestDto()));
    }

    [Test]
    public void ReviewAsync_RequestTypeNotRejection_ThrowsBusinessRuleException()
    {
        // Arrange
        _mockUserService.Setup(u => u.IsAuthenticated).Returns(true);
        _mockUserService.Setup(u => u.RoleId).Returns(ROLE_ADMIN);

        var request = new ComplaintRequest { RequestId = 1, RequestTypeId = 2 /* not rejection */ };
        _mockRequestRepo.Setup(r => r.GetDetailAsync(1)).ReturnsAsync(request);

        // Act & Assert
        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            _serviceSut.ReviewAsync(1, new ReviewComplaintRequestDto()));
        Assert.That(ex!.Message, Is.EqualTo("Only rejection requests can be reviewed."));
    }

    [TestCase(REQUEST_STATUS_APPROVED)]
    [TestCase(REQUEST_STATUS_REJECTED)]
    public void ReviewAsync_AlreadyReviewed_ThrowsBusinessRuleException(short statusId)
    {
        // Arrange
        _mockUserService.Setup(u => u.IsAuthenticated).Returns(true);
        _mockUserService.Setup(u => u.RoleId).Returns(ROLE_ADMIN);

        var request = new ComplaintRequest
        {
            RequestId = 1,
            RequestTypeId = REQUEST_TYPE_REJECTION,
            RequestStatusId = statusId
        };
        _mockRequestRepo.Setup(r => r.GetDetailAsync(1)).ReturnsAsync(request);

        // Act & Assert
        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            _serviceSut.ReviewAsync(1, new ReviewComplaintRequestDto()));
        Assert.That(ex!.Message, Is.EqualTo("This request has already been reviewed."));
    }

    [Test]
    public void ReviewAsync_ComplaintNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _mockUserService.Setup(u => u.IsAuthenticated).Returns(true);
        _mockUserService.Setup(u => u.RoleId).Returns(ROLE_ADMIN);

        var request = new ComplaintRequest
        {
            RequestId = 1,
            RequestTypeId = REQUEST_TYPE_REJECTION,
            RequestStatusId = REQUEST_STATUS_PENDING,
            ComplaintId = 10
        };
        _mockRequestRepo.Setup(r => r.GetDetailAsync(1)).ReturnsAsync(request);
        _mockComplaintRepo.Setup(c => c.GetDetailByIdAsync(10)).ReturnsAsync((Complaint?)null);

        // Act & Assert
        Assert.ThrowsAsync<NotFoundException>(() =>
            _serviceSut.ReviewAsync(1, new ReviewComplaintRequestDto { Approve = true }));
    }

    [Test]
    public async Task ReviewAsync_ApproveTrue_UpdatesStatusAndComplaintToRejected()
    {
        // Arrange
        _mockUserService.Setup(u => u.IsAuthenticated).Returns(true);
        _mockUserService.Setup(u => u.RoleId).Returns(ROLE_ADMIN);
        _mockUserService.Setup(u => u.EmployeeId).Returns(99);

        var request = new ComplaintRequest
        {
            RequestId = 1,
            RequestTypeId = REQUEST_TYPE_REJECTION,
            RequestStatusId = REQUEST_STATUS_PENDING,
            ComplaintId = 10,
            Remarks = "original remarks"
        };
        _mockRequestRepo.Setup(r => r.GetDetailAsync(1)).ReturnsAsync(request);

        var complaint = new Complaint
        {
            ComplaintId = 10,
            StatusId = STATUS_ASSIGNED,
            CurrentHandlerEmployeeId = 20,
            RaisedByEmployeeId = 5,
            ComplaintTitle = "Broken chair"
        };
        _mockComplaintRepo.Setup(c => c.GetDetailByIdAsync(10)).ReturnsAsync(complaint);

        var dto = new ReviewComplaintRequestDto { Approve = true, Remarks = "approved remarks" };

        // Act
        var result = await _serviceSut.ReviewAsync(1, dto);

        // Assert
        Assert.That(request.RequestStatusId, Is.EqualTo(REQUEST_STATUS_APPROVED));
        Assert.That(request.ReviewedBy, Is.EqualTo(99));
        Assert.That(request.Remarks, Is.EqualTo("approved remarks"));
        Assert.That(complaint.StatusId, Is.EqualTo(STATUS_REJECTED));

        _mockRequestRepo.Verify(r => r.Update(request, 1), Times.Once);
        _mockComplaintRepo.Verify(c => c.Update(complaint, 10), Times.Once);

        _mockHistoryRepo.Verify(h => h.Create(It.Is<ComplaintHistory>(hist =>
            hist.ComplaintId == 10 &&
            hist.OldStatusId == STATUS_ASSIGNED &&
            hist.NewStatusId == STATUS_REJECTED &&
            hist.OldHandlerEmployeeId == 20 &&
            hist.NewHandlerEmployeeId == 20 &&
            hist.ChangedBy == 99 &&
            hist.Remarks == "Rejection request approved :approved remarks" &&
            hist.RoleIdAtActionTime == ROLE_ADMIN
        )), Times.Once);

        _mockNotificationService.Verify(n => n.SendAsync(
            5,
            6 /* NOTIF_COMPLAINT_REJECTED */,
            "Complaint Rejected",
            "Your complaint 'Broken chair' has been rejected.",
            10
        ), Times.Once);
    }

    [Test]
    public async Task ReviewAsync_ApproveTrueRemarksNull_AppendsDefaultMessage()
    {
        // Arrange
        _mockUserService.Setup(u => u.IsAuthenticated).Returns(true);
        _mockUserService.Setup(u => u.RoleId).Returns(ROLE_ADMIN);
        _mockUserService.Setup(u => u.EmployeeId).Returns(99);

        var request = new ComplaintRequest
        {
            RequestId = 1,
            RequestTypeId = REQUEST_TYPE_REJECTION,
            RequestStatusId = REQUEST_STATUS_PENDING,
            ComplaintId = 10,
            Remarks = "original remarks "
        };
        _mockRequestRepo.Setup(r => r.GetDetailAsync(1)).ReturnsAsync(request);

        var complaint = new Complaint
        {
            ComplaintId = 10,
            StatusId = STATUS_ASSIGNED,
            CurrentHandlerEmployeeId = 20,
            RaisedByEmployeeId = 5,
            ComplaintTitle = "Broken chair"
        };
        _mockComplaintRepo.Setup(c => c.GetDetailByIdAsync(10)).ReturnsAsync(complaint);

        var dto = new ReviewComplaintRequestDto { Approve = true, Remarks = null };

        // Act
        await _serviceSut.ReviewAsync(1, dto);

        // Assert
        Assert.That(request.Remarks, Is.EqualTo("original remarks No remarks provided by reviewer."));
    }

    [Test]
    public async Task ReviewAsync_ApproveTrueRemarksAndRequestRemarksNull_SetsDefaultRemarks()
    {
        // Arrange
        _mockUserService.Setup(u => u.IsAuthenticated).Returns(true);
        _mockUserService.Setup(u => u.RoleId).Returns(ROLE_ADMIN);
        _mockUserService.Setup(u => u.EmployeeId).Returns(99);

        var request = new ComplaintRequest
        {
            RequestId = 1,
            RequestTypeId = REQUEST_TYPE_REJECTION,
            RequestStatusId = REQUEST_STATUS_PENDING,
            ComplaintId = 10,
            Remarks = null
        };
        _mockRequestRepo.Setup(r => r.GetDetailAsync(1)).ReturnsAsync(request);

        var complaint = new Complaint
        {
            ComplaintId = 10,
            StatusId = STATUS_ASSIGNED,
            CurrentHandlerEmployeeId = 20,
            RaisedByEmployeeId = 5,
            ComplaintTitle = "Broken chair"
        };
        _mockComplaintRepo.Setup(c => c.GetDetailByIdAsync(10)).ReturnsAsync(complaint);

        var dto = new ReviewComplaintRequestDto { Approve = true, Remarks = null };

        // Act
        await _serviceSut.ReviewAsync(1, dto);

        // Assert
        Assert.That(request.Remarks, Is.EqualTo("No remarks provided by reviewer."));
    }

    [Test]
    public void ReviewAsync_ApproveFalseMaxReopensReached_ThrowsBusinessRuleException()
    {
        // Arrange
        _mockUserService.Setup(u => u.IsAuthenticated).Returns(true);
        _mockUserService.Setup(u => u.RoleId).Returns(ROLE_ADMIN);
        _mockUserService.Setup(u => u.EmployeeId).Returns(99);

        var request = new ComplaintRequest
        {
            RequestId = 1,
            RequestTypeId = REQUEST_TYPE_REJECTION,
            RequestStatusId = REQUEST_STATUS_PENDING,
            ComplaintId = 10,
            Remarks = "remarks"
        };
        _mockRequestRepo.Setup(r => r.GetDetailAsync(1)).ReturnsAsync(request);

        var complaint = new Complaint
        {
            ComplaintId = 10,
            StatusId = STATUS_ASSIGNED,
            ReopenedCount = 3
        };
        _mockComplaintRepo.Setup(c => c.GetDetailByIdAsync(10)).ReturnsAsync(complaint);

        var dto = new ReviewComplaintRequestDto { Approve = false };

        // Act & Assert
        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            _serviceSut.ReviewAsync(1, dto));
        Assert.That(ex!.Message, Is.EqualTo("Cannot reopen complaint. Maximum reopen limit reached. Approve the rejection instead."));
    }

    [Test]
    public void ReviewAsync_ApproveFalseRaiserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _mockUserService.Setup(u => u.IsAuthenticated).Returns(true);
        _mockUserService.Setup(u => u.RoleId).Returns(ROLE_ADMIN);

        var request = new ComplaintRequest
        {
            RequestId = 1,
            RequestTypeId = REQUEST_TYPE_REJECTION,
            RequestStatusId = REQUEST_STATUS_PENDING,
            ComplaintId = 10
        };
        _mockRequestRepo.Setup(r => r.GetDetailAsync(1)).ReturnsAsync(request);

        var complaint = new Complaint
        {
            ComplaintId = 10,
            StatusId = STATUS_ASSIGNED,
            ReopenedCount = 2,
            RaisedByEmployeeId = 5,
            RaisedByEmployee = null // Triggers throw on line 214
        };
        _mockComplaintRepo.Setup(c => c.GetDetailByIdAsync(10)).ReturnsAsync(complaint);

        var dto = new ReviewComplaintRequestDto { Approve = false };

        // Act & Assert
        var ex = Assert.ThrowsAsync<NotFoundException>(() =>
            _serviceSut.ReviewAsync(1, dto));
        Assert.That(ex!.Message, Is.EqualTo("Employee 5 who raised the complaint"));
    }

    [Test]
    public async Task ReviewAsync_ApproveFalsePriorityBelowCritical_BumpsPriorityAndReassigns()
    {
        // Arrange
        _mockUserService.Setup(u => u.IsAuthenticated).Returns(true);
        _mockUserService.Setup(u => u.RoleId).Returns(ROLE_ADMIN);
        _mockUserService.Setup(u => u.EmployeeId).Returns(99);

        var request = new ComplaintRequest
        {
            RequestId = 1,
            RequestTypeId = REQUEST_TYPE_REJECTION,
            RequestStatusId = REQUEST_STATUS_PENDING,
            ComplaintId = 10
        };
        _mockRequestRepo.Setup(r => r.GetDetailAsync(1)).ReturnsAsync(request);

        var category = new Category { DepartmentId = 3 };
        var raiser = new Employee { EmployeeId = 5, RoleId = ROLE_EMPLOYEE };
        var complaint = new Complaint
        {
            ComplaintId = 10,
            StatusId = STATUS_ASSIGNED,
            ReopenedCount = 1,
            PriorityId = 2, // Below PRIORITY_CRITICAL (4)
            RaisedByEmployeeId = 5,
            RaisedByEmployee = raiser,
            Category = category,
            CurrentHandlerEmployeeId = 20,
            EscalationLevel = 1,
            ComplaintTitle = "Light not working"
        };
        _mockComplaintRepo.Setup(c => c.GetDetailByIdAsync(10)).ReturnsAsync(complaint);

        var initialAssignment = (30, (short)0);
        _mockAssignmentEngine.Setup(e => e.DetermineInitialAssignmentAsync(10, 3, ROLE_EMPLOYEE))
            .ReturnsAsync(initialAssignment);

        var expectedDueAt = DateTime.UtcNow.AddDays(2);
        _mockAssignmentEngine.Setup(e => e.CalculateEscalationDueAtAsync(complaint))
            .ReturnsAsync(expectedDueAt);

        var dto = new ReviewComplaintRequestDto { Approve = false };

        // Act
        var result = await _serviceSut.ReviewAsync(1, dto);

        // Assert
        Assert.That(complaint.PriorityId, Is.EqualTo(3)); // Bumped from 2
        Assert.That(complaint.ReopenedCount, Is.EqualTo(2));
        Assert.That(complaint.StatusId, Is.EqualTo(STATUS_ASSIGNED));
        Assert.That(complaint.CurrentHandlerEmployeeId, Is.EqualTo(30));
        Assert.That(complaint.EscalationLevel, Is.EqualTo(0));
        Assert.That(complaint.EscalationDueAt, Is.EqualTo(expectedDueAt));

        // Reopen history
        _mockHistoryRepo.Verify(h => h.Create(It.Is<ComplaintHistory>(hist =>
            hist.ComplaintId == 10 &&
            hist.OldStatusId == STATUS_ASSIGNED &&
            hist.NewStatusId == STATUS_REOPENED &&
            hist.OldHandlerEmployeeId == 20 &&
            hist.NewHandlerEmployeeId == 20 &&
            hist.ChangedBy == 99 &&
            hist.Remarks == "Rejection request rejected. Complaint reopened."
        )), Times.Once);

        // Reassign history
        _mockHistoryRepo.Verify(h => h.Create(It.Is<ComplaintHistory>(hist =>
            hist.ComplaintId == 10 &&
            hist.OldStatusId == STATUS_REOPENED &&
            hist.NewStatusId == STATUS_ASSIGNED &&
            hist.OldHandlerEmployeeId == 20 &&
            hist.NewHandlerEmployeeId == 30 &&
            hist.Remarks == "Complaint reassigned after reopen."
        )), Times.Once);

        // Assignment History
        _mockAssignmentRepo.Verify(a => a.Create(It.Is<ComplaintAssignmentHistory>(ah =>
            ah.ComplaintId == 10 &&
            ah.OldHandlerEmployeeId == 20 &&
            ah.NewHandlerEmployeeId == 30 &&
            ah.AssignmentReason == "REOPEN"
        )), Times.Once);

        // Notifications
        _mockNotificationService.Verify(n => n.SendAsync(
            30,
            7 /* NOTIF_COMPLAINT_REOPENED */,
            "Complaint Assigned",
            "Complaint 'Light not working' has been reopened and assigned to you.",
            10
        ), Times.Once);

        _mockNotificationService.Verify(n => n.SendAsync(
            5,
            7 /* NOTIF_COMPLAINT_REOPENED */,
            "Complaint Reopened",
            "Your complaint 'Light not working' has been reopened.",
            10
        ), Times.Once);
    }

    [Test]
    public async Task ReviewAsync_ApproveFalsePriorityAtCritical_DoesNotBumpPriority()
    {
        // Arrange
        _mockUserService.Setup(u => u.IsAuthenticated).Returns(true);
        _mockUserService.Setup(u => u.RoleId).Returns(ROLE_ADMIN);
        _mockUserService.Setup(u => u.EmployeeId).Returns(99);

        var request = new ComplaintRequest
        {
            RequestId = 1,
            RequestTypeId = REQUEST_TYPE_REJECTION,
            RequestStatusId = REQUEST_STATUS_PENDING,
            ComplaintId = 10
        };
        _mockRequestRepo.Setup(r => r.GetDetailAsync(1)).ReturnsAsync(request);

        var category = new Category { DepartmentId = 3 };
        var raiser = new Employee { EmployeeId = 5, RoleId = ROLE_EMPLOYEE };
        var complaint = new Complaint
        {
            ComplaintId = 10,
            StatusId = STATUS_ASSIGNED,
            ReopenedCount = 1,
            PriorityId = PRIORITY_CRITICAL, // At critical
            RaisedByEmployeeId = 5,
            RaisedByEmployee = raiser,
            Category = category,
            CurrentHandlerEmployeeId = 20,
            EscalationLevel = 1,
            ComplaintTitle = "Water leakage"
        };
        _mockComplaintRepo.Setup(c => c.GetDetailByIdAsync(10)).ReturnsAsync(complaint);

        var initialAssignment = (30, (short)0);
        _mockAssignmentEngine.Setup(e => e.DetermineInitialAssignmentAsync(10, 3, ROLE_EMPLOYEE))
            .ReturnsAsync(initialAssignment);

        var dto = new ReviewComplaintRequestDto { Approve = false };

        // Act
        await _serviceSut.ReviewAsync(1, dto);

        // Assert
        Assert.That(complaint.PriorityId, Is.EqualTo(PRIORITY_CRITICAL)); // No bump
    }

    [Test]
    public void ReviewAsync_ExceptionThrown_RollsBackTransaction()
    {
        // Arrange
        _mockUserService.Setup(u => u.IsAuthenticated).Returns(true);
        _mockUserService.Setup(u => u.RoleId).Returns(ROLE_ADMIN);
        _mockUserService.Setup(u => u.EmployeeId).Returns(99);

        var request = new ComplaintRequest
        {
            RequestId = 1,
            RequestTypeId = REQUEST_TYPE_REJECTION,
            RequestStatusId = REQUEST_STATUS_PENDING,
            ComplaintId = 10
        };
        _mockRequestRepo.Setup(r => r.GetDetailAsync(1)).ReturnsAsync(request);

        // Force exception when updating complaint
        _mockComplaintRepo.Setup(c => c.GetDetailByIdAsync(10)).ThrowsAsync(new InvalidOperationException("DB Crash"));

        var dto = new ReviewComplaintRequestDto { Approve = true };

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _serviceSut.ReviewAsync(1, dto));
    }

    #endregion

    #region GetPagedAsync Tests

    [Test]
    public async Task GetPagedAsync_CallsRepositoryAndMapsToDto()
    {
        // Arrange
        var requestList = new List<ComplaintRequest>
        {
            new()
            {
                RequestId = 1,
                ComplaintId = 10,
                RequestTypeId = REQUEST_TYPE_REJECTION,
                RequestedBy = 5,
                RequestStatusId = REQUEST_STATUS_PENDING,
                Remarks = "first",
                Complaint = new Complaint { ComplaintTitle = "Title A" },
                RequestType = new RequestType { RequestTypeName = "RejectionType" },
                RequestedByNavigation = new Employee { EmployeeName = "Alice" },
                ReviewedByNavigation = null,
                RequestStatus = new RequestStatus { StatusName = "Pending" }
            },
            new()
            {
                RequestId = 2,
                ComplaintId = 20,
                RequestTypeId = REQUEST_TYPE_REJECTION,
                RequestedBy = 6,
                ReviewedBy = 99,
                RequestStatusId = REQUEST_STATUS_APPROVED,
                Remarks = "second",
                Complaint = null, // Verify null checks
                RequestType = null,
                RequestedByNavigation = null,
                ReviewedByNavigation = new Employee { EmployeeName = "Admin Bob" },
                RequestStatus = null
            }
        };

        _mockRequestRepo.Setup(r => r.GetPagedAsync(1, 10, REQUEST_TYPE_REJECTION, REQUEST_STATUS_PENDING, 5))
            .ReturnsAsync((requestList, 2));

        // Act
        var result = await _serviceSut.GetPagedAsync(1, 10, REQUEST_STATUS_PENDING, 5);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Page, Is.EqualTo(1));
        Assert.That(result.PageSize, Is.EqualTo(10));
        Assert.That(result.TotalCount, Is.EqualTo(2));
        
        var itemsList = result.Items.ToList();
        Assert.That(itemsList, Has.Count.EqualTo(2));

        var dto1 = itemsList[0];
        Assert.That(dto1.RequestId, Is.EqualTo(1));
        Assert.That(dto1.ComplaintTitle, Is.EqualTo("Title A"));
        Assert.That(dto1.RequestTypeName, Is.EqualTo("RejectionType"));
        Assert.That(dto1.RequestedByName, Is.EqualTo("Alice"));
        Assert.That(dto1.ReviewedByName, Is.Null);
        Assert.That(dto1.RequestStatusName, Is.EqualTo("Pending"));

        var dto2 = itemsList[1];
        Assert.That(dto2.RequestId, Is.EqualTo(2));
        Assert.That(dto2.ComplaintTitle, Is.Null);
        Assert.That(dto2.RequestTypeName, Is.EqualTo(string.Empty));
        Assert.That(dto2.RequestedByName, Is.EqualTo(string.Empty));
        Assert.That(dto2.ReviewedByName, Is.EqualTo("Admin Bob"));
        Assert.That(dto2.RequestStatusName, Is.EqualTo(string.Empty));
    }

    #endregion
}
