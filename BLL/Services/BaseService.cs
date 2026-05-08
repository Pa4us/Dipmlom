using AutoMapper;
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
    public class BaseService<TEntity, TDto, TCreateDto, TUpdateDto>
        where TEntity : class // Только класс
        where TDto : BaseDto // Должен наследоваться от BaseDto
        where TUpdateDto : class // Только класс
    {
        protected readonly IRepository<TEntity> _repository;
        protected readonly IMapper _mapper;

        protected BaseService(IRepository<TEntity> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public virtual async Task<ApiResponse<IEnumerable<TDto>>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();
            var dtos = _mapper.Map<IEnumerable<TDto>>(entities);
            return ApiResponse<IEnumerable<TDto>>.Ok(dtos);
        }

        public virtual async Task<ApiResponse<TDto>> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return ApiResponse<TDto>.Fail($"Запись с ID {id} не найдена");

            var dto = _mapper.Map<TDto>(entity);
            return ApiResponse<TDto>.Ok(dto);
        }

        public virtual async Task<ApiResponse<TDto>> CreateAsync(TCreateDto createDto)
        {
            try
            {
                var entity = _mapper.Map<TEntity>(createDto);
                await _repository.AddAsync(entity);
                await _repository.SaveChangesAsync();

                var dto = _mapper.Map<TDto>(entity);
                return ApiResponse<TDto>.Ok(dto, "Запись успешно создана");
            }
            catch (Exception ex)
            {
                return ApiResponse<TDto>.Fail($"Ошибка при создании: {ex.Message}");
            }
        }

        public virtual async Task<ApiResponse<TDto>> UpdateAsync(TUpdateDto updateDto)
        {
            try
            {
                var entity = _mapper.Map<TEntity>(updateDto);
                _repository.Update(entity);
                await _repository.SaveChangesAsync();

                var dto = _mapper.Map<TDto>(entity);
                return ApiResponse<TDto>.Ok(dto, "Запись успешно обновлена");
            }
            catch (Exception ex)
            {
                return ApiResponse<TDto>.Fail($"Ошибка при обновлении: {ex.Message}");
            }
        }

        public virtual async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return ApiResponse<bool>.Fail($"Запись с ID {id} не найдена");

                _repository.Delete(entity);
                await _repository.SaveChangesAsync();
                return ApiResponse<bool>.Ok(true, "Запись успешно удалена");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Fail($"Ошибка при удалении: {ex.Message}");
            }
        }
    }
}
