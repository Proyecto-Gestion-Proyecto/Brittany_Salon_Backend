using Brittany_Salon_Backend.Application.DTOs.Client;
using Brittany_Salon_Backend.Application.Exceptions;
using Brittany_Salon_Backend.Application.Services;
using Brittany_Salon_Backend.Domain.Entities;
using Brittany_Salon_Backend.Infrastructure.Logging;
using Brittany_Salon_Backend.Infrastructure.Persistence;
using Brittany_Salon_Backend.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

public class ClientTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static (ClientService svc, Mock<IImageService> img, Mock<IDevLogger> log, AppDbContext db) Build()
    {
        var db = CreateDb();
        var img = new Mock<IImageService>();
        var log = new Mock<IDevLogger>();

        var svc = new ClientService(db, img.Object, log.Object);
        return (svc, img, log, db);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesClient_AndNormalizesNameEmail()
    {
        // Arrange
        var (svc, _, _, db) = Build();

        var dto = new ClientCreateDto
        {
            Name = "  isaac   chevez  ",
            Email = "  ISAAC.CHEVEZ@TEST.COM  ",
            Phone = "88887777",
            Password = "Password123!",
            IsActive = true
        };

        // Act
        var created = await svc.CreateAsync(dto);

        // Assert: creado y con id
        Assert.True(created.ClientId > 0);

        // Assert: normalización
        Assert.Equal("Isaac Chevez", created.Name);
        Assert.Equal("isaac.chevez@test.com", created.Email);

        // Assert: demás campos
        Assert.Equal("88887777", created.Phone);
        Assert.True(created.IsActive);

        // Confirmar persistencia
        var entity = await db.Clients.FirstOrDefaultAsync(c => c.ClientId == created.ClientId);
        Assert.NotNull(entity);
        Assert.Equal("Isaac Chevez", entity!.Name);
        Assert.Equal("isaac.chevez@test.com", entity.Email);
    }

    [Fact]
    public async Task CreateAsync_WhenPasswordIsInvalid_ThrowsValidationException()
    {
        // Arrange
        var (svc, _, _, _) = Build();

        var dto = new ClientCreateDto
        {
            Name = "Isaac Chevez",
            Email = "isaac.chevez@test.com",
            Phone = "88887777",
            Password = "123",  
            IsActive = true
        };

        // Act + Assert
        var ex = await Assert.ThrowsAsync<Brittany_Salon_Backend.Application.Exceptions.ValidationException>(
            () => svc.CreateAsync(dto)
        );

        Assert.NotEmpty(ex.Errors);
    }
    [Fact]
    public async Task CreateAsync_WhenEmailAlreadyExists_ThrowsDuplicateResourceException()
    {
        // Arrange
        var (svc, _, _, db) = Build();

        // Cliente existente
        db.Clients.Add(new Clients
        {
            Name = "Cliente Existente",
            Email = "cliente@test.com",
            Phone = "88887777",
            Password = "Password123!",
            IsActive = true
        });
        await db.SaveChangesAsync();

        // Intento de registrar otro cliente con el mismo correo
        var dto = new ClientCreateDto
        {
            Name = "Nuevo Cliente",
            Email = "cliente@test.com", // correo duplicado
            Phone = "99998888",
            Password = "Password123!",
            IsActive = true
        };

        // Act + Assert
        var ex = await Assert.ThrowsAsync<DuplicateResourceException>(
            () => svc.CreateAsync(dto)
        );

        Assert.Equal("email", ex.Field);
    }

    [Fact]
    public async Task GetByIdAsync_WhenClientExists_ReturnsClientReadDto()
    {
        // Arrange
        var (svc, _, _, db) = Build();

        var client = new Clients
        {
            Name = "Isaac Chevez",
            Email = "isaac@test.com",
            Phone = "88887777",
            Password = "Password123!",
            IsActive = true
        };

        db.Clients.Add(client);
        await db.SaveChangesAsync();

        // Act
        var result = await svc.GetByIdAsync(client.ClientId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(client.ClientId, result!.ClientId);
        Assert.Equal("Isaac Chevez", result.Name);
        Assert.Equal("isaac@test.com", result.Email);
        Assert.Equal("88887777", result.Phone);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_WhenClientExists_UpdatesClientSuccessfully()
    {
        // Arrange
        var (svc, _, _, db) = Build();

        var client = new Clients
        {
            Name = "Ana Mora",
            Email = "ana@test.com",
            Phone = "11112222",
            Password = "Password123!",
            IsActive = true
        };

        db.Clients.Add(client);
        await db.SaveChangesAsync();

        var dto = new ClientUpdateDto
        {
            Name = "Isaac Chevez",
            Phone = "88887777",
            Email = "isaac@test.com"
        };

        // Act
        var result = await svc.UpdateAsync(client.ClientId, dto);

        // Assert
        Assert.True(result);

        var updated = await db.Clients.FirstOrDefaultAsync(c => c.ClientId == client.ClientId);
        Assert.NotNull(updated);
        Assert.Equal("Isaac Chevez", updated!.Name);
        Assert.Equal("88887777", updated.Phone);
        Assert.Equal("isaac@test.com", updated.Email);
    }

    [Fact]
    public async Task UpdateAsync_WhenPasswordIsInvalid_ThrowsValidationException()
    {
        // Arrange
        var (svc, _, _, db) = Build();

        var client = new Clients
        {
            Name = "Ana Mora",
            Email = "ana@test.com",
            Phone = "11112222",
            Password = "Password123!",
            IsActive = true
        };

        db.Clients.Add(client);
        await db.SaveChangesAsync();

        var dto = new ClientUpdateDto
        {
            Password = "123"
        };

        // Act + Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => svc.UpdateAsync(client.ClientId, dto)
        );

        Assert.NotEmpty(ex.Errors);
    }

    [Fact]
    public async Task DeactivateAsync_WhenClientExists_SetsIsActiveFalse()
    {
        // Arrange
        var (svc, _, _, db) = Build();

        var client = new Clients
        {
            Name = "Isaac Chevez",
            Email = "isaac@test.com",
            Phone = "88887777",
            Password = "Password123!",
            IsActive = true
        };

        db.Clients.Add(client);
        await db.SaveChangesAsync();

        // Act
        var result = await svc.DeactivateAsync(client.ClientId);

        // Assert
        Assert.True(result);

        var updated = await db.Clients.FirstOrDefaultAsync(c => c.ClientId == client.ClientId);
        Assert.NotNull(updated);
        Assert.False(updated!.IsActive);
    }
    [Fact]
    public async Task GetAllAsync_ReturnsClientsOrderedByName()
    {
        // Arrange
        var (svc, _, _, db) = Build();

        db.Clients.AddRange(
            new Clients
            {
                Name = "Zoe",
                Email = "zoe@test.com",
                Phone = "11112222",
                Password = "Password123!",
                IsActive = true
            },
            new Clients
            {
                Name = "Ana",
                Email = "ana@test.com",
                Phone = "33334444",
                Password = "Password123!",
                IsActive = true
            },
            new Clients
            {
                Name = "Luis",
                Email = "luis@test.com",
                Phone = "55556666",
                Password = "Password123!",
                IsActive = false
            }
        );
        await db.SaveChangesAsync();

        // Act
        var result = await svc.GetAllAsync(); // onlyActive = false por defecto

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("Ana", result[0].Name);
        Assert.Equal("Luis", result[1].Name);
        Assert.Equal("Zoe", result[2].Name);
    }






}
