using Brittany_Salon_Backend.Application.DTOs.Service;
using Brittany_Salon_Backend.Application.Exceptions;
using Brittany_Salon_Backend.Application.Services;
using Brittany_Salon_Backend.Domain.Entities;
using Brittany_Salon_Backend.Infrastructure.Logging;
using Brittany_Salon_Backend.Infrastructure.Persistence;
using Brittany_Salon_Backend.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using AppointmentServiceEntity = Brittany_Salon_Backend.Domain.Entities.AppointmentService;

public class ServiceTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static (ServiceService svc, Mock<IImageService> img, Mock<IDevLogger> log, AppDbContext db) Build()
    {
        var db = CreateDb();
        var img = new Mock<IImageService>();
        var log = new Mock<IDevLogger>();

        var svc = new ServiceService(db, img.Object, log.Object);
        return (svc, img, log, db);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesService_AndNormalizesName()
    {
        // Arrange
        var (svc, _, _, db) = Build();

        var dto = new ServiceCreateDto
        {
            ServiceName = "  Corte de Cabello  ",
            ServiceDescription = "  Descripción del servicio  ",
            Price = 15000m,
            DurationMinutes = 60,
            ServiceType = "  Cabello  ",
            IsActive = true
        };

        // Act
        var created = await svc.CreateAsync(dto);

        // Assert: creado y con id
        Assert.True(created.ServiceId > 0);

        // Assert: normalización
        Assert.Equal("Corte de Cabello", created.ServiceName);
        Assert.Equal("Descripción del servicio", created.ServiceDescription);
        Assert.Equal("Cabello", created.ServiceType);

        // Assert: demás campos
        Assert.Equal(15000m, created.Price);
        Assert.Equal(60, created.DurationMinutes);
        Assert.True(created.IsActive);

        // Confirmar persistencia
        var entity = await db.Services.FirstOrDefaultAsync(s => s.ServiceId == created.ServiceId);
        Assert.NotNull(entity);
        Assert.Equal("Corte de Cabello", entity!.ServiceName);
    }

    [Fact]
    public async Task CreateAsync_WhenServiceNameIsInvalid_ThrowsValidationException()
    {
        // Arrange
        var (svc, _, _, _) = Build();

        var dto = new ServiceCreateDto
        {
            ServiceName = "",
            ServiceDescription = "Descripción",
            Price = 15000m,
            DurationMinutes = 60,
            ServiceType = "Cabello",
            IsActive = true
        };

        // Act + Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => svc.CreateAsync(dto)
        );

        Assert.NotEmpty(ex.Errors);
    }

    [Fact]
    public async Task GetByIdAsync_WhenServiceExists_ReturnsServiceReadDto()
    {
        // Arrange
        var (svc, _, _, db) = Build();

        var service = new Service
        {
            ServiceName = "Corte de Cabello",
            ServiceDescription = "Descripción",
            Price = 15000m,
            DurationMinutes = 60,
            ServiceType = "Cabello",
            IsActive = true
        };

        db.Services.Add(service);
        await db.SaveChangesAsync();

        // Act
        var result = await svc.GetByIdAsync(service.ServiceId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(service.ServiceId, result!.ServiceId);
        Assert.Equal("Corte de Cabello", result.ServiceName);
        Assert.Equal("Descripción", result.ServiceDescription);
        Assert.Equal(15000m, result.Price);
        Assert.Equal(60, result.DurationMinutes);
        Assert.Equal("Cabello", result.ServiceType);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_WhenServiceDoesNotExist_ReturnsNull()
    {
        // Arrange
        var (svc, _, _, _) = Build();

        // Act
        var result = await svc.GetByIdAsync(99999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_WhenServiceExists_UpdatesServiceSuccessfully()
    {
        // Arrange
        var (svc, _, _, db) = Build();

        var service = new Service
        {
            ServiceName = "Corte Básico",
            ServiceDescription = "Corte simple",
            Price = 10000m,
            DurationMinutes = 30,
            ServiceType = "Cabello",
            IsActive = true
        };

        db.Services.Add(service);
        await db.SaveChangesAsync();

        var dto = new ServiceUpdateDto
        {
            ServiceName = "Corte Avanzado",
            ServiceDescription = "Corte con estilo",
            Price = 20000m,
            DurationMinutes = 45,
            ServiceType = "Cabello",
            IsActive = true
        };

        // Act
        var result = await svc.UpdateAsync(service.ServiceId, dto);

        // Assert
        Assert.True(result);

        var updated = await db.Services.FirstOrDefaultAsync(s => s.ServiceId == service.ServiceId);
        Assert.NotNull(updated);
        Assert.Equal("Corte Avanzado", updated!.ServiceName);
        Assert.Equal("Corte con estilo", updated.ServiceDescription);
        Assert.Equal(20000m, updated.Price);
        Assert.Equal(45, updated.DurationMinutes);
        Assert.Equal("Cabello", updated.ServiceType);
    }

    [Fact]
    public async Task UpdateAsync_WhenServiceDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var (svc, _, _, _) = Build();

        var dto = new ServiceUpdateDto
        {
            ServiceName = "Nuevo Servicio",
            Price = 15000m,
            DurationMinutes = 60
        };

        // Act
        var result = await svc.UpdateAsync(99999, dto);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeactivateAsync_WhenServiceExists_SetsIsActiveFalse()
    {
        // Arrange
        var (svc, _, _, db) = Build();

        var service = new Service
        {
            ServiceName = "Corte de Cabello",
            ServiceDescription = "Descripción",
            Price = 15000m,
            DurationMinutes = 60,
            ServiceType = "Cabello",
            IsActive = true
        };

        db.Services.Add(service);
        await db.SaveChangesAsync();

        // Act
        var result = await svc.DeactivateAsync(service.ServiceId);

        // Assert
        Assert.True(result);

        var updated = await db.Services.FirstOrDefaultAsync(s => s.ServiceId == service.ServiceId);
        Assert.NotNull(updated);
        Assert.False(updated!.IsActive);
    }

    [Fact]
    public async Task DeactivateAsync_WhenServiceHasAppointments_ThrowsInvalidOperationException()
    {
        // Arrange
        var (svc, _, _, db) = Build();

        var service = new Service
        {
            ServiceName = "Corte de Cabello",
            ServiceDescription = "Descripción",
            Price = 15000m,
            DurationMinutes = 60,
            ServiceType = "Cabello",
            IsActive = true
        };

        db.Services.Add(service);
        await db.SaveChangesAsync();

        // Add an appointment service link
        var appointment = new Appointment
        {
            AppointmentDate = DateTime.Now.Date,
            StartTime = DateTime.Now.AddHours(1),
            EndTime = DateTime.Now.AddHours(2),
            AppointmentStatus = "Pendiente",
            ClientId = 1,
            IsActive = true
        };

        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();

        db.AppointmentServices.Add(new AppointmentServiceEntity
        {
            AppointmentId = appointment.AppointmentId,
            ServiceId = service.ServiceId,
            ServicePrice = service.Price
        });
        await db.SaveChangesAsync();

        // Act + Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.DeactivateAsync(service.ServiceId)
        );

        Assert.Contains("asociado a una o más citas", ex.Message);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsServicesOrderedByName()
    {
        // Arrange
        var (svc, _, _, db) = Build();

        db.Services.AddRange(
            new Service
            {
                ServiceName = "Zoe Servicio",
                ServiceDescription = "Descripción Z",
                Price = 20000m,
                DurationMinutes = 90,
                ServiceType = "Cabello",
                IsActive = true
            },
            new Service
            {
                ServiceName = "Ana Servicio",
                ServiceDescription = "Descripción A",
                Price = 15000m,
                DurationMinutes = 60,
                ServiceType = "Cabello",
                IsActive = true
            },
            new Service
            {
                ServiceName = "Luis Servicio",
                ServiceDescription = "Descripción L",
                Price = 10000m,
                DurationMinutes = 30,
                ServiceType = "Cabello",
                IsActive = false
            }
        );
        await db.SaveChangesAsync();

        // Act
        var result = await svc.GetAllAsync(); // onlyActive = false por defecto

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("Ana Servicio", result[0].ServiceName);
        Assert.Equal("Luis Servicio", result[1].ServiceName);
        Assert.Equal("Zoe Servicio", result[2].ServiceName);
    }

    [Fact]
    public async Task DeletePermanentlyAsync_WhenServiceExists_DeletesService()
    {
        // Arrange
        var (svc, img, _, db) = Build();

        var service = new Service
        {
            ServiceName = "Corte de Cabello",
            ServiceDescription = "Descripción",
            Price = 15000m,
            DurationMinutes = 60,
            ServiceType = "Cabello",
            IsActive = true,
            ImageUrl = "image.jpg"
        };

        db.Services.Add(service);
        await db.SaveChangesAsync();

        // Act
        var result = await svc.DeletePermanentlyAsync(service.ServiceId);

        // Assert
        Assert.True(result);

        var deleted = await db.Services.FirstOrDefaultAsync(s => s.ServiceId == service.ServiceId);
        Assert.Null(deleted);

        // Verificar que intentó borrar imagen
        img.Verify(x => x.DeleteImage("image.jpg"), Times.Once);
    }

    [Fact]
    public async Task DeletePermanentlyAsync_WhenServiceHasAppointments_ThrowsInvalidOperationException()
    {
        // Arrange
        var (svc, _, _, db) = Build();

        var service = new Service
        {
            ServiceName = "Corte de Cabello",
            ServiceDescription = "Descripción",
            Price = 15000m,
            DurationMinutes = 60,
            ServiceType = "Cabello",
            IsActive = true
        };

        db.Services.Add(service);
        await db.SaveChangesAsync();

        // Add an appointment service link
        var appointment = new Appointment
        {
            AppointmentDate = DateTime.Now.Date,
            StartTime = DateTime.Now.AddHours(1),
            EndTime = DateTime.Now.AddHours(2),
            AppointmentStatus = "Pendiente",
            ClientId = 1,
            IsActive = true
        };

        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();

        db.AppointmentServices.Add(new AppointmentServiceEntity
        {
            AppointmentId = appointment.AppointmentId,
            ServiceId = service.ServiceId,
            ServicePrice = service.Price
        });
        await db.SaveChangesAsync();

        // Act + Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.DeletePermanentlyAsync(service.ServiceId)
        );

        Assert.Contains("asociado a una o más citas", ex.Message);
    }
}