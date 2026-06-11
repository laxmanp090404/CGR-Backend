using System.Net;
using System.Text.Json;
using cgrmodellibrary.Exceptions;

namespace cgrwebapi.Middlewares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                switch (ex)
                {
                    case ValidationException:
                    case BusinessRuleException:
                    case ConflictException:
                    case NotFoundException:
                    case UnauthorizedAccessException:
                    case ForbiddenException:
                        _logger.LogWarning(ex.Message);
                        break;

                    default:
                        _logger.LogError(
                            ex,
                            $"Unhandled exception occurred {context.TraceIdentifier}");
                        break;
                }

                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            HttpStatusCode statusCode;

            switch (ex)
            {
                case NotFoundException:
                    statusCode = HttpStatusCode.NotFound;
                    break;

                case ForbiddenException:
                    statusCode = HttpStatusCode.Forbidden;
                    break;

                case ConflictException:
                    statusCode = HttpStatusCode.Conflict;
                    break;

                case ValidationException:
                    statusCode = HttpStatusCode.BadRequest;
                    break;

                case BusinessRuleException:
                    statusCode = HttpStatusCode.UnprocessableEntity;
                    break;

                case UnauthorizedAccessException:
                    statusCode = HttpStatusCode.Unauthorized;
                    break;

                default:
                    statusCode = HttpStatusCode.InternalServerError;
                    break;
            }
            var result = new
            {
                statusCode = (int)statusCode,
                error = ex.GetType().Name,
                message = ex.Message,
                traceId = context.TraceIdentifier
            };
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;
            return context.Response.WriteAsync(JsonSerializer.Serialize(result));
        }
    }
}