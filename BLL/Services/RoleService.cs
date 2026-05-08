using AutoMapper;
using BLL.Interfaces;
using DAL.Entities;
using DAL.Repositories;
using SharedModel.DTOs;
using SharedModel.DTOs.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class RoleService : BaseService<Role, RoleDto, CreateRoleDto, UpdateRoleDto>, IRoleService
    {
        public RoleService(IRepository<Role> repository, IMapper mapper)
            : base(repository, mapper)
        {
        }

        public async Task<ApiResponse<RoleDto>> GetByNameAsync(string name)
        {
            var roles = await _repository.FindAsync(r => r.Name == name);
            var role = roles.FirstOrDefault();

            if (role == null)
                return ApiResponse<RoleDto>.Fail($"Роль с названием '{name}' не найдена");

            var dto = _mapper.Map<RoleDto>(role);
            return ApiResponse<RoleDto>.Ok(dto);
        }
    }
}
