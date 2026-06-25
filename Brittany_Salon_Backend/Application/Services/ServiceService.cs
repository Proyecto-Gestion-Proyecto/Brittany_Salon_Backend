using Brittany_Salon_Backend.Application.DTOs.Service;
using Brittany_Salon_Backend.Application.Services.Interfaces;
using Brittany_Salon_Backend.Domain.Entities;
using Brittany_Salon_Backend.Infrastructure.Persistence;
using Brittany_Salon_Backend.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using Brittany_Salon_Backend.Application.Validators;
using Brittany_Salon_Backend.Application.Exceptions;
using Brittany_Salon_Backend.Infrastructure.Logging;

namespace Brittany_Salon_Backend.Application.Services
{
    public class ServiceService : IServiceService
    {
        private readonly AppDbContext _db;
        private readonly IImageService _imageService;
        private readonly IDevLogger _logger;

        public ServiceService(AppDbContext db, IImageService imageService, IDevLogger logger)
        {
            _db = db;
            _imageService = imageService;
            _logger = logger;
        }

        public async Task<List<ServiceReadDto>> GetAllAsync(bool onlyActive = false)
        {
            var query = _db.Services.AsNoTracking();

            if (onlyActive)
                query = query.Where(s => s.IsActive);

            return await query
                .OrderBy(s => s.ServiceName)
                .Select(s => new ServiceReadDto
                {
                    ServiceId = s.ServiceId,
                    ServiceName = s.ServiceName,
                    ServiceDescription = s.ServiceDescription,
                    Price = s.Price,
                    DurationMinutes = s.DurationMinutes,
                    ImageUrl = s.ImageUrl,
                    ServiceType = s.ServiceType,
                    IsActive = s.IsActive
                })
                .ToListAsync();
        }

        public async Task<ServiceReadDto?> GetByIdAsync(int id)
        {
            var s = await _db.Services.AsNoTracking()
                .FirstOrDefaultAsync(x => x.ServiceId == id);

            if (s is null) return null;

            return new ServiceReadDto
            {
                ServiceId = s.ServiceId,
                ServiceName = s.ServiceName,
                ServiceDescription = s.ServiceDescription,
                Price = s.Price,
                DurationMinutes = s.DurationMinutes,
                ImageUrl = s.ImageUrl,
                ServiceType = s.ServiceType,
                IsActive = s.IsActive
            };
        }

        public async Task<ServiceReadDto> CreateAsync(ServiceCreateDto dto)
        {
            var validationErrors = ServiceValidator.ValidateCreate(dto, _logger);
            if (validationErrors.Count > 0)
                throw new ValidationException(validationErrors);

            var entity = new Service
            {
                ServiceName = dto.ServiceName.Trim(),
                ServiceDescription = dto.ServiceDescription?.Trim(),
                Price = dto.Price,
                DurationMinutes = dto.DurationMinutes,
                ImageUrl = null,
                ServiceType = dto.ServiceType?.Trim(),
                IsActive = dto.IsActive ?? true
            };

            _db.Services.Add(entity);
            await _db.SaveChangesAsync();

            if (dto.Image != null && dto.Image.Length > 0)
            {
                await ProcessServiceImageAsync(entity, dto.Image);
            }

            return MapToReadDto(entity);
        }

