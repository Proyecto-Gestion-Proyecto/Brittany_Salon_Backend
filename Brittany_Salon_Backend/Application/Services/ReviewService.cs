using Brittany_Salon_Backend.Application.DTOs.Review;
using Brittany_Salon_Backend.Application.Exceptions;
using Brittany_Salon_Backend.Application.Services.Interfaces;
using Brittany_Salon_Backend.Application.Validators;
using Brittany_Salon_Backend.Domain.Entities;
using Brittany_Salon_Backend.Infrastructure.Logging;
using Brittany_Salon_Backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Brittany_Salon_Backend.Application.Services
{
    public class ReviewService : IReviewService
    {
        private readonly AppDbContext _db;
        private readonly IDevLogger _logger;

        public ReviewService(AppDbContext db, IDevLogger logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<ReviewReadDto>> GetAllAsync()
        {
            _logger.LogInfo("Obteniendo todas las reseñas");

            var reviews = await _db.Reviews
                .AsNoTracking()
                .Include(r => r.Client)
                .Include(r => r.Employee)
                .OrderByDescending(r => r.ReviewDate)
                .Select(r => MapToReadDto(r))
                .ToListAsync();

            _logger.LogInfo("Se encontraron {Count} reseñas", reviews.Count);
            return reviews;
        }
        public async Task<ReviewReadDto?> GetByIdAsync(int id)
        {
            _logger.LogInfo("Buscando reseña con ID: {Id}", id);

            var review = await _db.Reviews
                .AsNoTracking()
                .Include(r => r.Client)
                .Include(r => r.Employee)
                .FirstOrDefaultAsync(r => r.ReviewId == id);

            if (review == null)
            {
                _logger.LogWarning("Reseña con ID {Id} no encontrada", id);
                return null;
            }

            _logger.LogInfo("Reseña encontrada con ID {Id}", id);
            return MapToReadDto(review);
        }

        public async Task<List<ReviewReadDto>> GetByClientIdAsync(int clientId)
        {
            _logger.LogInfo("Obteniendo reseñas del cliente con ID: {ClientId}", clientId);

            var reviews = await _db.Reviews
                .AsNoTracking()
                .Where(r => r.ClientId == clientId)
                .Include(r => r.Client)
                .Include(r => r.Employee)
                .OrderByDescending(r => r.ReviewDate)
                .Select(r => MapToReadDto(r))
                .ToListAsync();

            _logger.LogInfo("Se encontraron {Count} reseñas para el cliente {ClientId}", reviews.Count, clientId);
            return reviews;
        }

        public async Task<ReviewReadDto?> GetByClientAndReviewIdAsync(int clientId, int reviewId)
        {
            _logger.LogInfo("Buscando reseña con ID {ReviewId} para cliente con ID {ClientId}", reviewId, clientId);

            var review = await _db.Reviews
                .AsNoTracking()
                .Include(r => r.Client)
                .Include(r => r.Employee)
                .FirstOrDefaultAsync(r => r.ReviewId == reviewId && r.ClientId == clientId);

            if (review == null)
            {
                _logger.LogWarning("Reseña con ID {ReviewId} no encontrada para cliente {ClientId}", reviewId, clientId);
                return null;
            }

            return MapToReadDto(review);
        }

        public async Task<List<ReviewReadDto>> GetByEmployeeIdAsync(int employeeId)
        {
            _logger.LogInfo("Obteniendo reseñas del empleado con ID: {EmployeeId}", employeeId);

            var reviews = await _db.Reviews
                .AsNoTracking()
                .Where(r => r.EmployeeId == employeeId)
                .Include(r => r.Client)
                .Include(r => r.Employee)
                .OrderByDescending(r => r.ReviewDate)
                .Select(r => MapToReadDto(r))
                .ToListAsync();

            _logger.LogInfo("Se encontraron {Count} reseñas para el empleado {EmployeeId}", reviews.Count, employeeId);
            return reviews;
        }

        public async Task<ReviewReadDto> CreateAsync(ReviewCreateDto dto)
        {
            _logger.LogInfo("Iniciando creación de reseña para cliente: {ClientId}", dto.ClientId);

            var validationErrors = ReviewValidator.ValidateCreate(dto, _logger);
            if (validationErrors.Count > 0)
            {
                _logger.LogWarning("Validación fallida con {ErrorCount} errores", validationErrors.Count);
                throw new ValidationException(validationErrors);
            }

            // Verificar que el cliente existe
            var clientExists = await _db.Clients.AnyAsync(c => c.ClientId == dto.ClientId);
            if (!clientExists)
            {
                _logger.LogWarning("Cliente con ID {ClientId} no existe", dto.ClientId);
                throw new Exception($"El cliente con ID {dto.ClientId} no existe");
            }

            // Verificar que el empleado existe
            var employeeExists = await _db.Employees.AnyAsync(e => e.Id == dto.EmployeeId);
            if (!employeeExists)
            {
                _logger.LogWarning("Empleado con ID {EmployeeId} no existe", dto.EmployeeId);
                throw new Exception($"El empleado con ID {dto.EmployeeId} no existe");
            }

            // Verificar que no exista una reseña previa del mismo cliente para el mismo empleado
            var existingReview = await _db.Reviews.FirstOrDefaultAsync(r => 
                r.ClientId == dto.ClientId && r.EmployeeId == dto.EmployeeId);
            if (existingReview != null)
            {
                _logger.LogWarning("Ya existe una reseña del cliente {ClientId} para el empleado {EmployeeId}", dto.ClientId, dto.EmployeeId);
                throw new Exception($"Ya existe una reseña de este cliente para este empleado");
            }

            var review = new Review
            {
                Comment = dto.Comment,
                Rating = dto.Rating,
                ClientId = dto.ClientId,
                EmployeeId = dto.EmployeeId,
                ReviewDate = DateTime.Now
            };

            _db.Reviews.Add(review);
            await _db.SaveChangesAsync();

            _logger.LogInfo("Reseña creada exitosamente con ID: {ReviewId}", review.ReviewId);

            return await GetByIdAsync(review.ReviewId) ?? throw new Exception("Error al recuperar la reseña creada");
        }

        public async Task<ReviewReadDto?> AddResponseAsync(int reviewId, ReviewResponseDto dto)
        {
            _logger.LogInfo("Iniciando adición de respuesta a reseña con ID: {ReviewId}", reviewId);

            var review = await _db.Reviews.FirstOrDefaultAsync(r => r.ReviewId == reviewId);
            if (review == null)
            {
                _logger.LogWarning("Reseña con ID {ReviewId} no encontrada", reviewId);
                return null;
            }

            // Verificar que no exista una respuesta previa
            if (!string.IsNullOrWhiteSpace(review.Response))
            {
                _logger.LogWarning("Reseña con ID {ReviewId} ya tiene una respuesta previa", reviewId);
                throw new Exception($"Esta reseña ya tiene una respuesta");
            }

            // Verificar que el empleado existe
            var employeeExists = await _db.Employees.AnyAsync(e => e.Id == dto.EmployeeId);
            if (!employeeExists)
            {
                _logger.LogWarning("Empleado con ID {EmployeeId} no existe", dto.EmployeeId);
                throw new Exception($"El empleado con ID {dto.EmployeeId} no existe");
            }

            review.Response = dto.Response;
            review.EmployeeId = dto.EmployeeId;

            _db.Reviews.Update(review);
            await _db.SaveChangesAsync();

            _logger.LogInfo("Respuesta agregada a reseña con ID {ReviewId} por empleado {EmployeeId}", reviewId, dto.EmployeeId);

            return await GetByIdAsync(reviewId);
        }

        public async Task<bool> DeleteResponseAsync(int reviewId)
        {
            _logger.LogInfo("Iniciando eliminación de respuesta de reseña con ID: {ReviewId}", reviewId);

            var review = await _db.Reviews.FirstOrDefaultAsync(r => r.ReviewId == reviewId);
            if (review == null)
            {
                _logger.LogWarning("Reseña con ID {ReviewId} no encontrada", reviewId);
                return false;
            }

            if (string.IsNullOrWhiteSpace(review.Response))
            {
                _logger.LogWarning("Reseña con ID {ReviewId} no tiene respuesta para eliminar", reviewId);
                return false;
            }

            review.Response = null;
            _db.Reviews.Update(review);
            await _db.SaveChangesAsync();

            _logger.LogInfo("Respuesta de reseña con ID {ReviewId} eliminada exitosamente", reviewId);
            return true;
        }

        public async Task<bool> UpdateAsync(int id, int clientId, ReviewUpdateDto dto)
        {
            _logger.LogInfo("Iniciando actualización de reseña con ID: {Id} para cliente: {ClientId}", id, clientId);

            var validationErrors = ReviewValidator.ValidateUpdate(dto, _logger);
            if (validationErrors.Count > 0)
            {
                _logger.LogWarning("Validación fallida con {ErrorCount} errores", validationErrors.Count);
                throw new ValidationException(validationErrors);
            }

            var review = await _db.Reviews.FirstOrDefaultAsync(r => r.ReviewId == id);
            if (review == null)
            {
                _logger.LogWarning("Reseña con ID {Id} no encontrada", id);
                return false;
            }

            // Validar que la reseña pertenezca al cliente
            if (review.ClientId != clientId)
            {
                _logger.LogWarning("Cliente {ClientId} intentó actualizar reseña que pertenece a cliente {OwnerClientId}", clientId, review.ClientId);
                throw new Exception($"No tienes permiso para actualizar esta reseña");
            }

            if (!string.IsNullOrWhiteSpace(dto.Comment))
                review.Comment = dto.Comment;

            if (dto.Rating.HasValue && dto.Rating.Value > 0 && dto.Rating.Value <= 5)
                review.Rating = dto.Rating.Value;

            if (!string.IsNullOrWhiteSpace(dto.Response))
                review.Response = dto.Response;

            _db.Reviews.Update(review);
            await _db.SaveChangesAsync();

            _logger.LogInfo("Reseña con ID {Id} actualizada exitosamente", id);
            return true;
        }

        public async Task<bool> DeleteAsync(int id, int clientId)
        {
            _logger.LogInfo("Iniciando eliminación de reseña con ID: {Id} para cliente: {ClientId}", id, clientId);

            var review = await _db.Reviews.FirstOrDefaultAsync(r => r.ReviewId == id);
            if (review == null)
            {
                _logger.LogWarning("Reseña con ID {Id} no encontrada", id);
                return false;
            }

            // Validar que la reseña pertenezca al cliente
            if (review.ClientId != clientId)
            {
                _logger.LogWarning("Cliente {ClientId} intentó eliminar reseña que pertenece a cliente {OwnerClientId}", clientId, review.ClientId);
                throw new Exception($"No tienes permiso para eliminar esta reseña");
            }

            _db.Reviews.Remove(review);
            await _db.SaveChangesAsync();

            _logger.LogInfo("Reseña con ID {Id} eliminada exitosamente", id);
            return true;
        }

        public async Task<double> GetAverageRatingByEmployeeAsync(int employeeId)
        {
            _logger.LogInfo("Calculando promedio de calificación para empleado: {EmployeeId}", employeeId);

            var averageRating = await _db.Reviews
                .Where(r => r.EmployeeId == employeeId)
                .AverageAsync(r => (double)r.Rating);

            _logger.LogInfo("Promedio de calificación del empleado {EmployeeId}: {AverageRating}", employeeId, averageRating);
            return averageRating;
        }

        private static ReviewReadDto MapToReadDto(Review review)
        {
            return new ReviewReadDto
            {
                ReviewId = review.ReviewId,
                Comment = review.Comment,
                Rating = review.Rating,
                ImageUrl = review.ImageUrl,
                Response = review.Response,
                ReviewDate = review.ReviewDate,
                ClientId = review.ClientId,
                EmployeeId = review.EmployeeId,
                ClientName = review.Client?.Name,
                EmployeeName = review.Employee?.Name,
                ClientImageUrl = review.Client?.ImageUrl,
                EmployeeImageUrl = review.Employee?.Image
            };
        }
    }
}
