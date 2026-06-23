using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using cgrbussinesslogic.Interfaces;
using cgrmodellibrary.Models;
using cgrbussinesslogic.Services;
using cgrdataaccesslibrary.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace cgrtest.Services
{
    [TestFixture]
    public class SlaEscalationJobTests
    {
        private Mock<IComplaintRepository> _complaintRepoMock;
        private Mock<IComplaintAssignmentEngine> _assignmentEngineMock;
        private Mock<IComplaintAssignmentRepository> _assignmentRepoMock;
        private Mock<IComplaintHistoryRepository> _historyRepoMock;
        private Mock<IRepository<int, ComplaintEscalation>> _escalationRepoMock;
        private Mock<IEscalationRuleRepository> _escalationRuleRepoMock;
        private Mock<INotificationService> _notificationServiceMock;
        private Mock<ILogger<SlaEscalationJob>> _loggerMock;
        private SlaEscalationJob _job;

        [SetUp]
        public void SetUp()
        {
            _complaintRepoMock = new Mock<IComplaintRepository>();
            _assignmentEngineMock = new Mock<IComplaintAssignmentEngine>();
            _assignmentRepoMock = new Mock<IComplaintAssignmentRepository>();
            _historyRepoMock = new Mock<IComplaintHistoryRepository>();
            _escalationRepoMock = new Mock<IRepository<int, ComplaintEscalation>>();
            _escalationRuleRepoMock = new Mock<IEscalationRuleRepository>();
            _notificationServiceMock = new Mock<INotificationService>();
            _loggerMock = new Mock<ILogger<SlaEscalationJob>>();

            _job = new SlaEscalationJob(
                _complaintRepoMock.Object,
                _assignmentEngineMock.Object,
                _assignmentRepoMock.Object,
                _historyRepoMock.Object,
                _escalationRepoMock.Object,
                _escalationRuleRepoMock.Object,
                _notificationServiceMock.Object,
                _loggerMock.Object,
                null);
        }

        [Test]
        public async Task ProcessEscalationsAsync_OnlyProcessesDueComplaints()
        {
            var now = DateTime.UtcNow;
            var due = new Complaint { ComplaintId = 1, EscalationDueAt = now.AddMinutes(-5) };
            var future = new Complaint { ComplaintId = 2, EscalationDueAt = now.AddHours(1) };
            var nullDue = new Complaint { ComplaintId = 3, EscalationDueAt = null };

            _complaintRepoMock.Setup(r => r.GetEscalationDueComplaintsAsync())
                .ReturnsAsync(new List<Complaint> { due, future, nullDue });

            // Spy on the private ProcessComplaintAsync via reflection
            var procMethod = typeof(SlaEscalationJob).GetMethod("ProcessComplaintAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            // var callCount = 0;
            procMethod!.Invoke(_job, new object[] { due }); // will be called by the job
            // Replace the method body is not feasible; instead we verify that the repository's Update is called only for due complaint.
            _assignmentEngineMock.Setup(e => e.DetermineEscalationAssignmentAsync(It.IsAny<Complaint>()))
                .ReturnsAsync((10, (short)1));
            _assignmentEngineMock.Setup(e => e.CalculateEscalationDueAtAsync(It.IsAny<Complaint>()))
                .ReturnsAsync(now.AddHours(2));

            await _job.ProcessEscalationsAsync();

            // Verify that Update was called once (for the due complaint) and not for others.
            _complaintRepoMock.Verify(r => r.Update(It.IsAny<Complaint>(), It.IsAny<int>()), Times.Once);
        }

        [Test]
        public async Task ProcessComplaintAsync_NormalEscalation_PerformsAllSteps()
        {
            var complaint = new Complaint { ComplaintId = 10, EscalationLevel = 0, StatusId = 1 };
            var assignment = (HandlerId: 5, EscalationLevel: (short)1);

            _assignmentEngineMock.Setup(e => e.DetermineEscalationAssignmentAsync(It.IsAny<Complaint>()))
                .ReturnsAsync(assignment);
            _assignmentEngineMock.Setup(e => e.CalculateEscalationDueAtAsync(It.IsAny<Complaint>()))
                .ReturnsAsync(DateTime.UtcNow.AddHours(2));

            var method = typeof(SlaEscalationJob).GetMethod("ProcessComplaintAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            var task = (Task)method!.Invoke(_job, new object[] { complaint })!;
            await task;

            _complaintRepoMock.Verify(r => r.Update(It.IsAny<Complaint>(), complaint.ComplaintId), Times.Once);
            _assignmentRepoMock.Verify(r => r.Create(It.IsAny<ComplaintAssignmentHistory>()), Times.Once);
            _historyRepoMock.Verify(r => r.Create(It.IsAny<ComplaintHistory>()), Times.Once);
            _escalationRepoMock.Verify(r => r.Create(It.IsAny<ComplaintEscalation>()), Times.Once);
            _notificationServiceMock.Verify(n => n.SendAsync(assignment.HandlerId, It.IsAny<short>(), It.IsAny<string>(), It.IsAny<string>(), complaint.ComplaintId), Times.Once);
        }

        [Test]
        public async Task ProcessComplaintAsync_ExternalEscalation_TriggersExternalPath()
        {
            var complaint = new Complaint { ComplaintId = 20, EscalationLevel = 3, StatusId = 1, RaisedByEmployeeId = 2 };
            var assignment = (HandlerId: 0, EscalationLevel: (short)3);

            _assignmentEngineMock.Setup(e => e.DetermineEscalationAssignmentAsync(It.IsAny<Complaint>()))
                .ReturnsAsync(assignment);

            var method = typeof(SlaEscalationJob).GetMethod("ProcessComplaintAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            var task = (Task)method!.Invoke(_job, new object[] { complaint })!;
            await task;

            _complaintRepoMock.Verify(r => r.Update(It.IsAny<Complaint>(), complaint.ComplaintId), Times.Once);
            _historyRepoMock.Verify(r => r.Create(It.IsAny<ComplaintHistory>()), Times.Once);
            _notificationServiceMock.Verify(n => n.SendAsync(complaint.RaisedByEmployeeId, It.IsAny<short>(), It.IsAny<string>(), It.IsAny<string>(), complaint.ComplaintId), Times.Once);
#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
            _loggerMock.Verify(l => l.Log(It.Is<LogLevel>(lvl => lvl == LogLevel.Error), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), null, (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.AtLeastOnce);
#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
        }

        [Test]
        public async Task CreateEscalationRecordAsync_NoRule_UsesNullRuleId()
        {
            var complaint = new Complaint { ComplaintId = 30, CategoryId = 1, PriorityId = 2, EscalationLevel = 1 };
            _escalationRuleRepoMock.Setup(r => r.GetRuleAsync(complaint.CategoryId, complaint.PriorityId, complaint.EscalationLevel))
                .ReturnsAsync((EscalationRule?)null);

            var method = typeof(SlaEscalationJob).GetMethod("CreateEscalationRecordAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            var task = (Task)method!.Invoke(_job, new object[] { complaint, 99, (short)1 })!;
            await task;

            _escalationRepoMock.Verify(r => r.Create(It.Is<ComplaintEscalation>(e => e.EscalationRuleId == null)), Times.Once);
        }
    }
}
