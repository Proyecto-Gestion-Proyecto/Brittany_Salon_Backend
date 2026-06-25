using Brittany_Salon_Backend.Application.DTOs.Category;
using Brittany_Salon_Backend.Application.Exceptions;
using Brittany_Salon_Backend.Application.Services;
using Brittany_Salon_Backend.Domain.Entities;
using Brittany_Salon_Backend.Infrastructure.Logging;
using Brittany_Salon_Backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

public class CategoryTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static (CategoryService svc, Mock<IDevLogger> log, AppDbContext db) Build()
    {
        var db = CreateDb();
        var log = new Mock<IDevLogger>();

        var svc = new CategoryService(db, log.Object);
        return (svc, log, db);
    }

//Crear categoria
    [Fact]
    public async Task CreateAsync_ValidDto_CreatesCategory()
    {
        var (svc, _, db) = Build();

        var dto = new CategoryCreateDto
        {
            CategoryName = "Champú",
            CategoryDescription = "Productos para limpiar cabello"
        };

        var result = await svc.CreateAsync(dto);

        Assert.NotNull(result);
        Assert.Equal("Champú", result.CategoryName);
        Assert.Equal("Productos para limpiar cabello", result.CategoryDescription);
        Assert.True(result.IsActive);

        var categoryInDb = await db.Categories.FirstOrDefaultAsync(c => c.CategoryId == result.CategoryId);
        Assert.NotNull(categoryInDb);
        Assert.Equal("Champú", categoryInDb!.CategoryName);
    }

    [Fact]
    public async Task CreateAsync_WhenCategoryNameIsEmpty_ThrowsValidationException()
    {
        var (svc, _, _) = Build();

        var dto = new CategoryCreateDto
        {
            CategoryName = "",
            CategoryDescription = "Descripción"
        };

        await Assert.ThrowsAsync<ValidationException>(
            () => svc.CreateAsync(dto)
        );
    }

    [Fact]
    public async Task CreateAsync_WhenCategoryNameAlreadyExists_ThrowsInvalidOperationException()
    {
        var (svc, _, db) = Build();

        var existingCategory = new Category
        {
            CategoryName = "Champú",
            CategoryDescription = "Descripción existente",
            IsActive = true
        };
        db.Categories.Add(existingCategory);
        await db.SaveChangesAsync();

        var dto = new CategoryCreateDto
        {
            CategoryName = "Champú",
            CategoryDescription = "Nueva descripción"
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.CreateAsync(dto)
        );

        Assert.Contains("Ya existe una categoría", ex.Message);
    }

    //Desactivar categoria

    [Fact]
    public async Task DeactivateAsync_WhenCategoryExists_DeactivatesCategory()
    {
        var (svc, _, db) = Build();

        var category = new Category
        {
            CategoryName = "Champú",
            CategoryDescription = "Descripción",
            IsActive = true
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var result = await svc.DeactivateAsync(category.CategoryId);

        Assert.True(result);

        var deactivatedCategory = await db.Categories.FirstOrDefaultAsync(c => c.CategoryId == category.CategoryId);
        Assert.NotNull(deactivatedCategory);
        Assert.False(deactivatedCategory!.IsActive);
    }

    [Fact]
    public async Task DeactivateAsync_WhenCategoryDoesNotExist_ReturnsFalse()
    {
        var (svc, _, _) = Build();

        var result = await svc.DeactivateAsync(99999);

        Assert.False(result);
    }

    [Fact]
    public async Task DeactivateAsync_WhenCategoryHasProducts_ThrowsInvalidOperationException()
    {
        var (svc, _, db) = Build();

        var category = new Category
        {
            CategoryName = "Champú",
            CategoryDescription = "Descripción",
            IsActive = true
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var product = new Product
        {
            ProductName = "Champú Premium",
            ProductDescription = "Descripción",
            Price = 25000m,
            CategoryId = category.CategoryId,
            IsActive = true,
            ImageUrl = "/images/product.jpg"
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.DeactivateAsync(category.CategoryId)
        );

        Assert.Contains("No se puede desactivar", ex.Message);
    }
}
