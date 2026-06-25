using Brittany_Salon_Backend.Application.DTOs.Inventory;
using Brittany_Salon_Backend.Application.Exceptions;
using Brittany_Salon_Backend.Application.Services;
using Brittany_Salon_Backend.Domain.Entities;
using Brittany_Salon_Backend.Infrastructure.Logging;
using Brittany_Salon_Backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Brittany_Salon_Backend.Tests.Inventory
{
    public class InventoryTests
    {
       
        private static AppDbContext BuildDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new AppDbContext(options);
        }

        private static async Task SeedCategoryAndProductAsync(AppDbContext db, int productId = 1)
        {
            var category = await db.Categories.FirstOrDefaultAsync(c => c.CategoryId == 1);
            if (category == null)
            {
                category = new Category
                {
                    CategoryId = 1,
                    CategoryName = "Test Category",
                    IsActive = true
                };

                db.Categories.Add(category);
                await db.SaveChangesAsync();
            }

            var productExists = await db.Products.AnyAsync(p => p.ProductId == productId);
            if (!productExists)
            {
                var product = new Product
                {
                    ProductId = productId,
                    ProductName = $"Shampoo Test {productId}",
                    ProductDescription = "Desc",
                    Price = 1000,
                    ImageUrl = null,
                    ExpirationDate = null,
                    IsActive = true,
                    CategoryId = 1
                };

                db.Products.Add(product);
                await db.SaveChangesAsync();
            }
        }
        private class FakeDevLogger : IDevLogger
        {
            public bool IsEnabled => false;

            public void LogInfo(string message, params object[] args) { }

            public void LogWarning(string message, params object[] args) { }

            public void LogError(string message, Exception? exception = null, params object[] args) { }

            public void LogDebug(string message, params object[] args) { }

            public void LogValidation(string context, object? data) { }
        }

       

        [Fact]
        public async Task CreateAsync_WhenProductExistsAndNoInventory_CreatesInventorySuccessfully()
        {
            // Arrange
            var db = BuildDbContext();
            await SeedCategoryAndProductAsync(db, productId: 5);

            var logger = new FakeDevLogger();
            var service = new InventoryService(db, logger);

            var dto = new InventoryCreateDto
            {
                ProductId = 5,
                Quantity = 10,
                MinimumStock = 2,
                MaximumStock = 20,
                Location = "  Bodega A  ",
                Notes = "  Nota test  "
            };

            // Act
            var result = await service.CreateAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.InventoryId > 0);

            Assert.Equal(5, result.ProductId);
            Assert.Equal(10, result.Quantity);
            Assert.Equal(2, result.MinimumStock);
            Assert.Equal(20, result.MaximumStock);

          
            Assert.Equal("Bodega A", result.Location);
            Assert.Equal("Nota test", result.Notes);

            Assert.True(result.IsActive);
            Assert.True(result.LastUpdatedAt != default);

            // También valida que realmente quedó en DB
            var entity = await db.Inventory.FirstOrDefaultAsync(i => i.ProductId == 5);
            Assert.NotNull(entity);
            Assert.True(entity!.IsActive);
        }

        [Fact]
        public async Task CreateAsync_WhenProductDoesNotExist_ThrowsNotFoundException()
        {
            // Arrange
            var db = BuildDbContext();
            // NO sembramos producto

            var logger = new FakeDevLogger();
            var service = new InventoryService(db, logger);

            var dto = new InventoryCreateDto
            {
                ProductId = 999,
                Quantity = 0,
                MinimumStock = 0,
                MaximumStock = 0,
                Location = null,
                Notes = null
            };

            // Act + Assert
            await Assert.ThrowsAsync<NotFoundException>(() => service.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateAsync_WhenInventoryAlreadyExists_ThrowsInvalidOperationException()
        {
            // Arrange
            var db = BuildDbContext();
            await SeedCategoryAndProductAsync(db, productId: 5);

            // Creamos inventario previo para ese producto
            db.Inventory.Add(new Domain.Entities.Inventory
            {
                ProductId = 5,
                Quantity = 1,
                MinimumStock = 0,
                MaximumStock = 10,
                Location = "X",
                Notes = "Y",
                IsActive = true,
                LastUpdatedAt = DateTime.Now
            });
            await db.SaveChangesAsync();

            var logger = new FakeDevLogger();
            var service = new InventoryService(db, logger);

            var dto = new InventoryCreateDto
            {
                ProductId = 5,
                Quantity = 2,
                MinimumStock = 1,
                MaximumStock = 10,
                Location = "Bodega",
                Notes = "duplicado"
            };

            // Act + Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(dto));
        }
        // Verificar que no se pueda descontar stock de un producto si no tiene registro en inventario
        [Fact]
        public async Task DiscountQuantityAsync_WhenInventoryNotFound_ReturnsFalse()
        {
            var db = BuildDbContext();
            await SeedCategoryAndProductAsync(db, productId: 5); // Producto existe, inventario no

            var service = new InventoryService(db, new FakeDevLogger());

            var ok = await service.DiscountQuantityAsync(productId: 5, quantity: 1);

            Assert.False(ok);
        }
        // Verificar que no se pueda descontar más stock del que hay disponible
        [Fact]
        public async Task DiscountQuantityAsync_WhenStockInsufficient_ReturnsFalse_AndKeepsQuantity()
        {
            var db = BuildDbContext();
            await SeedCategoryAndProductAsync(db, productId: 5);

            db.Inventory.Add(new Domain.Entities.Inventory
            {
                ProductId = 5,
                Quantity = 2,
                MinimumStock = 0,
                MaximumStock = 10,
                Location = "Bodega",
                Notes = null,
                IsActive = true,
                LastUpdatedAt = DateTime.Now
            });
            await db.SaveChangesAsync();

            var service = new InventoryService(db, new FakeDevLogger());

            var ok = await service.DiscountQuantityAsync(productId: 5, quantity: 5);

            Assert.False(ok);

            var inv = await db.Inventory.FirstAsync(i => i.ProductId == 5);
            Assert.Equal(2, inv.Quantity);
        }

        // Comprueba que si se puede descontar del stock
        [Fact]
        public async Task DiscountQuantityAsync_WhenStockSufficient_ReturnsTrue_AndDiscountsQuantity()
        {
            var db = BuildDbContext();
            await SeedCategoryAndProductAsync(db, productId: 5);

            db.Inventory.Add(new Domain.Entities.Inventory
            {
                ProductId = 5,
                Quantity = 10,
                MinimumStock = 0,
                MaximumStock = 20,
                Location = "Bodega",
                Notes = null,
                IsActive = true,
                LastUpdatedAt = DateTime.Now
            });
            await db.SaveChangesAsync();

            var service = new InventoryService(db, new FakeDevLogger());

            var ok = await service.DiscountQuantityAsync(productId: 5, quantity: 3);

            Assert.True(ok);

            var inv = await db.Inventory.FirstAsync(i => i.ProductId == 5);
            Assert.Equal(7, inv.Quantity);
            Assert.True(inv.LastUpdatedAt != default);
        }
        
        [Fact]
        public async Task DiscountMultipleAsync_WhenAllDiscountsSucceed_ReturnsTrue_AndDiscountsAll()
        {
            var db = BuildDbContext();
            await SeedCategoryAndProductAsync(db, productId: 1);
            await SeedCategoryAndProductAsync(db, productId: 2);

            db.Inventory.AddRange(
                new Domain.Entities.Inventory
                {
                    ProductId = 1,
                    Quantity = 10,
                    MinimumStock = 0,
                    MaximumStock = 50,
                    Location = "A",
                    Notes = null,
                    IsActive = true,
                    LastUpdatedAt = DateTime.Now
                },
                new Domain.Entities.Inventory
                {
                    ProductId = 2,
                    Quantity = 5,
                    MinimumStock = 0,
                    MaximumStock = 50,
                    Location = "B",
                    Notes = null,
                    IsActive = true,
                    LastUpdatedAt = DateTime.Now
                }
            );
            await db.SaveChangesAsync();

            var service = new InventoryService(db, new FakeDevLogger());

            var ok = await service.DiscountMultipleAsync(new System.Collections.Generic.Dictionary<int, int>
    {
        { 1, 3 },
        { 2, 2 }
    });

            Assert.True(ok);

            var inv1 = await db.Inventory.FirstAsync(i => i.ProductId == 1);
            var inv2 = await db.Inventory.FirstAsync(i => i.ProductId == 2);

            Assert.Equal(7, inv1.Quantity);
            Assert.Equal(3, inv2.Quantity);
        }

        [Fact]
        public async Task DiscountMultipleAsync_WhenAnyDiscountFails_ReturnsFalse()
        {
            var db = BuildDbContext();
            await SeedCategoryAndProductAsync(db, productId: 1);
            await SeedCategoryAndProductAsync(db, productId: 2);

            db.Inventory.AddRange(
                new Domain.Entities.Inventory
                {
                    ProductId = 1,
                    Quantity = 10,
                    MinimumStock = 0,
                    MaximumStock = 50,
                    Location = "A",
                    Notes = null,
                    IsActive = true,
                    LastUpdatedAt = DateTime.Now
                },
                new Domain.Entities.Inventory
                {
                    ProductId = 2,
                    Quantity = 1,
                    MinimumStock = 0,
                    MaximumStock = 50,
                    Location = "B",
                    Notes = null,
                    IsActive = true,
                    LastUpdatedAt = DateTime.Now
                }
            );
            await db.SaveChangesAsync();

            var service = new InventoryService(db, new FakeDevLogger());

            var ok = await service.DiscountMultipleAsync(new System.Collections.Generic.Dictionary<int, int>
    {
        { 1, 3 }, // podría
        { 2, 5 }  // no puede
    });

            Assert.False(ok);

           
        }
        [Fact]
        public async Task UpdateAsync_WhenDataIsValid_UpdatesInventorySuccessfully()
        {
            // Arrange
            var db = BuildDbContext();
            await SeedCategoryAndProductAsync(db, productId: 10);

            var inventory = new Domain.Entities.Inventory
            {
                InventoryId = 1,
                ProductId = 10,
                Quantity = 5,
                MinimumStock = 1,
                MaximumStock = 20,
                Location = "Old",
                Notes = "Old",
                IsActive = true,
                LastUpdatedAt = DateTime.Now
            };

            db.Inventory.Add(inventory);
            await db.SaveChangesAsync();

            var logger = new FakeDevLogger();
            var service = new InventoryService(db, logger);

            var dto = new InventoryUpdateDto
            {
                Quantity = 8,
                MinimumStock = 2,
                MaximumStock = 30,
                Location = "  Bodega Nueva  ",
                Notes = "  Nota Nueva  "
            };

            // Act
            var result = await service.UpdateAsync(1, dto);

            // Assert
            Assert.True(result);

            var updated = await db.Inventory.FirstAsync(i => i.InventoryId == 1);

            Assert.Equal(8, updated.Quantity);
            Assert.Equal(2, updated.MinimumStock);
            Assert.Equal(30, updated.MaximumStock);
            Assert.Equal("Bodega Nueva", updated.Location);
            Assert.Equal("Nota Nueva", updated.Notes);
        }
        [Fact]
        public async Task UpdateAsync_WhenMaximumLessThanMinimum_ThrowsInvalidOperationException()
        {
            // Arrange
            var db = BuildDbContext();
            await SeedCategoryAndProductAsync(db, productId: 20);

            db.Inventory.Add(new Domain.Entities.Inventory
            {
                InventoryId = 2,
                ProductId = 20,
                Quantity = 5,
                MinimumStock = 1,
                MaximumStock = 10,
                IsActive = true,
                LastUpdatedAt = DateTime.Now
            });

            await db.SaveChangesAsync();

            var logger = new FakeDevLogger();
            var service = new InventoryService(db, logger);

            var dto = new InventoryUpdateDto
            {
                Quantity = 5,
                MinimumStock = 10,
                MaximumStock = 5, // inválido
                Location = "Test",
                Notes = "Test"
            };

            // Act + Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.UpdateAsync(2, dto));
        }
        [Fact]
        public async Task DeleteAsync_WhenQuantityIsZero_DeactivatesInventoryAndReturnsTrue()
        {
            // Arrange
            var db = BuildDbContext();
            await SeedCategoryAndProductAsync(db, productId: 40);

            db.Inventory.Add(new Domain.Entities.Inventory
            {
                InventoryId = 10,
                ProductId = 40,
                Quantity = 0,
                MinimumStock = 0,
                MaximumStock = 10,
                Location = "Test",
                Notes = "Test",
                IsActive = true,
                LastUpdatedAt = DateTime.Now
            });

            await db.SaveChangesAsync();

            var logger = new FakeDevLogger();
            var service = new InventoryService(db, logger);

            // Act
            var result = await service.DeleteAsync(10);

            // Assert
            Assert.True(result);

            var entity = await db.Inventory.FirstAsync(i => i.InventoryId == 10);
            Assert.False(entity.IsActive);
            Assert.True(entity.LastUpdatedAt != default);
        }
        [Fact]
        public async Task GetAllAsync_WhenInventoriesExist_ReturnsAllRecords()
        {
            // Arrange
            var db = BuildDbContext();
            await SeedCategoryAndProductAsync(db, productId: 60);

            db.Inventory.AddRange(
                new Domain.Entities.Inventory
                {
                    ProductId = 60,
                    Quantity = 5,
                    MinimumStock = 1,
                    MaximumStock = 10,
                    Location = "Bodega A",
                    IsActive = true,
                    LastUpdatedAt = DateTime.Now
                },
                new Domain.Entities.Inventory
                {
                    ProductId = 61,
                    Quantity = 2,
                    MinimumStock = 1,
                    MaximumStock = 10,
                    Location = "Bodega B",
                    IsActive = false,
                    LastUpdatedAt = DateTime.Now
                });

            await db.SaveChangesAsync();

            var logger = new FakeDevLogger();
            var service = new InventoryService(db, logger);

            // Act
            var result = await service.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }
    }
}