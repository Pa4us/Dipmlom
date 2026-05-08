using Microsoft.AspNetCore.Mvc;
using SharedModel.DTOs.Common;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseApiController : ControllerBase
    {
        protected IActionResult HandleResponse<T>(ApiResponse<T> response)
        {
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        protected IActionResult HandleResponseNotFound<T>(ApiResponse<T> response)
        {
            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }
    }
}
