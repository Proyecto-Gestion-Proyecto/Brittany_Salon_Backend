using Brittany_Salon_Backend.Application.DTOs.Category;
using Brittany_Salon_Backend.Application.Exceptions;
using Brittany_Salon_Backend.Application.Services.Interfaces;
using Brittany_Salon_Backend.Application.Validators;
using Brittany_Salon_Backend.Domain.Entities;
using Brittany_Salon_Backend.Infrastructure.Persistence;
using Brittany_Salon_Backend.Infrastructure.Logging;
using Microsoft.EntityFrameworkCore;

namespace Brittany_Salon_Backend.Application.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly AppDbContext _db;
        private readonly IDevLogger _logger;

        public CategoryService(AppDbContext db, IDevLogger logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<CategoryReadDto> CreateAsync(CategoryCreateDto dto)
        {
            var validationErrors = CategoryValidator.ValidateCreate(dto, _logger);
            if (validationErrors.Count > 0)
                throw new ValidationException(validationErrors);

            var exists = await _db.Categories
                .AnyAsync(c => c.CategoryName.ToLower() == dto.CategoryName.Trim().ToLower());

            if (exists)
                throw new InvalidOperationException("Ya existe una categoría con ese nombre.");

            var entity = new Category
            {
                CategoryName = dto.CategoryName.Trim(),
                CategoryDescription = dto.CategoryDescription?.Trim(),
                IsActive = true
            };

            _db.Categories.Add(entity);
            await _db.SaveChangesAsync();

            return MapToReadDto(entity);
        }

       public async Task<List<CategoryReadDto>> GetAllAsync(bool onlyActive = true)
        {
            var query = _db.Categories.AsNoTracking();

            if (onlyActive)
                query = query.Where(c => c.IsActive);

            return await query
                .OrderBy(c => c.CategoryName)
                .Select(c => new CategoryReadDto
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    CategoryDescription = c.CategoryDescription,
                    IsActive = c.IsActive
                })
                .ToListAsync();
        }

        public async Task<CategoryReadDto?> GetByIdAsync(int id)
        {
            var entity = await _db.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (entity == null) return null;

            return MapToReadDto(entity);
        }

        public async Task<CategoryReadDto?> UpdateAsync(int id, CategoryUpdateDto dto)
        {
            var entity = await _db.Categories.FindAsync(id);
            if (entity is null) return null;

            var validationErrors = CategoryValidator.ValidateUpdate(dto, _logger);
            if (validationErrors.Count > 0)
                throw new ValidationException(validationErrors);

            var nameExists = await _db.Categories
                .AnyAsync(c => c.CategoryId != id &&
                               c.CategoryName.ToLower() == dto.CategoryName.Trim().ToLower());

            if (nameExists)
                throw new InvalidOperationException("Ya existe otra categoría con ese nombre.");

            entity.CategoryName = dto.CategoryName.Trim();
            entity.CategoryDescription = dto.CategoryDescription?.Trim();
            entity.IsActive = dto.IsActive;

            await _db.SaveChangesAsync();

            return MapToReadDto(entity);
        }

        public async Task<bool> DeactivateAsync(int id)
        {
            var entity = await _db.Categories.FirstOrDefaultAsync(c => c.CategoryId == id);
            if (entity is null) return false;

            var hasProducts = await _db.Products
                .AsNoTracking()
                .AnyAsync(p => p.CategoryId == id);

            if (hasProducts)
                throw new InvalidOperationException("No se puede desactivar la categoría porque está asociada a uno o más productos.");

            entity.IsActive = false;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ReactivateAsync(int id)
        {
            var entity = await _db.Categories.FindAsync(id);
            if (entity is null) return false;

            if (entity.IsActive) return true;

            entity.IsActive = true;
            await _db.SaveChangesAsync();
            return true;
        }

        private static CategoryReadDto MapToReadDto(Category entity)
        {
            return new CategoryReadDto
            {
                CategoryId = entity.CategoryId,
                CategoryName = entity.CategoryName,
                CategoryDescription = entity.CategoryDescription,
                IsActive = entity.IsActive
            };
        }
    }
}
