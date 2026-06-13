using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using cgrbussinesslogic.Services;
using cgrbussinesslogic.Interfaces;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.Models;
using cgrmodellibrary.Exceptions;

namespace cgrtest.Services
{
    [TestFixture]
    public class ComplaintAssignmentEngineTests
    {
        private Mock<IEmployeeRepository> _employeeRepoMock;
        private Mock<IComplaintAssignmentRepository> _assignmentRepoMock;
        private Mock<IEscalationRuleRepository> _escalationRepoMock;
        private ComplaintAssignmentEngine _engine;

        [SetUp]
        public void SetUp()
        {
            _employeeRepoMock = new Mock<IEmployeeRepository>();
            _assignmentRepoMock = new Mock<IComplaintAssignmentRepository>();
            _escalationRepoMock = new Mock<IEscalationRuleRepository>();
            _engine = new ComplaintAssignmentEngine(_employeeRepoMock.Object, _assignmentRepoMock.Object, _escalationRepoMock.Object);
        }

        #region DetermineInitialAssignmentAsync Tests
        [Test]
        public async Task DetermineInitialAssignmentAsync_EmployeeRole_DelegatesToAssign()
        {
            // Arrange
            int complaintId = 1;
            int deptId = 10;
            short creatorRole = 1; // ROLE_EMPLOYEE
            _assignmentRepoMock.Setup(a => a.GetPreviousHandlersAsync(complaintId)).ReturnsAsync(new List<int>());
            var gro = new VGroActiveWorkload { EmployeeId = 2 };
            _employeeRepoMock.Setup(e => e.GetGroActiveWorkloadAsync(deptId)).ReturnsAsync(new List<VGroActiveWorkload> { gro });

            // Act
            var result = await _engine.DetermineInitialAssignmentAsync(complaintId, deptId, creatorRole);

            // Assert
            Assert.That(result.HandlerId, Is.EqualTo(gro.EmployeeId));
            Assert.That(result.EscalationLevel, Is.EqualTo(0));
        }

        [Test]
        public async Task DetermineInitialAssignmentAsync_GroRole_ReturnsDeptHeadOrAdmin()
        {
            // Arrange
            int complaintId = 1;
            int deptId = 20;
            short creatorRole = 2; // ROLE_GRO
            var deptHead = new Employee { EmployeeId = 3 };
            _employeeRepoMock.Setup(e => e.GetDepartmentHeadAsync(deptId)).ReturnsAsync(deptHead);

            // Act
            var result = await _engine.DetermineInitialAssignmentAsync(complaintId, deptId, creatorRole);

            // Assert
            Assert.That(result.HandlerId, Is.EqualTo(deptHead.EmployeeId));
            Assert.That(result.EscalationLevel, Is.EqualTo(1));
        }

        [Test]
        public async Task DetermineInitialAssignmentAsync_GroRole_NoDeptHead_ReturnsAdmin()
        {
            // Arrange
            int complaintId = 1;
            int deptId = 30;
            short creatorRole = 2; // ROLE_GRO
            var admin = new Employee { EmployeeId = 99 };
            _employeeRepoMock.Setup(e => e.GetDepartmentHeadAsync(deptId)).ReturnsAsync((Employee?)null);
            _employeeRepoMock.Setup(e => e.GetAdminAsync()).ReturnsAsync(admin);

            // Act
            var result = await _engine.DetermineInitialAssignmentAsync(complaintId, deptId, creatorRole);

            // Assert
            Assert.That(result.HandlerId, Is.EqualTo(admin.EmployeeId));
            Assert.That(result.EscalationLevel, Is.EqualTo(2));
        }

        [Test]
        public async Task DetermineInitialAssignmentAsync_DepartmentHeadRole_ReturnsAdmin()
        {
            // Arrange
            int complaintId = 1;
            int deptId = 40;
            short creatorRole = 3; // ROLE_DEPARTMENT_HEAD
            var admin = new Employee { EmployeeId = 101 };
            _employeeRepoMock.Setup(e => e.GetAdminAsync()).ReturnsAsync(admin);

            // Act
            var result = await _engine.DetermineInitialAssignmentAsync(complaintId, deptId, creatorRole);

            // Assert
            Assert.That(result.HandlerId, Is.EqualTo(admin.EmployeeId));
            Assert.That(result.EscalationLevel, Is.EqualTo(2));
        }

        [Test]
        public void DetermineInitialAssignmentAsync_InvalidRole_Throws()
        {
            // Arrange
            int complaintId = 1;
            int deptId = 0;
            short creatorRole = 99; // invalid

            // Act & Assert
            var ex = Assert.ThrowsAsync<BusinessRuleException>(async () => await _engine.DetermineInitialAssignmentAsync(complaintId, deptId, creatorRole));
            Assert.That(ex.Message, Does.Contain("Invalid complaint creator role"));
        }
        #endregion

