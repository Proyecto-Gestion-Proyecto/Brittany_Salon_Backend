using Brittany_Salon_Backend.Application.DTOs.Service;
using Brittany_Salon_Backend.Application.Exceptions;
using Brittany_Salon_Backend.Application.Services;
using Brittany_Salon_Backend.Domain.Entities;
using Brittany_Salon_Backend.Infrastructure.Logging;
using Brittany_Salon_Backend.Infrastructure.Persistence;
using Brittany_Salon_Backend.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Brittany_Salon_Backend.Tests.Services
{
    public class ServiceServiceTests : IDisposable
    {
        private readonly AppDbContext _db;
        private readonly Mock<IImageService> _imageServiceMock;
        private readonly Mock<IDevLogger> _loggerMock;
        private readonly ServiceService _service;

        public ServiceServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _db = new AppDbContext(options);
            _imageServiceMock = new Mock<IImageService>();
            _loggerMock = new Mock<IDevLogger>();
            _service = new ServiceService(_db, _imageServiceMock.Object, _loggerMock.Object);
        }

        public void Dispose()
        {
            _db.Database.EnsureDeleted();
            _db.Dispose();
        }

        #region GetAllAsync Tests

        [Fact]
        public async Task GetAllAsync_ReturnsAllServices_WhenOnlyActiveIsFalse()
        {
            // Arrange
            _db.Services.AddRange(
                new Service { ServiceName = "Corte", Price = 10000, DurationMinutes = 30, IsActive = true },
                new Service { ServiceName = "Tinte", Price = 25000, DurationMinutes = 60, IsActive = false }
            );
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.GetAllAsync(onlyActive: false);

            // Assert
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsOnlyActiveServices_WhenOnlyActiveIsTrue()
        {
            // Arrange
            _db.Services.AddRange(
                new Service { ServiceName = "Corte", Price = 10000, DurationMinutes = 30, IsActive = true },
                new Service { ServiceName = "Tinte", Price = 25000, DurationMinutes = 60, IsActive = false }
            );
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.GetAllAsync(onlyActive: true);

            // Assert
            Assert.Single(result);
            Assert.Equal("Corte", result[0].ServiceName);
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_ReturnsService_WhenServiceExists()
        {
            // Arrange
            var service = new Service { ServiceName = "Corte", Price = 10000, DurationMinutes = 30, IsActive = true };
            _db.Services.Add(service);
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.GetByIdAsync(service.ServiceId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Corte", result.ServiceName);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenServiceDoesNotExist()
        {
            // Act
            var result = await _service.GetByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_CreatesService_WhenValidDto()
        {
            // Arrange
            var dto = new ServiceCreateDto
            {
                ServiceName = "Corte Premium",
                ServiceDescription = "Corte de cabello premium",
                Price = 15000,
                DurationMinutes = 45,
                ServiceType = "Cabello",
                IsActive = true
            };

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Corte Premium", result.ServiceName);
            Assert.Equal(15000, result.Price);

            var dbService = await _db.Services.FirstOrDefaultAsync();
            Assert.NotNull(dbService);
        }

        [Fact]
        public async Task CreateAsync_ThrowsValidationException_WhenInvalidDto()
        {
            // Arrange
            var dto = new ServiceCreateDto
            {
                ServiceName = "", // Nombre vacio - invalido
                Price = -100, // Precio negativo - invalido
                DurationMinutes = 0 // Duracion cero - invalido
            };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
        }

        #endregion

        #region DeactivateAsync Tests

        [Fact]
        public async Task DeactivateAsync_DeactivatesService_WhenNoAssociatedAppointments()
        {
            // Arrange
            var service = new Service { ServiceName = "Corte", Price = 10000, DurationMinutes = 30, IsActive = true };
            _db.Services.Add(service);
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.DeactivateAsync(service.ServiceId);

            // Assert
            Assert.True(result);
            var updatedService = await _db.Services.FindAsync(service.ServiceId);
            Assert.False(updatedService!.IsActive);
        }

        [Fact]
        public async Task DeactivateAsync_ThrowsException_WhenServiceHasAppointments()
        {
            // Arrange
            var service = new Service { ServiceName = "Corte", Price = 10000, DurationMinutes = 30, IsActive = true };
            _db.Services.Add(service);

            var client = new Clients { Name = "Test", Email = "test@test.com", Password = "123" };
            _db.Clients.Add(client);
            await _db.SaveChangesAsync();

            var appointment = new Appointment
            {
                AppointmentDate = DateTime.Now,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now.AddHours(1),
                ClientId = client.ClientId
            };
            _db.Appointments.Add(appointment);
            await _db.SaveChangesAsync();

            _db.AppointmentServices.Add(new Domain.Entities.AppointmentService
            {
                AppointmentId = appointment.AppointmentId,
                ServiceId = service.ServiceId
            });
            await _db.SaveChangesAsync();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeactivateAsync(service.ServiceId));
        }

        #endregion

        #region ReactivateAsync Tests

        [Fact]
        public async Task ReactivateAsync_ReactivatesService_WhenServiceIsInactive()
        {
            // Arrange
            var service = new Service { ServiceName = "Corte", Price = 10000, DurationMinutes = 30, IsActive = false };
            _db.Services.Add(service);
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.ReactivateAsync(service.ServiceId);

            // Assert
            Assert.True(result);
            var updatedService = await _db.Services.FindAsync(service.ServiceId);
            Assert.True(updatedService!.IsActive);
        }

        #endregion

        #region SearchByNameAsync Tests

        [Fact]
        public async Task SearchByNameAsync_ReturnsMatchingServices()
        {
            // Arrange
            _db.Services.AddRange(
                new Service { ServiceName = "Corte de Cabello", Price = 10000, DurationMinutes = 30, IsActive = true },
                new Service { ServiceName = "Tinte", Price = 25000, DurationMinutes = 60, IsActive = true },
                new Service { ServiceName = "Corte Barba", Price = 8000, DurationMinutes = 20, IsActive = true }
            );
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.SearchByNameAsync("Corte");

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.Contains("Corte", r.ServiceName));
        }

        [Fact]
        public async Task SearchByNameAsync_ReturnsEmptyList_WhenNoMatch()
        {
            // Arrange
            _db.Services.Add(new Service { ServiceName = "Tinte", Price = 25000, DurationMinutes = 60, IsActive = true });
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.SearchByNameAsync("Corte");

            // Assert
            Assert.Empty(result);
        }

        #endregion
    }
}
