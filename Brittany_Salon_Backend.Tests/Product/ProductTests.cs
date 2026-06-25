using Brittany_Salon_Backend.Application.DTOs.Product;
using Brittany_Salon_Backend.Application.Exceptions;
using Brittany_Salon_Backend.Application.Services;
using Brittany_Salon_Backend.Application.Validators;
using Brittany_Salon_Backend.Domain.Entities;
using Brittany_Salon_Backend.Infrastructure.Logging;
using Brittany_Salon_Backend.Infrastructure.Persistence;
using Brittany_Salon_Backend.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Xunit;

public class ProductTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options);
    }

    private static (ProductService svc, Mock<IImageService> imageService, Mock<IDevLogger> log, AppDbContext db) Build()
    {
        var db = CreateDb();
        var imageService = new Mock<IImageService>();
        var log = new Mock<IDevLogger>();

        var svc = new ProductService(db, imageService.Object, log.Object);
        return (svc, imageService, log, db);
    }

    private static IFormFile CreateMockImageFile()
    {
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.jpg");
        fileMock.Setup(f => f.Length).Returns(1024);
        fileMock.Setup(f => f.ContentType).Returns("image/jpeg");
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());
        return fileMock.Object;
    }

    //Test de crear

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesProduct()
    {
        var (svc, imageService, _, db) = Build();

        var category = new Category
        {
            CategoryName = "Champú",
            CategoryDescription = "Productos de champú",
            IsActive = true
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var mockImage = CreateMockImageFile();
        imageService.Setup(x => x.ProcessAndSaveImageAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync("/images/product_1.jpg");

        var dto = new ProductCreateDto
        {
            ProductName = "Champú Premium",
            ProductDescription = "Champú de calidad premium",
            Price = 25000m,
            CategoryId = category.CategoryId,
            ExpirationDate = DateTime.Now.AddYears(1),
            Image = mockImage
        };

        var result = await svc.CreateAsync(dto);

        Assert.NotNull(result);
        Assert.Equal("Champú Premium", result.ProductName);
        Assert.Equal(25000m, result.Price);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task CreateAsync_WhenCategoryDoesNotExist_ThrowsValidationException()
    {
        var (svc, _, _, _) = Build();
        var mockImage = CreateMockImageFile();

        var dto = new ProductCreateDto
        {
            ProductName = "Champú Premium",
            ProductDescription = "Descripción",
            Price = 25000m,
            CategoryId = 99999,
            ExpirationDate = DateTime.Now.AddYears(1),
            Image = mockImage
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => svc.CreateAsync(dto)
        );

        Assert.Contains("La categoría no existe", ex.Errors[0]);
    }

    [Fact]
    public async Task CreateAsync_WhenImageIsNull_ThrowsValidationException()
    {
        var (svc, _, _, db) = Build();

        var category = new Category
        {
            CategoryName = "Champú",
            CategoryDescription = "Productos de champú",
            IsActive = true
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var dto = new ProductCreateDto
        {
            ProductName = "Champú Premium",
            ProductDescription = "Descripción",
            Price = 25000m,
            CategoryId = category.CategoryId,
            ExpirationDate = DateTime.Now.AddYears(1),
            Image = null!
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => svc.CreateAsync(dto)
        );

        Assert.Contains("imagen", ex.Errors[0].ToLower());
    }

    // Test de actualizar

    [Fact]
    public async Task UpdateAsync_ValidDto_UpdatesProduct()
    {
        var (svc, imageService, _, db) = Build();

        var category = new Category
        {
            CategoryName = "Champú",
            CategoryDescription = "Productos de champú",
            IsActive = true
        };
        db.Categories.Add(category);

        var product = new Product
        {
            ProductName = "Champú Antiguo",
            ProductDescription = "Descripción antigua",
            Price = 10000m,
            CategoryId = category.CategoryId,
            IsActive = true,
            ImageUrl = "/images/old.jpg"
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var mockImage = CreateMockImageFile();
        imageService.Setup(x => x.ProcessAndSaveImageAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync("/images/product_new.jpg");

        var dto = new ProductUpdateDto
        {
            ProductName = "Champú Nuevo",
            ProductDescription = "Descripción nueva",
            Price = 30000m,
            CategoryId = category.CategoryId,
            IsActive = true,
            Image = mockImage
        };

        var result = await svc.UpdateAsync(product.ProductId, dto);

        Assert.NotNull(result);
        Assert.Equal("Champú Nuevo", result.ProductName);
        Assert.Equal(30000m, result.Price);
    }

    [Fact]
    public async Task UpdateAsync_WhenProductDoesNotExist_ReturnsNull()
    {
        var (svc, _, _, db) = Build();

        var category = new Category
        {
            CategoryName = "Champú",
            CategoryDescription = "Productos de champú",
            IsActive = true
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var dto = new ProductUpdateDto
        {
            ProductName = "Champú Nuevo",
            ProductDescription = "Descripción",
            Price = 25000m,
            CategoryId = category.CategoryId,
            IsActive = true,
            Image = null
        };

        var result = await svc.UpdateAsync(99999, dto);

        Assert.Null(result);
    }

    // Test de obtener por id

    [Fact]
    public async Task GetByIdAsync_WhenProductExists_ReturnsProduct()
    {
        var (svc, _, _, db) = Build();

        var category = new Category
        {
            CategoryName = "Champú",
            CategoryDescription = "Productos de champú",
            IsActive = true
        };
        db.Categories.Add(category);

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

        var result = await svc.GetByIdAsync(product.ProductId);

        Assert.NotNull(result);
        Assert.Equal("Champú Premium", result!.ProductName);
        Assert.Equal(25000m, result.Price);
    }

//Test de obtener por id cuando no existe el producto
    [Fact]
    public async Task GetByIdAsync_WhenProductDoesNotExist_ReturnsNull()
    {
        // Arrange
        var (svc, _, _, _) = Build();

        // Act
        var result = await svc.GetByIdAsync(99999);

        // Assert
        Assert.Null(result);
    }

    // Test de obtener todos los productos

    [Fact]
    public async Task GetAllAsync_ReturnsAllProducts()
    {
        var (svc, _, _, db) = Build();

        var category = new Category
        {
            CategoryName = "Champú",
            CategoryDescription = "Productos de champú",
            IsActive = true
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        db.Products.AddRange(
            new Product { ProductName = "Champú A", ProductDescription = "D", Price = 25000m, CategoryId = category.CategoryId, IsActive = true, ImageUrl = "/a.jpg" },
            new Product { ProductName = "Champú B", ProductDescription = "D", Price = 30000m, CategoryId = category.CategoryId, IsActive = true, ImageUrl = "/b.jpg" }
        );
        await db.SaveChangesAsync();

        var result = await svc.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

//Test de obtener todos los productos con filtro de solo activos
    [Fact]
    public async Task GetAllAsync_WithOnlyActiveFilter_ReturnsOnlyActiveProducts()
    {
        var (svc, _, _, db) = Build();

        var category = new Category
        {
            CategoryName = "Champú",
            CategoryDescription = "Productos de champú",
            IsActive = true
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        db.Products.AddRange(
            new Product { ProductName = "Activo", ProductDescription = "D", Price = 25000m, CategoryId = category.CategoryId, IsActive = true, ImageUrl = "/a.jpg" },
            new Product { ProductName = "Inactivo", ProductDescription = "D", Price = 30000m, CategoryId = category.CategoryId, IsActive = false, ImageUrl = "/i.jpg" }
        );
        await db.SaveChangesAsync();

        var result = await svc.GetAllAsync(onlyActive: true);

        Assert.Single(result);
    }

    // Test de búsqueda por nombre

    [Fact]
    public async Task SearchByNameAsync_ReturnsMatchingProducts()
    {
        var (svc, _, _, db) = Build();

        var category = new Category
        {
            CategoryName = "Champú",
            CategoryDescription = "Productos de champú",
            IsActive = true
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        db.Products.AddRange(
            new Product { ProductName = "Champú Premium", ProductDescription = "D", Price = 25000m, CategoryId = category.CategoryId, IsActive = true, ImageUrl = "/p.jpg" },
            new Product { ProductName = "Gel", ProductDescription = "D", Price = 15000m, CategoryId = category.CategoryId, IsActive = true, ImageUrl = "/g.jpg" }
        );
        await db.SaveChangesAsync();

        var result = await svc.SearchByNameAsync("Premium");

        Assert.Single(result);
        Assert.Equal("Champú Premium", result[0].ProductName);
    }

    // Test de desactivar producto

    [Fact]
    public async Task DeactivateAsync_WhenProductExists_DeactivatesProduct()
    {
        var (svc, _, _, db) = Build();

        var category = new Category
        {
            CategoryName = "Champú",
            CategoryDescription = "Productos de champú",
            IsActive = true
        };
        db.Categories.Add(category);

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

        var result = await svc.DeactivateAsync(product.ProductId);

        Assert.True(result);

        var updatedProduct = await db.Products.FirstOrDefaultAsync(p => p.ProductId == product.ProductId);
        Assert.NotNull(updatedProduct);
        Assert.False(updatedProduct!.IsActive);
    }

//Test de desactivar producto cuando no existe
    [Fact]
    public async Task DeactivateAsync_WhenProductDoesNotExist_ReturnsFalse()
    {
        var (svc, _, _, _) = Build();

        var result = await svc.DeactivateAsync(99999);

        Assert.False(result);
    }

    // Reactivar producto

    [Fact]
    public async Task ReactivateAsync_ReactivatesInactiveProduct()
    {
        var (svc, _, _, db) = Build();

        var category = new Category
        {
            CategoryName = "Champú",
            CategoryDescription = "Productos de champú",
            IsActive = true
        };
        db.Categories.Add(category);

        var product = new Product
        {
            ProductName = "Champú Premium",
            ProductDescription = "Descripción",
            Price = 25000m,
            CategoryId = category.CategoryId,
            IsActive = false,
            ImageUrl = "/images/product.jpg"
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var result = await svc.ReactivateAsync(product.ProductId);

        Assert.True(result);

        var updatedProduct = await db.Products.FirstOrDefaultAsync(p => p.ProductId == product.ProductId);
        Assert.True(updatedProduct!.IsActive);
    }

    [Fact]
    public async Task ReactivateAsync_WhenProductDoesNotExist_ReturnsFalse()
    {
        var (svc, _, _, _) = Build();

        var result = await svc.ReactivateAsync(99999);

        Assert.False(result);
    }

    // Test de validación de precio y categoría

    [Fact]
    public void ProductValidator_ValidatePrice_WithValidPrice_ReturnsNoErrors()
    {
        var errors = ProductValidator.ValidatePrice(25000.99m);
        Assert.Empty(errors);
    }

    [Fact]
    public void ProductValidator_ValidatePrice_WithZeroPrice_ReturnsError()
    {
        var errors = ProductValidator.ValidatePrice(0);

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void ProductValidator_ValidateCategoryId_WithValidId_ReturnsNoErrors()
    {
        var errors = ProductValidator.ValidateCategoryId(1);

        Assert.Empty(errors);
    }
}
