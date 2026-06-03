using cgrbussinesslogic.Interfaces;
using cgrmodellibrary.DTOs.Common;
using Microsoft.AspNetCore.Mvc;

namespace cgrwebapi.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseController : ControllerBase
{
    protected static ActionResult<ApiResponseDto<T>> Ok<T>(T data, string? message = null) =>
        new OkObjectResult(new ApiResponseDto<T> { Success = true, Data = data, Message = message });

    protected static ActionResult<ApiResponseDto<T>> Created<T>(T data, string? message = null) =>
        new ObjectResult(new ApiResponseDto<T> { Success = true, Data = data, Message = message })
        { StatusCode = 201 };

    protected static ActionResult<ApiResponseDto<object>> NoContent(string message = "Done") =>
        new OkObjectResult(new ApiResponseDto<object> { Success = true, Message = message });
}
