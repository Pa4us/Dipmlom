using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedModel.DTOs;

namespace WebAPI.Controllers
{
    [Authorize]
    public class UsersController : BaseApiController
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>Получить всех пользователей</summary>
        [HttpGet]
        [Authorize(Roles = "Educator,Inspector,Manager")]
        public async Task<IActionResult> GetAll()
        {
            var response = await _userService.GetAllAsync();
            return HandleResponse(response);
        }

        /// <summary>Получить пользователя по ID</summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var response = await _userService.GetByIdAsync(id);
            return HandleResponseNotFound(response);
        }

        /// <summary>Получить пользователей по роли</summary>
        [HttpGet("by-role/{roleId:int}")]
        [Authorize(Roles = "Educator,Inspector,Manager")]
        public async Task<IActionResult> GetByRole(int roleId)
        {
            var response = await _userService.GetUsersByRoleAsync(roleId);
            return HandleResponse(response);
        }

        /// <summary>Получить жильцов блока</summary>
        [HttpGet("by-block/{blockId:int}")]
        [Authorize(Roles = "Educator,Inspector,Manager")]
        public async Task<IActionResult> GetByBlock(int blockId)
        {
            var response = await _userService.GetResidentsByBlockAsync(blockId);
            return HandleResponse(response);
        }

        /// <summary>Создать пользователя</summary>
        [HttpPost]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
        {
            var response = await _userService.CreateAsync(dto);
            return HandleResponse(response);
        }

        /// <summary>Обновить пользователя</summary>
        [HttpPut]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Update([FromBody] UpdateUserDto dto)
        {
            var response = await _userService.UpdateAsync(dto);
            return HandleResponse(response);
        }

        /// <summary>Сбросить пароль пользователя (Заведующая)</summary>
        [HttpPost("{id:int}/reset-password")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> AdminResetPassword(int id, [FromBody] AdminResetPasswordDto dto)
        {
            var response = await _userService.AdminResetPasswordAsync(id, dto.NewPassword);
            return HandleResponse(response);
        }

        /// <summary>Удалить пользователя</summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            var response = await _userService.DeleteAsync(id);
            return HandleResponse(response);
        }
    }
}
