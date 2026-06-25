using Brittany_Salon_Backend.Application.DTOs.Product;

namespace Brittany_Salon_Backend.Application.Services.Interfaces
{
    public interface IProductService
    {
        Task<ProductReadDto> CreateAsync(ProductCreateDto dto);
        Task<List<ProductReadDto>> GetAllAsync(bool? onlyActive = false);
        Task<ProductReadDto?> GetByIdAsync(int id);
        Task<List<ProductReadDto>> SearchByNameAsync(string name, bool? onlyActive = null);
        Task<ProductReadDto?> UpdateAsync(int id, ProductUpdateDto dto);
        Task<bool> DeactivateAsync(int id);
        Task<bool> ReactivateAsync(int id);


    }
}
