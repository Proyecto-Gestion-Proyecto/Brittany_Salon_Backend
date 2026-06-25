using Brittany_Salon_Backend.Application.DTOs.Category;

namespace Brittany_Salon_Backend.Application.Services.Interfaces
{
    public interface ICategoryService
    {
        Task<CategoryReadDto> CreateAsync(CategoryCreateDto dto);
        Task<List<CategoryReadDto>> GetAllAsync(bool onlyActive = true);
        Task<CategoryReadDto?> GetByIdAsync(int id);
        Task<CategoryReadDto?> UpdateAsync(int id, CategoryUpdateDto dto);
        Task<bool> DeactivateAsync(int id);
        Task<bool> ReactivateAsync(int id);


    }
}
