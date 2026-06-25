using Brittany_Salon_Backend.Application.DTOs.Product;
using Brittany_Salon_Backend.Application.Exceptions;
using Brittany_Salon_Backend.Application.Services.Interfaces;
using Brittany_Salon_Backend.Application.Validators;
using Brittany_Salon_Backend.Domain.Entities;
using Brittany_Salon_Backend.Infrastructure.Logging;
using Brittany_Salon_Backend.Infrastructure.Persistence;
using Brittany_Salon_Backend.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Brittany_Salon_Backend.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly AppDbContext _db;
        private readonly IImageService _imageService;
        private readonly IDevLogger _logger;

        public ProductService(AppDbContext db, IImageService imageService, IDevLogger logger)
        {
            _db = db;
            _imageService = imageService;
            _logger = logger;
        }

        public async Task<ProductReadDto> CreateAsync(ProductCreateDto dto)
        {
            var validationErrors = ProductValidator.ValidateCreate(dto, _logger);
            if (validationErrors.Count > 0)
                throw new ValidationException(validationErrors);

            var categoryErrors = await ProductValidator.ValidateCategoryExistsAsync(dto.CategoryId, _db, _logger);
            if (categoryErrors.Count > 0)
                throw new ValidationException(categoryErrors);

            var normalizedProductName = dto.ProductName.Trim().ToLower();
            var productExists = await _db.Products
                .AnyAsync(p => p.ProductName.ToLower() == normalizedProductName);

            if (productExists)
            {
                _logger.LogWarning("Intento de crear producto duplicado: {ProductName}", dto.ProductName);
                throw new InvalidOperationException($"Ya existe un producto con el nombre '{dto.ProductName}'.");
            }

            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                var entity = new Product
                {
                    ProductName = dto.ProductName.Trim(),
                    ProductDescription = dto.ProductDescription?.Trim(),
                    Price = dto.Price,
                    ImageUrl = null,
                    ExpirationDate = dto.ExpirationDate,
                    IsActive = true,
                    CategoryId = dto.CategoryId
                };

                _db.Products.Add(entity);
                await _db.SaveChangesAsync();

                var inventory = new Inventory
                {
                    ProductId = entity.ProductId,
                    Quantity = 0,
                    MinimumStock = 0,
                    MaximumStock = 0,
                    Location = null,
                    Notes = null,
                    IsActive = true,
                    LastUpdatedAt = DateTime.UtcNow
                };

                _db.Inventory.Add(inventory);
                await _db.SaveChangesAsync();

                _logger.LogInfo("Producto creado con inventario vacío. ProductId: {ProductId}, InventoryId: {InventoryId}", 
                    entity.ProductId, inventory.InventoryId);

                if (dto.Image != null && dto.Image.Length > 0)
                {
                    await ProcessProductImageAsync(entity, dto.Image);
                }

                await _db.Entry(entity).Reference(p => p.Category).LoadAsync();
                await tx.CommitAsync();

                return MapToReadDto(entity);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogWarning("Error al crear producto e inventario: {Error}", ex.Message);
                throw;
            }
        }


        private static ProductReadDto MapToReadDto(Product entity)
        {
            return new ProductReadDto
            {
                ProductId = entity.ProductId,
                ProductName = entity.ProductName,
                ProductDescription = entity.ProductDescription,
                Price = entity.Price,
                ImageUrl = entity.ImageUrl,
                ExpirationDate = entity.ExpirationDate,
                IsActive = entity.IsActive,
                CategoryId = entity.CategoryId,
                CategoryName = entity.Category?.CategoryName ?? string.Empty
            };
        }

        private async Task ProcessProductImageAsync(Product entity, IFormFile imageFile)
        {
            string imageUrl = await _imageService.ProcessAndSaveImageAsync(imageFile, "imageProduct", entity.ProductId);
            entity.ImageUrl = imageUrl;
            await _db.SaveChangesAsync();
        }

        private async Task UpdateProductImageAsync(Product entity, IFormFile newImage)
        {
            if (!string.IsNullOrWhiteSpace(entity.ImageUrl))
            {
                _imageService.DeleteImage(entity.ImageUrl);
            }

            string imageUrl = await _imageService.ProcessAndSaveImageAsync(newImage, "imageProduct", entity.ProductId);
            entity.ImageUrl = imageUrl;
        }
        public async Task<List<ProductReadDto>> GetAllAsync(bool? onlyActive = null)
        {
            IQueryable<Product> query = _db.Products
            .AsNoTracking()
            .Include(p => p.Category);

            if (onlyActive.HasValue)
                query = query.Where(p => p.IsActive == onlyActive.Value);

            return await query
                .OrderBy(p => p.ProductName)
                .Select(p => new ProductReadDto
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    ProductDescription = p.ProductDescription,
                    Price = p.Price,
                    ImageUrl = p.ImageUrl,
                    ExpirationDate = p.ExpirationDate,
                    IsActive = p.IsActive,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.CategoryName : string.Empty
                })
                .ToListAsync();
        }

        public async Task<ProductReadDto?> GetByIdAsync(int id)
        {
            var p = await _db.Products
                .AsNoTracking()
                .Include(x => x.Category)
                .FirstOrDefaultAsync(x => x.ProductId == id);

            if (p is null) return null;

            return new ProductReadDto
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                ProductDescription = p.ProductDescription,
                Price = p.Price,
                ImageUrl = p.ImageUrl,
                ExpirationDate = p.ExpirationDate,
                IsActive = p.IsActive,
                CategoryId = p.CategoryId,
                CategoryName = p.Category != null ? p.Category.CategoryName : string.Empty
            };
        }
        public async Task<List<ProductReadDto>> SearchByNameAsync(string name, bool? onlyActive = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new List<ProductReadDto>();

            var searchTerm = name.Trim().ToLower();

            IQueryable<Product> query = _db.Products
                .AsNoTracking()
                .Include(p => p.Category);

            if (onlyActive.HasValue)
                query = query.Where(p => p.IsActive == onlyActive.Value);

            return await query
                .Where(p => p.ProductName.ToLower().Contains(searchTerm))
                .OrderBy(p => p.ProductName)
                .Select(p => new ProductReadDto
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    ProductDescription = p.ProductDescription,
                    Price = p.Price,
                    ImageUrl = p.ImageUrl,
                    ExpirationDate = p.ExpirationDate,
                    IsActive = p.IsActive,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.CategoryName : string.Empty
                })
                .ToListAsync();
        }
        public async Task<ProductReadDto?> UpdateAsync(int id, ProductUpdateDto dto)
        {
            var entity = await _db.Products.FindAsync(id);
            if (entity is null) return null;

            var validationErrors = ProductValidator.ValidateUpdate(dto, _logger);
            if (validationErrors.Count > 0)
                throw new ValidationException(validationErrors);

            var categoryErrors = await ProductValidator.ValidateCategoryExistsAsync(dto.CategoryId, _db, _logger);
            if (categoryErrors.Count > 0)
                throw new ValidationException(categoryErrors);

            entity.ProductName = dto.ProductName.Trim();
            entity.ProductDescription = dto.ProductDescription?.Trim();
            entity.Price = dto.Price;
            entity.ExpirationDate = dto.ExpirationDate;
            entity.CategoryId = dto.CategoryId;
            entity.IsActive = dto.IsActive;

            if (dto.Image != null && dto.Image.Length > 0)
            {
                await UpdateProductImageAsync(entity, dto.Image);
            }

            await _db.SaveChangesAsync();

            await _db.Entry(entity).Reference(p => p.Category).LoadAsync();

            return MapToReadDto(entity);
        }


        public async Task<bool> DeactivateAsync(int id)
        {
            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                var product = await _db.Products
                    .FirstOrDefaultAsync(p => p.ProductId == id);

                if (product is null) return false;

                if (!product.IsActive) return true; // ya está desactivado

                product.IsActive = false;

                // Desactivar inventario asociado (si existe)
                var inventory = await _db.Inventory
                    .FirstOrDefaultAsync(i => i.ProductId == id);

                if (inventory != null)
                {
                    inventory.IsActive = false;
                    inventory.LastUpdatedAt = DateTime.UtcNow;
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogWarning("Error desactivando producto/inventario. ProductId: {ProductId}. Error: {Error}",
                    id, ex.InnerException?.Message ?? ex.Message);
                throw;
            }
        }

        public async Task<bool> ReactivateAsync(int id)
        {
            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                var product = await _db.Products
                    .FirstOrDefaultAsync(p => p.ProductId == id);

                if (product is null) return false;

                if (product.IsActive) return true; // ya está activo

                product.IsActive = true;

                // Reactivar inventario asociado (si existe)
                var inventory = await _db.Inventory
                    .FirstOrDefaultAsync(i => i.ProductId == id);

                if (inventory != null)
                {
                    inventory.IsActive = true;
                    inventory.LastUpdatedAt = DateTime.UtcNow;
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogWarning("Error reactivando producto/inventario. ProductId: {ProductId}. Error: {Error}",
                    id, ex.InnerException?.Message ?? ex.Message);
                throw;
            }
        }

        private async Task<bool> HasAssociatedAppointmentsAsync(int productId)
        {
            return await _db.AppointmentProducts
                .AsNoTracking()
                .AnyAsync(ap => ap.ProductId == productId);
        }




    }
}
