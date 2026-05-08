using SharedModel.DTOs;
using SharedModel.DTOs.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Interfaces
{
    public interface IBaseService<TDto, TCreateDto, TUpdateDto>
        where TDto : BaseDto
        where TUpdateDto : class
    {
        Task<ApiResponse<IEnumerable<TDto>>> GetAllAsync();
        Task<ApiResponse<TDto>> GetByIdAsync(int id);
        Task<ApiResponse<TDto>> CreateAsync(TCreateDto createDto);
        Task<ApiResponse<TDto>> UpdateAsync(TUpdateDto updateDto);
        Task<ApiResponse<bool>> DeleteAsync(int id);
    }
}