        #region AssignComplaintAsync (private) via reflection
        private async Task<(int HandlerId, short EscalationLevel)> CallAssignComplaintAsync(int complaintId, int departmentId)
        {
            var method = typeof(ComplaintAssignmentEngine).GetMethod("AssignComplaintAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var task = (Task)method!.Invoke(_engine, new object[] { complaintId, departmentId });
            await task.ConfigureAwait(false);
            var resultProp = task.GetType().GetProperty("Result");
            return ((int HandlerId, short EscalationLevel))resultProp!.GetValue(task)!;
        }

        [Test]
        public async Task AssignComplaintAsync_WithAvailableGRO_ReturnsGRO()
        {
            int complaintId = 5;
            int deptId = 11;
            var gro = new VGroActiveWorkload { EmployeeId = 7 };
            _assignmentRepoMock.Setup(a => a.GetPreviousHandlersAsync(complaintId)).ReturnsAsync(new List<int>());
            _employeeRepoMock.Setup(e => e.GetGroActiveWorkloadAsync(deptId)).ReturnsAsync(new List<VGroActiveWorkload> { gro });

            var result = await CallAssignComplaintAsync(complaintId, deptId);
            Assert.That(result.HandlerId, Is.EqualTo(gro.EmployeeId));
            Assert.That(result.EscalationLevel, Is.EqualTo(0));
        }

        [Test]
        public async Task AssignComplaintAsync_NoGRO_UsesDeptHead()
        {
            int complaintId = 6;
            int deptId = 12;
            var deptHead = new Employee { EmployeeId = 8 };
            _assignmentRepoMock.Setup(a => a.GetPreviousHandlersAsync(complaintId)).ReturnsAsync(new List<int>());
            _employeeRepoMock.Setup(e => e.GetGroActiveWorkloadAsync(deptId)).ReturnsAsync(new List<VGroActiveWorkload>());
            _employeeRepoMock.Setup(e => e.GetDepartmentHeadAsync(deptId)).ReturnsAsync(deptHead);

            var result = await CallAssignComplaintAsync(complaintId, deptId);
            Assert.That(result.HandlerId, Is.EqualTo(deptHead.EmployeeId));
            Assert.That(result.EscalationLevel, Is.EqualTo(1));
        }

        [Test]
        public async Task AssignComplaintAsync_NoGROOrDeptHead_UsesAdmin()
        {
            int complaintId = 7;
            int deptId = 13;
            var admin = new Employee { EmployeeId = 99 };
            _assignmentRepoMock.Setup(a => a.GetPreviousHandlersAsync(complaintId)).ReturnsAsync(new List<int>());
            _employeeRepoMock.Setup(e => e.GetGroActiveWorkloadAsync(deptId)).ReturnsAsync(new List<VGroActiveWorkload>());
            _employeeRepoMock.Setup(e => e.GetDepartmentHeadAsync(deptId)).ReturnsAsync((Employee?)null);
            _employeeRepoMock.Setup(e => e.GetAdminAsync()).ReturnsAsync(admin);

            var result = await CallAssignComplaintAsync(complaintId, deptId);
            Assert.That(result.HandlerId, Is.EqualTo(admin.EmployeeId));
            Assert.That(result.EscalationLevel, Is.EqualTo(2));
        }
        #endregion

        #region CalculateEscalationDueAtAsync Tests
        [Test]
        public async Task CalculateEscalationDueAtAsync_LevelZero_ReturnsNowPlusSla()
        {
            var complaint = new Complaint { EscalationLevel = 0, Category = new Category { SlaHours = 5 } };
            var due = await _engine.CalculateEscalationDueAtAsync(complaint);
            var expected = DateTime.UtcNow.AddHours(5);
            Assert.That(due.Value, Is.InRange(expected.AddSeconds(-5), expected.AddSeconds(5)));
        }

        [Test]
        public async Task CalculateEscalationDueAtAsync_WithRule_ReturnsRuleHours()
        {
            var complaint = new Complaint { EscalationLevel = 1, CategoryId = 1, PriorityId = 2 };
            var rule = new EscalationRule { EscalateAfterHours = 8 };
            _escalationRepoMock.Setup(r => r.GetRuleAsync(complaint.CategoryId, complaint.PriorityId, complaint.EscalationLevel)).ReturnsAsync(rule);
            var due = await _engine.CalculateEscalationDueAtAsync(complaint);
            var expected = DateTime.UtcNow.AddHours(8);
            Assert.That(due.Value, Is.InRange(expected.AddSeconds(-5), expected.AddSeconds(5)));
        }

        [Test]
        public void CalculateEscalationDueAtAsync_NoRule_Throws()
        {
            var complaint = new Complaint { EscalationLevel = 2, CategoryId = 5, PriorityId = 3 };
            _escalationRepoMock.Setup(r => r.GetRuleAsync(complaint.CategoryId, complaint.PriorityId, complaint.EscalationLevel)).ReturnsAsync((EscalationRule?)null);
            var ex = Assert.ThrowsAsync<BusinessRuleException>(async () => await _engine.CalculateEscalationDueAtAsync(complaint));
            Assert.That(ex.Message, Does.Contain("No escalation rule configured"));
        }
        #endregion
    }
}
