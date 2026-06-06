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
// using Microsoft.OpenApi;
// using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddScoped<IComplaintRepository, ComplaintRepository>();
builder.Services.AddScoped<IComplaintHistoryRepository, ComplaintHistoryRepository>();
builder.Services.AddScoped<IComplaintAssignmentRepository, ComplaintAssignmentRepository>();
builder.Services.AddScoped<IComplaintCommentRepository, ComplaintCommentRepository>();
builder.Services.AddScoped<IComplaintAttachmentRepository, ComplaintAttachmentRepository>();
builder.Services.AddScoped<IComplaintRequestRepository, ComplaintRequestRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IEscalationRuleRepository, EscalationRuleRepository>();
builder.Services.AddScoped<ILookupRepository, LookupRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IRoleRequestRepository, RoleRequestRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IRepository<int, Department>, AbstractRepository<int, Department>>();
builder.Services.AddScoped<IRepository<int,ComplaintEscalation>,AbstractRepository<int,ComplaintEscalation>>();
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
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
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

builder.Services.AddControllers();
// builder.Services.AddOpenApi();
// builder.Services.AddSwaggerGen(c =>
// {
//     c.SwaggerDoc("v1", new OpenApiInfo { Title = "CGR API", Version = "v1" });
//     c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
//     {
//         Description = "Please Enter token",
//         Name        = "Authorization",
//         In          = ParameterLocation.Header,
//         Type        = SecuritySchemeType.ApiKey,
//         Scheme      = "Bearer"
//     });
//     c.AddSecurityRequirement(new OpenApiSecurityRequirement
// {
//     {
//         new OpenApiSecurityScheme
//         {
//             Scheme = "Bearer",
//             Name = "Authorization",
//             In = ParameterLocation.Header,
//             Reference = new OpenApiReference
//             {
//                 Type = ReferenceType.SecurityScheme,
//                 Id = "Bearer"
//             }
//         },
//         new string[]{}
//     }
// });
// });
builder.Services.AddSwaggerGen();
var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHangfireDashboard();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
RecurringJob.AddOrUpdate<ISlaEscalationJob>(
    "sla-escalation-job",
    job => job.ProcessEscalationsAsync(),

    "*/5 * * * *");
app.MapControllers();

app.Run();