        public async Task<bool> UpdateAsync(int id, ServiceUpdateDto dto)
        {
            var entity = await _db.Services.FindAsync(id);
            if (entity is null) return false;

            var validationErrors = ServiceValidator.ValidateUpdate(dto, _logger);
            if (validationErrors.Count > 0)
                throw new ValidationException(validationErrors);

            entity.ServiceName = dto.ServiceName.Trim();
            entity.ServiceDescription = dto.ServiceDescription?.Trim();
            entity.Price = dto.Price;
            entity.DurationMinutes = dto.DurationMinutes;
            entity.ServiceType = dto.ServiceType?.Trim();
            entity.IsActive = dto.IsActive;

            if (dto.Image != null && dto.Image.Length > 0)
            {
                await UpdateServiceImageAsync(entity, dto.Image);
            }

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeactivateAsync(int id)
        {
            var entity = await _db.Services.FirstOrDefaultAsync(x => x.ServiceId == id);
            if (entity is null) return false;

            var hasAppointments = await HasAssociatedAppointmentsAsync(id);
            if (hasAppointments)
                throw new InvalidOperationException("No se puede desactivar el servicio porque está asociado a una o más citas.");

            entity.IsActive = false;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateImageUrlAsync(int serviceId, string imageUrl)
        {
            var service = await _db.Services.FindAsync(serviceId);
            if (service == null) return false;

            service.ImageUrl = imageUrl.Trim();
            await _db.SaveChangesAsync();
            return true;
        }

        private static ServiceReadDto MapToReadDto(Service entity)
        {
            return new ServiceReadDto
            {
                ServiceId = entity.ServiceId,
                ServiceName = entity.ServiceName,
                ServiceDescription = entity.ServiceDescription,
                Price = entity.Price,
                DurationMinutes = entity.DurationMinutes,
                ImageUrl = entity.ImageUrl,
                ServiceType = entity.ServiceType,
                IsActive = entity.IsActive
            };
        }

        private async Task ProcessServiceImageAsync(Service entity, IFormFile imageFile)
        {
            string imageUrl = await _imageService.ProcessAndSaveImageAsync(imageFile, "imageService", entity.ServiceId);
            entity.ImageUrl = imageUrl;
            await _db.SaveChangesAsync();
        }

        private async Task UpdateServiceImageAsync(Service entity, IFormFile newImage)
        {
            if (!string.IsNullOrWhiteSpace(entity.ImageUrl))
            {
                _imageService.DeleteImage(entity.ImageUrl);
            }

            string imageUrl = await _imageService.ProcessAndSaveImageAsync(newImage, "imageService", entity.ServiceId);
            entity.ImageUrl = imageUrl;
        }

        public async Task<List<ServiceReadDto>> SearchByNameAsync(string name, bool onlyActive = false)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new List<ServiceReadDto>();

            var searchTerm = name.Trim().ToLower();

            var query = _db.Services.AsNoTracking();

            if (onlyActive)
                query = query.Where(s => s.IsActive);

            return await query
                .Where(s => s.ServiceName.ToLower().Contains(searchTerm))
                .OrderBy(s => s.ServiceName)
                .Select(s => new ServiceReadDto
                {
                    ServiceId = s.ServiceId,
                    ServiceName = s.ServiceName,
                    ServiceDescription = s.ServiceDescription,
                    Price = s.Price,
                    DurationMinutes = s.DurationMinutes,
                    ImageUrl = s.ImageUrl,
                    ServiceType = s.ServiceType,
                    IsActive = s.IsActive
                })
                .ToListAsync();
        }

        public async Task<bool> ReactivateAsync(int id)
        {
            var entity = await _db.Services.FindAsync(id);
            if (entity == null) return false;

            if (entity.IsActive) return true;

            entity.IsActive = true;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeletePermanentlyAsync(int id)
        {
            var entity = await _db.Services.FindAsync(id);
            if (entity == null) return false;

            var hasAppointments = await HasAssociatedAppointmentsAsync(id);
            if (hasAppointments)
                throw new InvalidOperationException("No se puede eliminar el servicio porque está asociado a una o más citas.");

            if (!string.IsNullOrWhiteSpace(entity.ImageUrl))
            {
                _imageService.DeleteImage(entity.ImageUrl);
            }

            _db.Services.Remove(entity);
            await _db.SaveChangesAsync();
            return true;
        }
        private async Task<bool> HasAssociatedAppointmentsAsync(int serviceId)
        {
            return await _db.AppointmentServices
                .AsNoTracking()
                .AnyAsync(x => x.ServiceId == serviceId);
        }
        public async Task<List<FeaturedServiceReadDto>> GetFeaturedAsync(int top = 5, bool onlyActive = true)
        {
            if (top <= 0) top = 5;
            if (top > 50) top = 50;

            var query =
                from aps in _db.AppointmentServices.AsNoTracking()
                join s in _db.Services.AsNoTracking() on aps.ServiceId equals s.ServiceId
                where !onlyActive || s.IsActive
                group s by new
                {
                    s.ServiceId,
                    s.ServiceName,
                    s.ServiceDescription,
                    s.Price,
                    s.DurationMinutes,
                    s.ImageUrl,
                    s.ServiceType,
                    s.IsActive
                }
                into g
                orderby g.Count() descending
                select new FeaturedServiceReadDto
                {
                    ServiceId = g.Key.ServiceId,
                    ServiceName = g.Key.ServiceName,
                    ServiceDescription = g.Key.ServiceDescription,
                    Price = g.Key.Price,
                    DurationMinutes = g.Key.DurationMinutes,
                    ImageUrl = g.Key.ImageUrl,
                    ServiceType = g.Key.ServiceType,
                    IsActive = g.Key.IsActive,
                    RequestsCount = g.Count()
                };

            return await query.Take(top).ToListAsync();
        }

    }
}
