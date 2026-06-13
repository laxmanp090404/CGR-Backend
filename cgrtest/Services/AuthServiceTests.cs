using System;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Microsoft.Extensions.Logging;
using cgrbussinesslogic.Services;
using cgrbussinesslogic.Interfaces;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.DTOs.Auth;
using cgrmodellibrary.Models;
using cgrmodellibrary.Exceptions;

namespace cgrtest.Services
{
    [TestFixture]
    public class AuthServiceTests
    {
        private Mock<IEmployeeRepository> _employeeRepoMock;
        private Mock<ITokenService> _tokenServiceMock;
        private Mock<IRoleRequestRepository> _roleRequestRepoMock;
        private Mock<IDepartmentRepository> _deptRepoMock;
        private Mock<ILogger<AuthService>> _loggerMock;
        private AuthService _service;

        [SetUp]
        public void SetUp()
        {
            _employeeRepoMock = new Mock<IEmployeeRepository>();
            _tokenServiceMock = new Mock<ITokenService>();
            _roleRequestRepoMock = new Mock<IRoleRequestRepository>();
            _deptRepoMock = new Mock<IDepartmentRepository>();
            _loggerMock = new Mock<ILogger<AuthService>>();
            _service = new AuthService(
                _employeeRepoMock.Object,
                _tokenServiceMock.Object,
                _roleRequestRepoMock.Object,
                _deptRepoMock.Object,
                _loggerMock.Object);
        }

        [Test]
        public async Task LoginAsync_Success_ReturnsToken()
        {
            var employee = new Employee
            {
                EmployeeId = 1,
                EmployeeName = "John Doe",
                Email = "john@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("pwd123"),
                IsActive = true,
                Role = new Role { RoleName = "Employee" }
            };
            _employeeRepoMock.Setup(r => r.GetByEmailAsync(employee.Email)).ReturnsAsync(employee);
            _tokenServiceMock.Setup(t => t.GenerateToken(employee)).Returns("jwt-token");

            var dto = new LoginRequestDto { Email = employee.Email, Password = "pwd123" };
            var result = await _service.LoginAsync(dto);

            Assert.That(result.Token, Is.EqualTo("jwt-token"));
            Assert.That(result.EmployeeName, Is.EqualTo(employee.EmployeeName));
            Assert.That(result.Role, Is.EqualTo("Employee"));
            Assert.That(result.EmployeeId, Is.EqualTo(employee.EmployeeId));
        }

        [Test]
        public void LoginAsync_NullEmployee_ThrowsValidationException()
        {
            _employeeRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((Employee?)null);
            var dto = new LoginRequestDto { Email = "missing@example.com", Password = "any" };
            var ex = Assert.ThrowsAsync<ValidationException>(async () => await _service.LoginAsync(dto));
            Assert.That(ex.Message, Is.EqualTo("Account may not exist."));
        }

        [Test]
        public void LoginAsync_InactiveEmployee_ThrowsBusinessRuleException()
        {
            var emp = new Employee { IsActive = false };
            _employeeRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(emp);
            var dto = new LoginRequestDto { Email = "inactive@example.com", Password = "any" };
            var ex = Assert.ThrowsAsync<BusinessRuleException>(async () => await _service.LoginAsync(dto));
            Assert.That(ex.Message, Is.EqualTo("Account is deactivated. Please contact an administrator."));
        }

        [Test]
        public void LoginAsync_WrongPassword_ThrowsValidationException()
        {
            var emp = new Employee
            {
                IsActive = true,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct")
            };
            _employeeRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(emp);
            var dto = new LoginRequestDto { Email = "user@example.com", Password = "wrong" };
            var ex = Assert.ThrowsAsync<ValidationException>(async () => await _service.LoginAsync(dto));
            Assert.That(ex.Message, Is.EqualTo("Invalid email or password."));
        }

        [Test]
        public async Task RegisterAsync_WithGroAndDept_CreatesRoleRequest()
        {
            var dto = new RegisterRequestDto
            {
                EmployeeName = "Ada",
                Email = "ada@example.com",
                Password = "pwd",
                MobileNumber = "123",
                DepartmentId = 5,
                RequestGroRole = true
            };
            // email not existing
            _employeeRepoMock.Setup(r => r.ExistsByEmailAsync(dto.Email, null)).ReturnsAsync(false);
            // department exists check (no return needed, just ensure called)
            _deptRepoMock.Setup(r => r.Get(dto.DepartmentId.Value)).ReturnsAsync(new Department());
            // employee creation returns with Id
            var createdEmp = new Employee { EmployeeId = 10, Email = dto.Email };
            _employeeRepoMock.Setup(r => r.Create(It.IsAny<Employee>())).ReturnsAsync(createdEmp);
            // after creation, GetByEmailAsync returns employee with role placeholder
            var reloaded = new Employee { EmployeeId = 10, EmployeeName = "Ada", Email = dto.Email, Role = new Role { RoleName = "Employee" } };
            _employeeRepoMock.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync(reloaded);
            _tokenServiceMock.Setup(t => t.GenerateToken(reloaded)).Returns("token");

            var result = await _service.RegisterAsync(dto);

            Assert.That(result.Token, Is.EqualTo("token"));
            Assert.That(result.EmployeeName, Is.EqualTo("Ada"));
            Assert.That(result.Role, Is.EqualTo("Employee"));
            Assert.That(result.EmployeeId, Is.EqualTo(createdEmp.EmployeeId));
            _roleRequestRepoMock.Verify(r => r.Create(It.Is<RoleRequest>(rr => rr.EmployeeId == createdEmp.EmployeeId && rr.RequestedRoleId == 2)), Times.Once);
        }

        [Test]
        public void RegisterAsync_GroWithoutDept_ThrowsValidationException()
        {
            var dto = new RegisterRequestDto
            {
                EmployeeName = "Bob",
                Email = "bob@example.com",
                Password = "pwd",
                MobileNumber = "123",
                DepartmentId = null,
                RequestGroRole = true
            };
            _employeeRepoMock.Setup(r => r.ExistsByEmailAsync(dto.Email, null)).ReturnsAsync(false);
            var ex = Assert.ThrowsAsync<ValidationException>(async () => await _service.RegisterAsync(dto));
            Assert.That(ex.Message, Is.EqualTo("Department is required when requesting GRO role."));
        }
    }
}
