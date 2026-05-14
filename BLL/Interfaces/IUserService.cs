using SharedModel.DTOs;
using SharedModel.DTOs.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Interfaces
{
    public interface IUserService: IBaseService<UserDto, CreateUserDto, UpdateUserDto>
    {
        Task<ApiResponse<UserDto>> GetByUsernameAsync(string username);
        Task<ApiResponse<UserDto>> GetByEmailAsync(string email);
        Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginDto loginDto);
        Task<ApiResponse<IEnumerable<UserDto>>> GetUsersByRoleAsync(int roleId);
        Task<ApiResponse<IEnumerable<UserDto>>> GetUsersByRoleNameAsync(string roleName);
        Task<ApiResponse<IEnumerable<UserDto>>> GetResidentsByBlockAsync(int blockId);
        Task<ApiResponse<bool>> ChangePasswordAsync(int userId, string oldPassword, string newPassword);
        Task<ApiResponse<bool>> ResetPasswordAsync(string email, string newPassword);
        Task<ApiResponse<bool>> AdminResetPasswordAsync(int userId, string newPassword);
        Task<ApiResponse<ImportUsersResultDto>> ImportUsersAsync(ImportUsersRequestDto dto);
    }
}
