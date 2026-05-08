using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedModel.DTOs;

namespace WebAPI.Controllers
{
    [Authorize(Roles = "Educator,Manager")]
    public class RolesController : BaseApiController
    {
        private readonly IRoleService _roleService;

        public RolesController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        /// <summary>Получить все роли</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var response = await _roleService.GetAllAsync();
            return HandleResponse(response);
        }

        /// <summary>Получить роль по ID</summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var response = await _roleService.GetByIdAsync(id);
            return HandleResponseNotFound(response);
        }

        /// <summary>Получить роль по названию</summary>
        [HttpGet("by-name/{name}")]
        public async Task<IActionResult> GetByName(string name)
        {
            var response = await _roleService.GetByNameAsync(name);
            return HandleResponseNotFound(response);
        }

        /// <summary>Создать роль</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateRoleDto dto)
        {
            var response = await _roleService.CreateAsync(dto);
            return HandleResponse(response);
        }

        /// <summary>Обновить роль</summary>
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateRoleDto dto)
        {
            var response = await _roleService.UpdateAsync(dto);
            return HandleResponse(response);
        }

        /// <summary>Удалить роль</summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var response = await _roleService.DeleteAsync(id);
            return HandleResponse(response);
        }
    }
}
