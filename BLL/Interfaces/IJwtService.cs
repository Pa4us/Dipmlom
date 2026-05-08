using SharedModel.DTOs;

namespace BLL.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(UserDto user);
    }
}
