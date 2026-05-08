using SharedModel.DTOs;
using SharedModel.DTOs.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Interfaces
{
    public interface IStudentPointService: IBaseService<StudentPointDto, CreateStudentPointDto, UpdateStudentPointDto>
    {
        Task<ApiResponse<StudentRatingDto>> GetStudentRatingAsync(int userId);
        Task<ApiResponse<IEnumerable<StudentRatingDto>>> GetAllRatingsAsync();
        Task<ApiResponse<IEnumerable<StudentRatingDto>>> GetTopStudentsAsync(int count);
        Task<ApiResponse<StudentPointDto>> AwardPointsAsync(int userId, int points, string reason, string sourceType, int? sourceId = null);
        Task<ApiResponse<StudentPointDto>> DeductPointsAsync(int userId, int points, string reason, string sourceType, int? sourceId = null);
        Task<ApiResponse<int>> GetTotalPointsByUserAsync(int userId);
        Task<ApiResponse<IEnumerable<StudentPointDto>>> GetPointsByUserAsync(int userId);
    }
}
