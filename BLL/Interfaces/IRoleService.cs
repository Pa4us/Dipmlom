using DAL.Entities;
using SharedModel.DTOs;
using SharedModel.DTOs.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Interfaces
{
    public interface IRoleService: IBaseService<RoleDto, CreateRoleDto, UpdateRoleDto>
    {
        Task<ApiResponse<RoleDto>> GetByNameAsync(string name);
    }
}
