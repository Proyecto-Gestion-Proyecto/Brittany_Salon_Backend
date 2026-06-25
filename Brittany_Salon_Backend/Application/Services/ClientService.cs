using Brittany_Salon_Backend.Application.DTOs.Client;
using Brittany_Salon_Backend.Application.Exceptions;
using Brittany_Salon_Backend.Application.Services.Interfaces;
using Brittany_Salon_Backend.Application.Validators;
using Brittany_Salon_Backend.Domain.Entities;
using Brittany_Salon_Backend.Infrastructure.Logging;
using Brittany_Salon_Backend.Infrastructure.Persistence;
using Brittany_Salon_Backend.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace Brittany_Salon_Backend.Application.Services
{
    public class ClientService : IClientService
    {
        private readonly AppDbContext _db;
        private readonly IImageService _imageService;
        private readonly IDevLogger _logger;

        public ClientService(AppDbContext db, IImageService imageService, IDevLogger logger)
        {
            _db = db;
            _imageService = imageService;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene todos los clientes
        /// </summary>
        public async Task<List<ClientReadDto>> GetAllAsync(bool onlyActive = false)
        {
            _logger.LogInfo("Obteniendo clientes. Solo activos: {OnlyActive}", onlyActive);

            var query = _db.Clients.AsNoTracking();

            if (onlyActive)
                query = query.Where(c => c.IsActive);

            var clients = await query
                .OrderBy(c => c.Name)
                .Select(c => MapToReadDto(c))
                .ToListAsync();

            _logger.LogInfo("Se encontraron {Count} clientes", clients.Count);
            return clients;
        }

        /// <summary>
        /// Obtiene un cliente por su ID
        /// </summary>
        public async Task<ClientReadDto?> GetByIdAsync(int id)
        {
            _logger.LogInfo("Buscando cliente con ID: {Id}", id);

            var client = await _db.Clients
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ClientId == id);

            if (client == null)
            {
                _logger.LogWarning("Cliente con ID {Id} no encontrado", id);
                return null;
            }

            _logger.LogInfo("Cliente encontrado: {Name}", client.Name);
            return MapToReadDto(client);
        }

        /// <summary>
        /// Busca clientes por nombre (busqueda parcial)
        /// </summary>
        public async Task<List<ClientReadDto>> SearchByNameAsync(string name, bool onlyActive = false)
        {
            _logger.LogInfo("Buscando clientes por nombre: '{Name}', Solo activos: {OnlyActive}", name, onlyActive);

            var searchTerm = name.Trim().ToLower();

            var query = _db.Clients.AsNoTracking();

            if (onlyActive)
                query = query.Where(c => c.IsActive);

            var clients = await query
                .Where(c => c.Name.ToLower().Contains(searchTerm))
                .OrderBy(c => c.Name)
                .Select(c => MapToReadDto(c))
                .ToListAsync();

            _logger.LogInfo("Se encontraron {Count} clientes con nombre '{Name}'", clients.Count, name);
            return clients;
        }

        /// <summary>
        /// Crea un nuevo cliente
        /// </summary>
        public async Task<ClientReadDto> CreateAsync(ClientCreateDto dto)
        {
            _logger.LogInfo("Iniciando creacion de cliente: {Email}", dto.Email);

            var validationErrors = ClientValidator.ValidateCreate(dto, _logger);
            if (validationErrors.Count > 0)
                throw new ValidationException(validationErrors);

            var normalizedEmail = dto.Email.Trim().ToLower();
            var normalizedName = NormalizeName(dto.Name);
            var normalizedPhone = dto.Phone?.Trim() ?? string.Empty;

            await ValidateUniqueConstraintsAsync(normalizedEmail);

            var entity = new Clients
            {
                Name = normalizedName,
                Phone = normalizedPhone,
                Email = normalizedEmail,
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                IsActive = dto.IsActive ?? true,
                CreatedAt = DateTime.Now
            };

            _db.Clients.Add(entity);
            await _db.SaveChangesAsync();

            if (dto.Image != null && dto.Image.Length > 0)
            {
                await ProcessClientImageAsync(entity, dto.Image);
            }

            return MapToReadDto(entity);
        }

        /// <summary>
        /// Actualiza un cliente existente
        /// </summary>
        public async Task<bool> UpdateAsync(int id, ClientUpdateDto dto)
        {
            _logger.LogInfo("Iniciando actualizacion de cliente ID: {Id}", id);

            var entity = await _db.Clients.FindAsync(id);
            if (entity == null)
            {
                _logger.LogWarning("Cliente con ID {Id} no encontrado", id);
                return false;
            }

            var validationErrors = ClientValidator.ValidateUpdate(dto, _logger);
            if (validationErrors.Count > 0)
                throw new ValidationException(validationErrors);

            var newEmail = !string.IsNullOrWhiteSpace(dto.Email) ? dto.Email.Trim().ToLower() : null;
            await ValidateUniqueConstraintsForUpdateAsync(id, newEmail);

            if (!string.IsNullOrWhiteSpace(dto.Name))
                entity.Name = NormalizeName(dto.Name);

            if (!string.IsNullOrWhiteSpace(dto.Email))
                entity.Email = newEmail!;

            if (!string.IsNullOrWhiteSpace(dto.Phone))
                entity.Phone = dto.Phone.Trim();

            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                entity.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            }

            if (dto.Image != null && dto.Image.Length > 0)
            {
                await ProcessClientImageAsync(entity, dto.Image);
            }

            await _db.SaveChangesAsync();

            _logger.LogInfo("Cliente {Id} actualizado exitosamente", id);
            return true;
        }

        /// <summary>
        /// Desactiva un cliente (eliminaci�n l�gica)
        /// </summary>
        public async Task<bool> DeactivateAsync(int id)
        {
            _logger.LogInfo("Desactivando cliente ID: {Id}", id);

            var client = await _db.Clients.FindAsync(id);
            if (client == null)
            {
                _logger.LogWarning("Cliente con ID {Id} no encontrado", id);
                return false;
            }

            client.IsActive = false;
            await _db.SaveChangesAsync();

            _logger.LogInfo("Cliente {Id} desactivado exitosamente", id);
            return true;
        }

        /// <summary>
        /// Valida restricciones �nicas para creaci�n
        /// </summary>
        private async Task ValidateUniqueConstraintsAsync(string email)
        {
            // Validar email �nico
            var existingEmail = await _db.Clients
                .AnyAsync(c => c.Email == email);

            if (existingEmail)
                throw new DuplicateResourceException("email", "El correo electr�nico ya est� registrado.");
        }

        /// <summary>
        /// Valida restricciones �nicas para actualizaci�n
        /// </summary>
        private async Task ValidateUniqueConstraintsForUpdateAsync(int clientId, string? newEmail)
        {
            if (!string.IsNullOrWhiteSpace(newEmail))
            {
                var existingEmail = await _db.Clients
                    .AnyAsync(c => c.Email == newEmail && c.ClientId != clientId);

                if (existingEmail)
                    throw new DuplicateResourceException("email", "El correo electr�nico ya est� registrado por otro cliente.");
            }
        }

        /// <summary>
        /// Procesa la imagen del cliente
        /// </summary>
        private async Task ProcessClientImageAsync(Clients client, IFormFile image)
        {
            try
            {
                _logger.LogInfo("Procesando imagen para cliente {Id}", client.ClientId);

                var imageUrl = await _imageService.ProcessAndSaveImageAsync(image, $"clients", client.ClientId);
                client.ImageUrl = imageUrl;

                await _db.SaveChangesAsync();

                _logger.LogInfo("Imagen procesada exitosamente para cliente {Id}: {ImageUrl}", client.ClientId, imageUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error procesando imagen para cliente {Id}", ex, client.ClientId);
                // No lanzamos excepci�n para no fallar la creaci�n del cliente
            }
        }

        /// <summary>
        /// Normaliza el nombre (capitaliza primera letra de cada palabra)
        /// </summary>
        private static string NormalizeName(string name)
        {
            return string.Join(" ", name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(word => char.ToUpper(word[0]) + word[1..].ToLower()));
        }

        /// <summary>
        /// Mapea entidad a DTO de lectura
        /// </summary>
        private static ClientReadDto MapToReadDto(Clients client)
        {
            return new ClientReadDto
            {
                ClientId = client.ClientId,
                Name = client.Name,
                Email = client.Email,
                Phone = client.Phone,
                PendingBalance = client.PendingBalance,
                ImageUrl = client.ImageUrl,
                IsActive = client.IsActive,
                CreatedAt = client.CreatedAt
            };
        }

        public async Task<bool> UpdateBalanceAsync(int clientId, decimal pendingBalance)
        {
            var client = await _db.Clients.FindAsync(clientId);
            if (client == null) return false;

            client.PendingBalance = pendingBalance;
            await _db.SaveChangesAsync();
            return true;
        }
    }


}