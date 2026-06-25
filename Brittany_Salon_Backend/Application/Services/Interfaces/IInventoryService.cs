using Brittany_Salon_Backend.Application.DTOs.Inventory;

namespace Brittany_Salon_Backend.Application.Services.Interfaces
{
    public interface IInventoryService
    {
        Task<List<InventoryReadDto>> GetAllActiveAsync();
        Task<List<InventoryReadDto>> GetAllAsync();
        Task<InventoryReadDto?> GetByIdAsync(int id);
        Task<InventoryReadDto?> GetByProductIdAsync(int productId);
        Task<List<InventoryReadDto>> GetLowStockAlertsAsync();
        Task<InventoryReadDto> CreateAsync(InventoryCreateDto dto);
        Task<bool> DiscountQuantityAsync(int productId, int quantity);
        Task<bool> DiscountMultipleAsync(Dictionary<int, int> products);
        Task<bool> DeleteAsync(int inventoryId);
        Task<bool> UpdateAsync(int inventoryId, InventoryUpdateDto dto);

        Task<bool> ReactivateAsync(int inventoryId);
    }
}
