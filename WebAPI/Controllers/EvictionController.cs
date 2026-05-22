using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedModel.DTOs;

namespace WebAPI.Controllers;

[Authorize]
public class EvictionController : BaseApiController
{
    private readonly IEvictionService _evictionService;

    public EvictionController(IEvictionService evictionService)
        => _evictionService = evictionService;

    [HttpPost("requests")]
    [Authorize(Roles = "Educator")]
    public async Task<IActionResult> Create([FromBody] CreateEvictionRequestDto dto)
    {
        var response = await _evictionService.CreateRequestsAsync(dto);
        return HandleResponse(response);
    }

    [HttpGet("pending")]
    [Authorize(Roles = "Manager,Educator")]
    public async Task<IActionResult> GetPending()
    {
        var response = await _evictionService.GetPendingAsync();
        return HandleResponse(response);
    }

    [HttpPost("confirm")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Confirm([FromBody] ConfirmEvictionDto dto)
    {
        var response = await _evictionService.ConfirmBatchAsync(dto);
        return HandleResponse(response);
    }
}
