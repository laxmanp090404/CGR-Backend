using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using cgrmodellibrary.Models;
using cgrbussinesslogic.Services;
using cgrbussinesslogic.Interfaces;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.DTOs.Comment;
using cgrmodellibrary.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace cgrtest.Services
{
    [TestFixture]
    public class ComplaintCommentServiceTests
    {
        private Mock<IComplaintCommentRepository> _commentRepoMock;
        private Mock<IComplaintRepository> _complaintRepoMock;
        private Mock<ICurrentUserService> _currentUserMock;
        private ComplaintCommentService _service;

        private const short ROLE_ADMIN = 4;
        private const short ROLE_EMPLOYEE = 1;
        private const short ROLE_GRO = 2;
        private const short ROLE_DEPARTMENT_HEAD = 3;

        [SetUp]
        public void SetUp()
        {
            _commentRepoMock = new Mock<IComplaintCommentRepository>();
            _complaintRepoMock = new Mock<IComplaintRepository>();
            _currentUserMock = new Mock<ICurrentUserService>();
            _service = new ComplaintCommentService(_commentRepoMock.Object, _complaintRepoMock.Object, _currentUserMock.Object);
        }

        [Test]
        public async Task GetByComplaintIdAsync_AdminSeesAllComments()
        {
            // Arrange
            var complaint = new Complaint { ComplaintId = 10, RaisedByEmployeeId = 2 };
            _complaintRepoMock.Setup(r => r.GetDetailByIdAsync(10)).ReturnsAsync(complaint);
            _currentUserMock.Setup(u => u.RoleId).Returns(ROLE_ADMIN);
            var comments = new List<ComplaintComment>
            {
                new ComplaintComment { IsInternal = true },
                new ComplaintComment { IsInternal = false }
            };
            _commentRepoMock.Setup(r => r.GetByComplaintIdAsync(10)).ReturnsAsync(comments);

            // Act
            var result = await _service.GetByComplaintIdAsync(10);

            // Assert
            Assert.That(result.Count(), Is.EqualTo(2));
        }

        [Test]
        public async Task GetByComplaintIdAsync_GroSeesInternalWhenNotOwnComplaint()
        {
            // Arrange
            var complaint = new Complaint { ComplaintId = 20, RaisedByEmployeeId = 5 };
            _complaintRepoMock.Setup(r => r.GetDetailByIdAsync(20)).ReturnsAsync(complaint);
            _currentUserMock.Setup(u => u.RoleId).Returns(ROLE_GRO);
            _currentUserMock.Setup(u => u.EmployeeId).Returns(3);
            var comments = new List<ComplaintComment>
            {
                new ComplaintComment { IsInternal = true },
                new ComplaintComment { IsInternal = false }
            };
            _commentRepoMock.Setup(r => r.GetByComplaintIdAsync(20)).ReturnsAsync(comments);

            var result = await _service.GetByComplaintIdAsync(20);
            Assert.That(result.Count(), Is.EqualTo(2)); // internal visible
        }

        [Test]
        public async Task GetByComplaintIdAsync_EmployeeSeesOnlyExternal()
        {
            // Arrange
            var complaint = new Complaint { ComplaintId = 30, RaisedByEmployeeId = 7 };
            _complaintRepoMock.Setup(r => r.GetDetailByIdAsync(30)).ReturnsAsync(complaint);
            _currentUserMock.Setup(u => u.RoleId).Returns(ROLE_EMPLOYEE);
            _currentUserMock.Setup(u => u.EmployeeId).Returns(9);
            var comments = new List<ComplaintComment>
            {
                new ComplaintComment { IsInternal = true },
                new ComplaintComment { IsInternal = false }
            };
            _commentRepoMock.Setup(r => r.GetByComplaintIdAsync(30)).ReturnsAsync(comments);

            var result = await _service.GetByComplaintIdAsync(30);
            Assert.That(result.Count(), Is.EqualTo(1));
            Assert.That(result.First().IsInternal, Is.False);
        }

        [Test]
        public void AddCommentAsync_ThrowsWhenComplaintClosed()
        {
            // Arrange
            var complaint = new Complaint { ComplaintId = 40, StatusId = 6 }; // STATUS_CLOSED = 6
            _complaintRepoMock.Setup(r => r.GetDetailByIdAsync(40)).ReturnsAsync(complaint);
            var dto = new CreateComplaintCommentDto { CommentText = "test", IsInternal = false };
            _currentUserMock.Setup(u => u.RoleId).Returns(ROLE_EMPLOYEE);

            // Act & Assert
            var ex = Assert.ThrowsAsync<BusinessRuleException>(async () => await _service.AddCommentAsync(40, dto));
            Assert.That(ex.Message, Does.Contain("Cannot comment on complaints in status"));
        }

        [Test]
        public void AddCommentAsync_EmployeeCannotAddInternalComment()
        {
            // Arrange
            var complaint = new Complaint { ComplaintId = 50, StatusId = 1 };
            _complaintRepoMock.Setup(r => r.GetDetailByIdAsync(50)).ReturnsAsync(complaint);
            var dto = new CreateComplaintCommentDto { CommentText = "secret", IsInternal = true };
            _currentUserMock.Setup(u => u.RoleId).Returns(ROLE_EMPLOYEE);

            var ex = Assert.ThrowsAsync<ForbiddenException>(async () => await _service.AddCommentAsync(50, dto));
            Assert.That(ex.Message, Does.Contain("Employees cannot add internal comments"));
        }

        [Test]
        public void AddCommentAsync_CannotAddInternalOnOwnComplaint()
        {
            // Arrange
            var complaint = new Complaint { ComplaintId = 60, RaisedByEmployeeId = 99, StatusId = 1 };
            _complaintRepoMock.Setup(r => r.GetDetailByIdAsync(60)).ReturnsAsync(complaint);
            var dto = new CreateComplaintCommentDto { CommentText = "internal", IsInternal = true };
            _currentUserMock.Setup(u => u.RoleId).Returns(ROLE_GRO);
            _currentUserMock.Setup(u => u.EmployeeId).Returns(99); // same as raiser

            var ex = Assert.ThrowsAsync<ForbiddenException>(async () => await _service.AddCommentAsync(60, dto));
            Assert.That(ex.Message, Does.Contain("Cannot add internal comments on your own complaint"));
        }

        [Test]
        public async Task AddCommentAsync_SuccessfulCreationReturnsDto()
        {
            // Arrange
            var complaint = new Complaint { ComplaintId = 70, StatusId = 1 };
            _complaintRepoMock.Setup(r => r.GetDetailByIdAsync(70)).ReturnsAsync(complaint);
            var dto = new CreateComplaintCommentDto { CommentText = "hello", IsInternal = false };
            _currentUserMock.Setup(u => u.RoleId).Returns(ROLE_GRO);
            _currentUserMock.Setup(u => u.EmployeeId).Returns(5);
            var createdComment = new ComplaintComment
            {
                CommentId = 123,
                ComplaintId = 70,
                CommentText = "hello",
                CommentedBy = 5,
                IsInternal = false,
                CreatedAt = DateTime.UtcNow
            };
            _commentRepoMock.Setup(r => r.Create(It.IsAny<ComplaintComment>())).ReturnsAsync(createdComment);

            var result = await _service.AddCommentAsync(70, dto);
            Assert.That(result.CommentId, Is.EqualTo(123));
            Assert.That(result.CommentText, Is.EqualTo("hello"));
            Assert.That(result.IsInternal, Is.False);
        }
    }
}
