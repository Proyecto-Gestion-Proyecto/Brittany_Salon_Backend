using Brittany_Salon_Backend.Application.DTOs.Review;
using Brittany_Salon_Backend.Application.Exceptions;
using Brittany_Salon_Backend.Application.Services;
using Brittany_Salon_Backend.Domain.Entities;
using Brittany_Salon_Backend.Infrastructure.Logging;
using Brittany_Salon_Backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;

namespace Brittany_Salon_Backend.Tests.Services
{
    public class ReviewServiceTests : IDisposable
    {
        private readonly AppDbContext _db;
        private readonly Mock<IDevLogger> _loggerMock;
        private readonly ReviewService _service;

        public ReviewServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _db = new AppDbContext(options);
            _loggerMock = new Mock<IDevLogger>();
            _service = new ReviewService(_db, _loggerMock.Object);
        }

        public void Dispose()
        {
            _db.Database.EnsureDeleted();
            _db.Dispose();
        }

        private async Task<Clients> CreateTestClient()
        {
            var client = new Clients
            {
                Name = "Cliente Test",
                Email = "cliente@test.com",
                Phone = "88887777",
                Password = "Password123!",
                IsActive = true
            };
            _db.Clients.Add(client);
            await _db.SaveChangesAsync();
            return client;
        }

        private async Task<Employee> CreateTestEmployee()
        {
            var employee = new Employee
            {
                Name = "Empleado Test",
                Email = "empleado@test.com",
                Phone = "88881111",
                Password = "Password123!",
                Specialty = "Estilista",
                IsActive = true
            };
            _db.Employees.Add(employee);
            await _db.SaveChangesAsync();
            return employee;
        }

        #region GetAllAsync Tests

        [Fact]
        public async Task GetAllAsync_ReturnsAllReviews()
        {
            // Arrange
            var client = await CreateTestClient();
            var employee = await CreateTestEmployee();

            _db.Reviews.AddRange(
                new Review { Comment = "Excelente servicio", Rating = 5, ClientId = client.ClientId, EmployeeId = employee.Id, ReviewDate = DateTime.Now },
                new Review { Comment = "Buen servicio", Rating = 4, ClientId = client.ClientId, EmployeeId = employee.Id, ReviewDate = DateTime.Now.AddDays(-1) }
            );
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsReviewsOrderedByDateDescending()
        {
            // Arrange
            var client = await CreateTestClient();
            var employee = await CreateTestEmployee();

            _db.Reviews.AddRange(
                new Review { Comment = "Antigua", Rating = 3, ClientId = client.ClientId, EmployeeId = employee.Id, ReviewDate = DateTime.Now.AddDays(-5) },
                new Review { Comment = "Reciente", Rating = 5, ClientId = client.ClientId, EmployeeId = employee.Id, ReviewDate = DateTime.Now }
            );
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            Assert.Equal("Reciente", result[0].Comment);
            Assert.Equal("Antigua", result[1].Comment);
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_ReturnsReview_WhenExists()
        {
            // Arrange
            var client = await CreateTestClient();
            var employee = await CreateTestEmployee();

            var review = new Review
            {
                Comment = "Muy buen servicio",
                Rating = 5,
                ClientId = client.ClientId,
                EmployeeId = employee.Id,
                ReviewDate = DateTime.Now
            };
            _db.Reviews.Add(review);
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.GetByIdAsync(review.ReviewId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Muy buen servicio", result.Comment);
            Assert.Equal(5, result.Rating);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
        {
            // Act
            var result = await _service.GetByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_CreatesReview_WhenValidDto()
        {
            // Arrange
            var client = await CreateTestClient();
            var employee = await CreateTestEmployee();

            var dto = new ReviewCreateDto
            {
                Comment = "Servicio excelente",
                Rating = 5,
                ClientId = client.ClientId,
                EmployeeId = employee.Id
            };

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Servicio excelente", result.Comment);
            Assert.Equal(5, result.Rating);
            Assert.Equal(client.ClientId, result.ClientId);

            var dbReview = await _db.Reviews.FirstOrDefaultAsync();
            Assert.NotNull(dbReview);
        }

        [Fact]
        public async Task CreateAsync_ThrowsException_WhenClientDoesNotExist()
        {
            // Arrange
            var dto = new ReviewCreateDto
            {
                Comment = "Buen servicio",
                Rating = 4,
                ClientId = 99999,
                EmployeeId = 1
            };

            // Act & Assert
            await Assert.ThrowsAnyAsync<Exception>(() => _service.CreateAsync(dto));
        }

        #endregion

        #region GetByClientIdAsync Tests

        [Fact]
        public async Task GetByClientIdAsync_ReturnsClientReviews()
        {
            // Arrange
            var client1 = await CreateTestClient();
            var client2 = new Clients { Name = "Otro Cliente", Email = "otro@test.com", Password = "123", IsActive = true };
            _db.Clients.Add(client2);
            var employee = await CreateTestEmployee();

            _db.Reviews.AddRange(
                new Review { Comment = "Review Cliente 1", Rating = 5, ClientId = client1.ClientId, EmployeeId = employee.Id, ReviewDate = DateTime.Now },
                new Review { Comment = "Review Cliente 2", Rating = 4, ClientId = client2.ClientId, EmployeeId = employee.Id, ReviewDate = DateTime.Now }
            );
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.GetByClientIdAsync(client1.ClientId);

            // Assert
            Assert.Single(result);
            Assert.Equal("Review Cliente 1", result[0].Comment);
        }

        #endregion

        #region AddResponseAsync Tests

        [Fact]
        public async Task AddResponseAsync_AddsResponse_WhenReviewExists()
        {
            // Arrange
            var client = await CreateTestClient();
            var employee = await CreateTestEmployee();

            var review = new Review
            {
                Comment = "Buen servicio",
                Rating = 4,
                ClientId = client.ClientId,
                EmployeeId = employee.Id,
                ReviewDate = DateTime.Now
            };
            _db.Reviews.Add(review);
            await _db.SaveChangesAsync();

            var responseDto = new ReviewResponseDto
            {
                Response = "Gracias por tu comentario!",
                EmployeeId = employee.Id
            };

            // Act
            var result = await _service.AddResponseAsync(review.ReviewId, responseDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Gracias por tu comentario!", result.Response);
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_DeletesReview_WhenOwnerDeletes()
        {
            // Arrange
            var client = await CreateTestClient();
            var employee = await CreateTestEmployee();

            var review = new Review
            {
                Comment = "Para eliminar",
                Rating = 3,
                ClientId = client.ClientId,
                EmployeeId = employee.Id,
                ReviewDate = DateTime.Now
            };
            _db.Reviews.Add(review);
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.DeleteAsync(review.ReviewId, client.ClientId);

            // Assert
            Assert.True(result);
            var dbReview = await _db.Reviews.FindAsync(review.ReviewId);
            Assert.Null(dbReview);
        }

        [Fact]
        public async Task DeleteAsync_ThrowsException_WhenNotOwner()
        {
            // Arrange
            var client = await CreateTestClient();
            var otherClient = new Clients { Name = "Otro", Email = "otro@email.com", Password = "123", IsActive = true };
            _db.Clients.Add(otherClient);
            var employee = await CreateTestEmployee();

            var review = new Review
            {
                Comment = "Mi resena",
                Rating = 5,
                ClientId = client.ClientId,
                EmployeeId = employee.Id,
                ReviewDate = DateTime.Now
            };
            _db.Reviews.Add(review);
            await _db.SaveChangesAsync();

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.DeleteAsync(review.ReviewId, otherClient.ClientId));
        }

        #endregion
    }
}
