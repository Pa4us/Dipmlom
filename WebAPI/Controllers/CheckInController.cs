using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedModel.DTOs;

namespace WebAPI.Controllers;

[Authorize]
public class CheckInController : BaseApiController
{
    private readonly ICheckInService _checkInService;

    public CheckInController(ICheckInService checkInService)
        => _checkInService = checkInService;

    [HttpPost("requests")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Create([FromBody] CreateCheckInRequestDto dto)
    {
        var response = await _checkInService.CreateRequestAsync(dto);
        return HandleResponse(response);
    }

    [HttpGet("requests")]
    [Authorize(Roles = "Manager,Educator")]
    public async Task<IActionResult> GetAll()
    {
        var response = await _checkInService.GetAllRequestsAsync();
        return HandleResponse(response);
    }

    [HttpGet("pending-items")]
    [Authorize(Roles = "Educator")]
    public async Task<IActionResult> GetPendingItems()
    {
        var response = await _checkInService.GetPendingItemsAsync();
        return HandleResponse(response);
    }

    [HttpPost("requests/{id:int}/process")]
    [Authorize(Roles = "Educator")]
    public async Task<IActionResult> ProcessRequest(int id, [FromBody] ProcessRequestBody body)
    {
        var response = await _checkInService.ProcessRequestAsync(id, body.EducatorId);
        return HandleResponse(response);
    }

    [HttpPost("items/process")]
    [Authorize(Roles = "Educator")]
    public async Task<IActionResult> ProcessItems([FromBody] ProcessItemsBody body)
    {
        var response = await _checkInService.ProcessItemsAsync(body.ItemIds, body.EducatorId);
        return HandleResponse(response);
    }

    /// Возвращает всех студентов, заселённых за последние N дней (по умолчанию 4).
    /// Используется для сводного PDF с паролями.
    [HttpGet("recently-checked-in")]
    [Authorize(Roles = "Educator")]
    public async Task<IActionResult> GetRecentlyCheckedIn([FromQuery] int days = 4)
    {
        var response = await _checkInService.GetRecentlyCheckedInAsync(days);
        return HandleResponse(response);
    }

    [HttpPost("items/reject")]
    [Authorize(Roles = "Educator")]
    public async Task<IActionResult> RejectItems([FromBody] RejectItemsBody body)
    {
        var response = await _checkInService.RejectItemsAsync(body.ItemIds);
        return HandleResponse(response);
    }

    public class ProcessRequestBody
    {
        public int EducatorId { get; set; }
    }

    public class ProcessItemsBody
    {
        public int        EducatorId { get; set; }
        public List<int>  ItemIds    { get; set; } = new();
    }

    public class RejectItemsBody
    {
        public List<int> ItemIds { get; set; } = new();
    }
}
