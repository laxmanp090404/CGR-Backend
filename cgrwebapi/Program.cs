using cgrbussinesslogic.Interfaces;
using cgrbussinesslogic.Services;
using cgrdataaccesslibrary.Context;
using cgrdataaccesslibrary.Interfaces;
using cgrdataaccesslibrary.Repositories;
using cgrmodellibrary.Models;
using cgrwebapi.Middlewares;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;
using System.Text;
using Serilog;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
//configure serilog and override default logging for custom filtering
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteTo.File(
        "logs/CGRApplog-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddDbContext<CGRContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHangfire(config =>
{
    config.UsePostgreSqlStorage(options =>
    {
        options.UseNpgsqlConnection(
            builder.Configuration.GetConnectionString("DefaultConnection"));
    });
});

builder.Services.AddHangfireServer();

#region Repos
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped(typeof(IRepository<,>),typeof(AbstractRepository<,>));
builder.Services.AddScoped<IComplaintRepository, ComplaintRepository>();
builder.Services.AddScoped<IComplaintHistoryRepository, ComplaintHistoryRepository>();
builder.Services.AddScoped<IComplaintAssignmentRepository, ComplaintAssignmentRepository>();
builder.Services.AddScoped<IComplaintCommentRepository, ComplaintCommentRepository>();
builder.Services.AddScoped<IComplaintAttachmentRepository, ComplaintAttachmentRepository>();
builder.Services.AddScoped<IComplaintRequestRepository, ComplaintRequestRepository>();
builder.Services.AddScoped<IEscalationRuleRepository, EscalationRuleRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IRoleRequestRepository, RoleRequestRepository>();
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IComplaintEscalationRepository, ComplaintEscalationRepository>();
// builder.Services.AddScoped<IRepository<int,ComplaintEscalation>,AbstractRepository<int,ComplaintEscalation>>();
// builder.Services.AddScoped<IRepository<short,Priority>,AbstractRepository<short,Priority>>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
// builder.Services.AddScoped<IRepository<int, EscalationRule>,AbstractRepository<int, EscalationRule>>();
#endregion
#region Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IEscalationRuleService, EscalationRuleService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IComplaintService, ComplaintService>();
builder.Services.AddScoped<IComplaintCommentService, ComplaintCommentService>();
builder.Services.AddScoped<IComplaintAttachmentService, ComplaintAttachmentService>();
builder.Services.AddScoped<IComplaintRequestService, ComplaintRequestService>();
builder.Services.AddScoped<IRoleRequestService, RoleRequestService>();
builder.Services.AddScoped<IComplaintAssignmentEngine,ComplaintAssignmentEngine>();
builder.Services.AddScoped<ISlaEscalationJob, SlaEscalationJob>();
builder.Services.AddScoped<IDepartmentRepository,DepartmentRepository>();
#endregion
#region JWT
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer              = jwtSettings["Issuer"],
        ValidAudience            = jwtSettings["Audience"],
        IssuerSigningKey         = new SymmetricSecurityKey(key)
    };
});
#endregion

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly",          policy => policy.RequireRole("ADMIN"));
    options.AddPolicy("GROOrAdmin",         policy => policy.RequireRole("GRO", "ADMIN"));
    options.AddPolicy("DeptHeadOrAdmin",    policy => policy.RequireRole("DEPARTMENT_HEAD", "ADMIN"));
    options.AddPolicy("NotEmployee",        policy => policy.RequireRole("GRO", "DEPARTMENT_HEAD", "ADMIN"));
    options.AddPolicy("Authenticated",      policy => policy.RequireAuthenticatedUser());
});

builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory =
            context =>
            {
                var errors =
                    context.ModelState
                        .Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage);

                return new BadRequestObjectResult(
                    new
                    {
                        statusCode = 400,
                        error = "ValidationException",
                        message = string.Join("; ", errors)
                    });
            };
    });
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    // rate limit for creating compplaints
    options.AddPolicy("ComplaintCreate",
    context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.FindFirst("employee_id")?.Value?? context.Connection.RemoteIpAddress?.ToString()?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    // policy for rate limiting complaint request
     options.AddPolicy("ComplaintRequestReopen",
        context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.User.FindFirst("employee_id")?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));

   
    // policy for login attempts 
    // user not logged in so use IP
    options.AddPolicy(
        "Login",
        context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
});
builder.Services.AddSwaggerGen();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = (httpContext, elapsed, ex) =>
    {
        if (httpContext.Request.Path.StartsWithSegments("/hangfire"))
        {
            return Serilog.Events.LogEventLevel.Debug;
        }

        return Serilog.Events.LogEventLevel.Information;
    };
});
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHangfireDashboard();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseRateLimiter();//after auth otherwise empid null
app.UseAuthorization();
RecurringJob.AddOrUpdate<ISlaEscalationJob>(
    "sla-escalation-job",
    job => job.ProcessEscalationsAsync(),

    "*/5 * * * *");
app.MapControllers();

app.Run();
