using Brittany_Salon_Backend.Application.DTOs.Employee;
using Brittany_Salon_Backend.Application.Exceptions;
using Brittany_Salon_Backend.Application.Services.Interfaces;
using Brittany_Salon_Backend.Application.Validators;
using Brittany_Salon_Backend.Domain.Entities;
using Brittany_Salon_Backend.Infrastructure.Logging;
using Brittany_Salon_Backend.Infrastructure.Persistence;
using Brittany_Salon_Backend.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace Brittany_Salon_Backend.Application.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly AppDbContext _db;
        private readonly IImageService _imageService;
        private readonly IDevLogger _logger;

        public EmployeeService(AppDbContext db, IImageService imageService, IDevLogger logger)
        {
            _db = db;
            _imageService = imageService;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene todos los empleados
        /// </summary>
        public async Task<List<EmployeeReadDto>> GetAllAsync(bool onlyActive = false)
        {
            _logger.LogInfo("Obteniendo empleados. Solo activos: {OnlyActive}", onlyActive);

            var query = _db.Employees.AsNoTracking();

            if (onlyActive)
                query = query.Where(e => e.IsActive);

            var employees = await query
                .OrderBy(e => e.Name)
                .Select(e => MapToReadDto(e))
                .ToListAsync();

            _logger.LogInfo("Se encontraron {Count} empleados", employees.Count);
            return employees;
        }

        /// <summary>
        /// Obtiene un empleado por su ID
        /// </summary>
        public async Task<EmployeeReadDto?> GetByIdAsync(int id)
        {
            _logger.LogInfo("Buscando empleado con ID: {Id}", id);

            var employee = await _db.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                _logger.LogWarning("Empleado con ID {Id} no encontrado", id);
                return null;
            }

            _logger.LogInfo("Empleado encontrado: {Name}", employee.Name);
            return MapToReadDto(employee);
        }

        /// <summary>
        /// Busca empleados por nombre (busqueda parcial)
        /// </summary>
        public async Task<List<EmployeeReadDto>> SearchByNameAsync(string name, bool onlyActive = false)
        {
            _logger.LogInfo("Buscando empleados por nombre: '{Name}', Solo activos: {OnlyActive}", name, onlyActive);

            var searchTerm = name.Trim().ToLower();

            var query = _db.Employees.AsNoTracking();

            if (onlyActive)
                query = query.Where(e => e.IsActive);

            var employees = await query
                .Where(e => e.Name.ToLower().Contains(searchTerm))
                .OrderBy(e => e.Name)
                .Select(e => MapToReadDto(e))
                .ToListAsync();

            _logger.LogInfo("Se encontraron {Count} empleados con nombre '{Name}'", employees.Count, name);
            return employees;
        }

        /// <summary>
        /// Busca empleados por especialidad (busqueda parcial)
        /// </summary>
        public async Task<List<EmployeeReadDto>> SearchBySpecialtyAsync(string specialty, bool onlyActive = false)
        {
            _logger.LogInfo("Buscando empleados por especialidad: '{Specialty}', Solo activos: {OnlyActive}", specialty, onlyActive);

            var searchTerm = specialty.Trim().ToLower();

            var query = _db.Employees.AsNoTracking();

            if (onlyActive)
                query = query.Where(e => e.IsActive);

            var employees = await query
                .Where(e => e.Specialty != null && e.Specialty.ToLower().Contains(searchTerm))
                .OrderBy(e => e.Name)
                .Select(e => MapToReadDto(e))
                .ToListAsync();

            _logger.LogInfo("Se encontraron {Count} empleados con especialidad '{Specialty}'", employees.Count, specialty);
            return employees;
        }

        public async Task<EmployeeReadDto> CreateAsync(EmployeeCreateDto dto)
        {
            _logger.LogInfo("Iniciando creacion de empleado: {Email}", dto.Email);

            var validationErrors = EmployeeValidator.ValidateCreate(dto, _logger);
            if (validationErrors.Count > 0)
                throw new ValidationException(validationErrors);

            var normalizedEmail = dto.Email.Trim().ToLower();
            var normalizedName = NormalizeName(dto.Name);
            var normalizedPhone = dto.Phone.Trim();

            await ValidateUniqueConstraintsAsync(normalizedEmail, normalizedPhone);

            var entity = new Employee
            {
                Name = normalizedName,
                Phone = normalizedPhone,
                Email = normalizedEmail,
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Specialty = dto.Specialty?.Trim(),
                IsActive = dto.IsActive ?? true,
                DateCreated = DateTime.Now
            };

            _db.Employees.Add(entity);
            await _db.SaveChangesAsync();

            if (dto.Image != null && dto.Image.Length > 0)
            {
                await ProcessEmployeeImageAsync(entity, dto.Image);
            }

            return MapToReadDto(entity);
        }

        /// <summary>
        /// Actualiza un empleado existente
        /// </summary>
        public async Task<bool> UpdateAsync(int id, EmployeeUpdateDto dto)
        {
            _logger.LogInfo("Iniciando actualizacion de empleado ID: {Id}", id);

            var entity = await _db.Employees.FindAsync(id);
            if (entity == null)
            {
                _logger.LogWarning("Empleado con ID {Id} no encontrado", id);
                return false;
            }

            var validationErrors = EmployeeValidator.ValidateUpdate(dto, _logger);
            if (validationErrors.Count > 0)
                throw new ValidationException(validationErrors);

            var newEmail = !string.IsNullOrWhiteSpace(dto.Email) ? dto.Email.Trim().ToLower() : null;
            var newPhone = !string.IsNullOrWhiteSpace(dto.Phone) ? dto.Phone.Trim() : null;
            await ValidateUniqueConstraintsForUpdateAsync(id, newEmail, newPhone);

            if (!string.IsNullOrWhiteSpace(dto.Name))
                entity.Name = NormalizeName(dto.Name);

            if (!string.IsNullOrWhiteSpace(dto.Email))
                entity.Email = newEmail!;

            if (!string.IsNullOrWhiteSpace(dto.Phone))
                entity.Phone = newPhone!;

            if (!string.IsNullOrWhiteSpace(dto.Password))
                entity.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            if (!string.IsNullOrWhiteSpace(dto.Specialty))
                entity.Specialty = dto.Specialty.Trim();

            if (dto.IsActive.HasValue)
                entity.IsActive = dto.IsActive.Value;

            if (dto.RemoveImage)
            {
                RemoveEmployeeImage(entity);
            }
            else if (dto.Image != null && dto.Image.Length > 0)
            {
                await UpdateEmployeeImageAsync(entity, dto.Image);
            }

            await _db.SaveChangesAsync();

            _logger.LogInfo("Empleado ID {Id} actualizado exitosamente", id);
            return true;
        }

        /// <summary>
        /// Desactiva un empleado (eliminacion logica)
        /// </summary>
        public async Task<bool> DeactivateAsync(int id)
        {
            _logger.LogInfo("Desactivando empleado ID: {Id}", id);

            var entity = await _db.Employees.FindAsync(id);
            if (entity == null)
            {
                _logger.LogWarning("Empleado con ID {Id} no encontrado", id);
                return false;
            }

            if (!entity.IsActive)
            {
                _logger.LogInfo("Empleado ID {Id} ya estaba desactivado", id);
                return true; // Ya esta desactivado, consideramos exito
            }

            entity.IsActive = false;
            await _db.SaveChangesAsync();

            _logger.LogInfo("Empleado ID {Id} desactivado exitosamente", id);
            return true;
        }

        /// <summary>
        /// Reactiva un empleado previamente desactivado
        /// </summary>
        public async Task<bool> ReactivateAsync(int id)
        {
            _logger.LogInfo("Reactivando empleado ID: {Id}", id);

            var entity = await _db.Employees.FindAsync(id);
            if (entity == null)
            {
                _logger.LogWarning("Empleado con ID {Id} no encontrado", id);
                return false;
            }

            if (entity.IsActive)
            {
                _logger.LogInfo("Empleado ID {Id} ya estaba activo", id);
                return true; // Ya esta activo, consideramos exito
            }

            entity.IsActive = true;
            await _db.SaveChangesAsync();

            _logger.LogInfo("Empleado ID {Id} reactivado exitosamente", id);
            return true;
        }

        /// <summary>
        /// Elimina permanentemente un empleado de la base de datos
        /// </summary>
        public async Task<bool> DeletePermanentlyAsync(int id)
        {
            _logger.LogInfo("Eliminando permanentemente empleado ID: {Id}", id);

            var entity = await _db.Employees.FindAsync(id);
            if (entity == null)
            {
                _logger.LogWarning("Empleado con ID {Id} no encontrado", id);
                return false;
            }

            // Eliminar imagen si existe
            if (!string.IsNullOrWhiteSpace(entity.Image))
            {
                _logger.LogInfo("Eliminando imagen del empleado: {Image}", entity.Image);
                _imageService.DeleteImage(entity.Image);
            }

            // Eliminar de la base de datos
            _db.Employees.Remove(entity);
            await _db.SaveChangesAsync();

            _logger.LogInfo("Empleado ID {Id} eliminado permanentemente", id);
            return true;
        }

        /// <summary>
        /// Valida que no existan duplicados en email y telefono
        /// </summary>
        private async Task ValidateUniqueConstraintsAsync(string email, string phone)
        {
            // Verificar email duplicado
            var emailExists = await _db.Employees.AnyAsync(x => x.Email == email);
            if (emailExists)
                throw new DuplicateResourceException("Email", "Ya existe un empleado registrado con este correo electronico.");

            // Verificar telefono duplicado
            var phoneExists = await _db.Employees.AnyAsync(x => x.Phone == phone);
            if (phoneExists)
                throw new DuplicateResourceException("Phone", "Ya existe un empleado registrado con este numero de telefono.");
        }

        /// <summary>
        /// Valida que email/telefono no esten duplicados (excluyendo el empleado actual)
        /// </summary>
        private async Task ValidateUniqueConstraintsForUpdateAsync(int employeeId, string? email, string? phone)
        {
            if (!string.IsNullOrWhiteSpace(email))
            {
                var emailExists = await _db.Employees.AnyAsync(x => x.Email == email && x.Id != employeeId);
                if (emailExists)
                    throw new DuplicateResourceException("Email", "Ya existe otro empleado con este correo electronico.");
            }

            if (!string.IsNullOrWhiteSpace(phone))
            {
                var phoneExists = await _db.Employees.AnyAsync(x => x.Phone == phone && x.Id != employeeId);
                if (phoneExists)
                    throw new DuplicateResourceException("Phone", "Ya existe otro empleado con este numero de telefono.");
            }
        }

        /// <summary>
        /// Normaliza el nombre: capitaliza primera letra de cada palabra
        /// </summary>
        private static string NormalizeName(string name)
        {
            var trimmed = name.Trim();
            var words = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var normalized = words.Select(word =>
                char.ToUpper(word[0]) + word[1..].ToLower()
            );

            return string.Join(" ", normalized);
        }

        /// <summary>
        /// Procesa y guarda la imagen del empleado (IFormFile)
        /// </summary>
        private async Task ProcessEmployeeImageAsync(Employee entity, IFormFile imageFile)
        {
            try
            {
                string imageUrl = await _imageService.ProcessAndSaveImageAsync(imageFile, "imageUser", entity.Id);
                entity.Image = imageUrl;
                await _db.SaveChangesAsync();
            }
            catch (ArgumentException ex)
            {
                throw new ValidationException($"Error al procesar la imagen: {ex.Message}");
            }
        }

        /// <summary>
        /// Actualiza la imagen del empleado (elimina la anterior si existe)
        /// </summary>
        private async Task UpdateEmployeeImageAsync(Employee entity, IFormFile newImage)
        {
            try
            {
                // Eliminar imagen anterior si existe
                if (!string.IsNullOrWhiteSpace(entity.Image))
                {
                    _logger.LogInfo("Eliminando imagen anterior: {Image}", entity.Image);
                    _imageService.DeleteImage(entity.Image);
                }

                // Guardar nueva imagen
                string imageUrl = await _imageService.ProcessAndSaveImageAsync(newImage, "imageUser", entity.Id);
                entity.Image = imageUrl;
                _logger.LogInfo("Nueva imagen guardada: {Image}", imageUrl);
            }
            catch (ArgumentException ex)
            {
                throw new ValidationException($"Error al procesar la imagen: {ex.Message}");
            }
        }

        /// <summary>
        /// Elimina la imagen del empleado
        /// </summary>
        private void RemoveEmployeeImage(Employee entity)
        {
            if (!string.IsNullOrWhiteSpace(entity.Image))
            {
                _logger.LogInfo("Eliminando imagen del empleado: {Image}", entity.Image);
                _imageService.DeleteImage(entity.Image);
                entity.Image = null;
            }
        }

        /// <summary>
        /// Mapea la entidad Employee a DTO de lectura
        /// </summary>
        private static EmployeeReadDto MapToReadDto(Employee entity)
        {
            return new EmployeeReadDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Phone = entity.Phone,
                Email = entity.Email,
                Image = entity.Image,
                Specialty = entity.Specialty,
                DateCreated = entity.DateCreated,
                IsActive = entity.IsActive
            };
        }
    }
}
