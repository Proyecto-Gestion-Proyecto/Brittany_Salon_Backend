using Brittany_Salon_Backend.Application.DTOs.Inventory;
using Brittany_Salon_Backend.Application.Exceptions;
using Brittany_Salon_Backend.Application.Services.Interfaces;
using Brittany_Salon_Backend.Domain.Entities;
using Brittany_Salon_Backend.Infrastructure.Logging;
using Brittany_Salon_Backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Brittany_Salon_Backend.Application.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly AppDbContext _db;
        private readonly IDevLogger _logger;

        public InventoryService(AppDbContext db, IDevLogger logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<InventoryReadDto>> GetAllActiveAsync()
        {
            _logger.LogInfo("Obteniendo todos los registros activos del inventario");

            var inventoryItems = await _db.Inventory
                .AsNoTracking()
                .Where(i => i.IsActive)
                .OrderBy(i => i.Location)
                .ThenBy(i => i.ProductId)
                .Select(i => MapToReadDto(i))
                .ToListAsync();

            _logger.LogInfo("Se encontraron {Count} registros activos del inventario", inventoryItems.Count);
            return inventoryItems;
        }

        public async Task<List<InventoryReadDto>> GetAllAsync()
        {
            _logger.LogInfo("Obteniendo todos los registros del inventario");

            var inventoryItems = await _db.Inventory
                .AsNoTracking()
                .OrderBy(i => i.Location)
                .ThenBy(i => i.ProductId)
                .Select(i => MapToReadDto(i))
                .ToListAsync();

            _logger.LogInfo("Se encontraron {Count} registros del inventario", inventoryItems.Count);
            return inventoryItems;
        }
        public async Task<InventoryReadDto?> GetByIdAsync(int id)
        {
            _logger.LogInfo("Obteniendo registro del inventario con ID: {Id}", id);

            var inventory = await _db.Inventory
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.InventoryId == id);

            if (inventory == null)
            {
                _logger.LogInfo("No se encontró registro del inventario con ID: {Id}", id);
                return null;
            }

            return MapToReadDto(inventory);
        }

        public async Task<InventoryReadDto?> GetByProductIdAsync(int productId)
        {
            _logger.LogInfo("Obteniendo inventario para producto con ID: {ProductId}", productId);

            var inventory = await _db.Inventory
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.ProductId == productId);

            if (inventory == null)
            {
                _logger.LogInfo("No se encontró inventario para producto con ID: {ProductId}", productId);
                return null;
            }

            return MapToReadDto(inventory);
        }

        public async Task<List<InventoryReadDto>> GetLowStockAlertsAsync()
        {
            _logger.LogInfo("Obteniendo registros de inventario con alerta de stock bajo");

            var lowStockItems = await _db.Inventory
                .AsNoTracking()
                .Where(i => i.IsActive && i.Quantity <= i.MinimumStock)
                .OrderBy(i => i.Quantity)
                .ThenBy(i => i.Location)
                .Select(i => MapToReadDto(i))
                .ToListAsync();

            _logger.LogInfo("Se encontraron {Count} registros con stock bajo", lowStockItems.Count);
            return lowStockItems;
        }

        public async Task<InventoryReadDto> CreateAsync(InventoryCreateDto dto)
        {
            _logger.LogInfo("Iniciando creación de registro de inventario para producto ID: {ProductId}", dto.ProductId);

            var product = await _db.Products
            .Where(p => p.ProductId == dto.ProductId)
            .Select(p => new { p.ProductId, p.ProductName })
            .FirstOrDefaultAsync();

            if (product == null)
            {
                throw new NotFoundException($"El producto con ID {dto.ProductId} no existe");
            }

            var inventoryExists = await _db.Inventory
                .AnyAsync(i => i.ProductId == dto.ProductId);

            if (inventoryExists)
            {
                throw new InvalidOperationException(
                    $"Ya existe un registro de inventario para el producto \"{product.ProductName}\""
                );
            }

            var entity = new Inventory
            {
                ProductId = dto.ProductId,
                Quantity = dto.Quantity,
                MinimumStock = dto.MinimumStock,
                MaximumStock = dto.MaximumStock,
                Location = !string.IsNullOrWhiteSpace(dto.Location) ? dto.Location.Trim() : null,
                Notes = !string.IsNullOrWhiteSpace(dto.Notes) ? dto.Notes.Trim() : null,
                IsActive = true,
                LastUpdatedAt = DateTime.Now
            };

            _db.Inventory.Add(entity);
            await _db.SaveChangesAsync();

            _logger.LogInfo("Inventario creado exitosamente con ID: {InventoryId}", entity.InventoryId);
            return MapToReadDto(entity);
        }

        private static InventoryReadDto MapToReadDto(Inventory inventory)
        {
            return new InventoryReadDto
            {
                InventoryId = inventory.InventoryId,
                ProductId = inventory.ProductId,
                Quantity = inventory.Quantity,
                MinimumStock = inventory.MinimumStock,
                MaximumStock = inventory.MaximumStock,
                Location = inventory.Location,
                Notes = inventory.Notes,
                IsActive = inventory.IsActive,
                LastUpdatedAt = inventory.LastUpdatedAt,
                LowStockAlert = inventory.Quantity <= inventory.MinimumStock
            };
        }

       
        public async Task<bool> DiscountQuantityAsync(int productId, int quantity)
        {
            if (productId <= 0)
            {
                _logger.LogWarning("ProductId inválido para descuento: {ProductId}", productId);
                return false;
            }

            if (quantity <= 0)
            {
                _logger.LogWarning("Cantidad inválida para descuento: {Quantity}", quantity);
                return false;
            }

            var inventory = await _db.Inventory
                .FirstOrDefaultAsync(i => i.ProductId == productId);

            if (inventory == null)
            {
                _logger.LogWarning("Inventario no encontrado para ProductId: {ProductId}", productId);
                return false;
            }

            if (inventory.Quantity < quantity)
            {
                _logger.LogWarning("Stock insuficiente para ProductId {ProductId}. Stock: {Stock}, Solicitado: {Requested}", 
                    productId, inventory.Quantity, quantity);
                return false;
            }

            inventory.Quantity -= quantity;
            inventory.LastUpdatedAt = DateTime.Now;

            _logger.LogInfo("Inventario descontado para ProductId {ProductId}: {Quantity} unidades", productId, quantity);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DiscountMultipleAsync(Dictionary<int, int> products)
        {
            if (products == null || products.Count == 0)
            {
                _logger.LogWarning("Diccionario de productos vacío para descuento múltiple");
                return false;
            }

            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                foreach (var product in products)
                {
                    var success = await DiscountQuantityAsync(product.Key, product.Value);
                    if (!success)
                    {
                        await tx.RollbackAsync();
                        _logger.LogWarning("Fallo en descuento múltiple para ProductId {ProductId}", product.Key);
                        return false;
                    }
                }

                await tx.CommitAsync();
                _logger.LogInfo("Descuento múltiple completado para {Count} productos", products.Count);
                return true;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogWarning("Error en descuento múltiple: {Error}", ex.InnerException?.Message ?? ex.Message);
                return false;
            }
        }

        public async Task<bool> DeleteAsync(int inventoryId)
        {
            if (inventoryId <= 0)
            {
                _logger.LogWarning("InventoryId inválido para eliminación: {InventoryId}", inventoryId);
                return false;
            }

            var inventory = await _db.Inventory
                .FirstOrDefaultAsync(i => i.InventoryId == inventoryId);

            if (inventory == null)
            {
                _logger.LogWarning("Inventario no encontrado para eliminación: {InventoryId}", inventoryId);
                return false;
            }

            if (inventory.Quantity != 0)
            {
                _logger.LogWarning("No se puede eliminar inventario con productos. InventoryId: {InventoryId}, Quantity: {Quantity}", 
                    inventoryId, inventory.Quantity);
                throw new InvalidOperationException($"No se puede eliminar el inventario. Hay {inventory.Quantity} unidades en stock. Debe reducir el stock a 0 antes de eliminar.");
            }

            inventory.IsActive = false;
            inventory.LastUpdatedAt = DateTime.Now;

            _logger.LogInfo("Inventario eliminado (borrado lógico) para InventoryId: {InventoryId}", inventoryId);
            await _db.SaveChangesAsync();
            return true;
        }

       
        public async Task<bool> UpdateAsync(int inventoryId, InventoryUpdateDto dto)
        {
            if (inventoryId <= 0)
            {
                _logger.LogWarning("InventoryId inválido para actualización: {InventoryId}", inventoryId);
                return false;
            }

            var inventory = await _db.Inventory
                .FirstOrDefaultAsync(i => i.InventoryId == inventoryId);

            if (inventory == null)
            {
                _logger.LogWarning("Inventario no encontrado para actualización: {InventoryId}", inventoryId);
                return false;
            }

            if (dto.MaximumStock < dto.MinimumStock)
            {
                _logger.LogWarning("Stock máximo no puede ser menor que mínimo. Max: {Max}, Min: {Min}", 
                    dto.MaximumStock, dto.MinimumStock);
                throw new InvalidOperationException("El stock máximo debe ser mayor o igual al stock mínimo.");
            }

            if (dto.Quantity > dto.MaximumStock)
            {
                _logger.LogWarning("Cantidad no puede exceder stock máximo. Qty: {Qty}, Max: {Max}", 
                    dto.Quantity, dto.MaximumStock);
                throw new InvalidOperationException("La cantidad no puede exceder el stock máximo.");
            }

            inventory.Quantity = dto.Quantity;
            inventory.MinimumStock = dto.MinimumStock;
            inventory.MaximumStock = dto.MaximumStock;
            inventory.Location = !string.IsNullOrWhiteSpace(dto.Location) ? dto.Location.Trim() : null;
            inventory.Notes = !string.IsNullOrWhiteSpace(dto.Notes) ? dto.Notes.Trim() : null;
            inventory.LastUpdatedAt = DateTime.Now;

            _logger.LogInfo("Inventario actualizado para InventoryId: {InventoryId}", inventoryId);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ReactivateAsync(int inventoryId)
        {
            if (inventoryId <= 0)
            {
                _logger.LogWarning("InventoryId inválido para reactivación: {InventoryId}", inventoryId);
                return false;
            }

            var inventory = await _db.Inventory
                .FirstOrDefaultAsync(i => i.InventoryId == inventoryId);

            if (inventory == null)
            {
                _logger.LogWarning("Inventario no encontrado para reactivación: {InventoryId}", inventoryId);
                return false;
            }

            if (inventory.IsActive)
            {
                _logger.LogInfo("Inventario ya está activo. InventoryId: {InventoryId}", inventoryId);
                return true; // ya estaba activo, no es error
            }

            inventory.IsActive = true;
            inventory.LastUpdatedAt = DateTime.Now;

            _logger.LogInfo("Inventario reactivado para InventoryId: {InventoryId}", inventoryId);
            await _db.SaveChangesAsync();

            return true;
        }
    }
}
