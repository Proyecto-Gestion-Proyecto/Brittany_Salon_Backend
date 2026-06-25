using Brittany_Salon_Backend.Application.DTOs.Payment;
using Brittany_Salon_Backend.Application.Services.Interfaces;
using Brittany_Salon_Backend.Domain.Entities;
using Brittany_Salon_Backend.Infrastructure.Logging;
using Brittany_Salon_Backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Brittany_Salon_Backend.Application.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly AppDbContext _db;
        private readonly IDevLogger _logger;

        public PaymentService(AppDbContext db, IDevLogger logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<PaymentReadDto>> GetAllAsync()
        {
            _logger.LogInfo("Obteniendo todos los pagos");

            var payments = await _db.Payments
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => MapToReadDto(p))
                .ToListAsync();

            _logger.LogInfo("Se encontraron {Count} pagos", payments.Count);
            return payments;
        }

        public async Task<List<PaymentReadDto>> GetAllWithInactiveAsync()
        {
            _logger.LogInfo("Obteniendo todos los pagos incluyendo inactivos");

            var payments = await _db.Payments
                .AsNoTracking()
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => MapToReadDto(p))
                .ToListAsync();

            _logger.LogInfo("Se encontraron {Count} pagos", payments.Count);
            return payments;
        }

        public async Task<PaymentReadDto?> GetByIdAsync(int id)
        {
            _logger.LogInfo("Buscando pago con ID: {Id}", id);

            var payment = await _db.Payments
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PaymentId == id && p.IsActive);

            if (payment == null)
            {
                _logger.LogWarning("Pago con ID {Id} no encontrado", id);
                return null;
            }

            _logger.LogInfo("Pago encontrado");
            return MapToReadDto(payment);
        }

        public async Task<List<PaymentReadDto>> GetByAppointmentIdAsync(int appointmentId)
        {
            _logger.LogInfo("Buscando pagos para cita ID: {AppointmentId}", appointmentId);

            var payments = await _db.Payments
                .AsNoTracking()
                .Where(p => p.AppointmentId == appointmentId && p.IsActive)
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => MapToReadDto(p))
                .ToListAsync();

            _logger.LogInfo("Se encontraron {Count} pagos para la cita", payments.Count);
            return payments;
        }

        public async Task<List<PaymentReadDto>> GetByClientIdAsync(int clientId)
        {
            _logger.LogInfo("Buscando pagos para cliente ID: {ClientId}", clientId);

            var payments = await _db.Payments
                .AsNoTracking()
                .Where(p => p.Appointment.ClientId == clientId && p.IsActive)
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => MapToReadDto(p))
                .ToListAsync();

            _logger.LogInfo("Se encontraron {Count} pagos para el cliente", payments.Count);
            return payments;
        }

        public async Task<List<PaymentReadDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            _logger.LogInfo("Buscando pagos en rango de fechas: {StartDate} a {EndDate}", startDate, endDate);

            var payments = await _db.Payments
                .AsNoTracking()
                .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate)
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => MapToReadDto(p))
                .ToListAsync();

            _logger.LogInfo("Se encontraron {Count} pagos en el rango", payments.Count);
            return payments;
        }

        public async Task<List<PaymentReadDto>> GetByStatusAsync(bool isActive)
        {
            _logger.LogInfo("Buscando pagos con estado activo: {IsActive}", isActive);

            var payments = await _db.Payments
                .AsNoTracking()
                .Where(p => p.IsActive == isActive)
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => MapToReadDto(p))
                .ToListAsync();

            _logger.LogInfo("Se encontraron {Count} pagos con el estado", payments.Count);
            return payments;
        }

        public async Task<PaymentReadDto> CreateAsync(PaymentCreateDto dto)
        {
            _logger.LogInfo("Iniciando creación de pago para cita ID: {AppointmentId}", dto.AppointmentId);

            // Validar que la cita existe
            var appointment = await _db.Appointments.FindAsync(dto.AppointmentId);
            if (appointment == null)
            {
                throw new ArgumentException("La cita especificada no existe.");
            }

            // Crear entidad
            var entity = new Payment
            {
                AppointmentId = dto.AppointmentId,
                Amount = dto.Amount,
                PaymentDate = DateTime.Now,
                PaymentMethod = dto.PaymentMethod,
                PaymentStatus = dto.PaymentStatus,
                IsActive = dto.IsActive ?? true
            };

            _db.Payments.Add(entity);
            await _db.SaveChangesAsync();

            _logger.LogInfo("Pago creado exitosamente con ID: {Id}", entity.PaymentId);
            return MapToReadDto(entity);
        }

        public async Task<PaymentReadDto> CreateAndReduceBalanceAsync(PaymentCreateDto dto)
        {
            _logger.LogInfo("Iniciando creación de pago y reducción de saldo para cita ID: {AppointmentId}", dto.AppointmentId);

            // Validar que la cita existe
            var appointment = await _db.Appointments
                .Include(a => a.Client)
                .FirstOrDefaultAsync(a => a.AppointmentId == dto.AppointmentId);
            if (appointment == null)
            {
                throw new ArgumentException("La cita especificada no existe.");
            }

            // Crear entidad
            var entity = new Payment
            {
                AppointmentId = dto.AppointmentId,
                Amount = dto.Amount,
                PaymentDate = DateTime.Now,
                PaymentMethod = dto.PaymentMethod,
                PaymentStatus = dto.PaymentStatus,
                IsActive = dto.IsActive ?? true
            };

            _db.Payments.Add(entity);

            // Reducir el saldo pendiente del cliente
            appointment.Client.PendingBalance -= dto.Amount;

            await _db.SaveChangesAsync();

            _logger.LogInfo("Pago creado y saldo reducido exitosamente con ID: {Id}", entity.PaymentId);
            return MapToReadDto(entity);
        }

        public async Task<bool> UpdateAsync(int id, PaymentUpdateDto dto)
        {
            _logger.LogInfo("Iniciando actualización de pago ID: {Id}", id);

            var entity = await _db.Payments.FindAsync(id);
            if (entity == null || !entity.IsActive)
            {
                _logger.LogWarning("Pago con ID {Id} no encontrado", id);
                return false;
            }

            // Validar datos modificados
            if (dto.Amount.HasValue && dto.Amount.Value <= 0)
            {
                throw new ArgumentException("El monto debe ser mayor a 0.");
            }

            if (!string.IsNullOrWhiteSpace(dto.PaymentMethod) && dto.PaymentMethod != "Efectivo" && dto.PaymentMethod != "SINPE")
            {
                throw new ArgumentException("El método de pago debe ser 'Efectivo' o 'SINPE'.");
            }

            if (dto.Notes != null && dto.Notes.Length > 255)
            {
                throw new ArgumentException("Las notas no pueden exceder 255 caracteres.");
            }

            // Actualizar campos
            if (dto.Amount.HasValue)
                entity.Amount = dto.Amount.Value;

            if (!string.IsNullOrWhiteSpace(dto.PaymentMethod))
                entity.PaymentMethod = dto.PaymentMethod;

            if (dto.PaymentStatus != null)
                entity.PaymentStatus = dto.PaymentStatus;

            if (dto.IsActive.HasValue)
                entity.IsActive = dto.IsActive.Value;

            await _db.SaveChangesAsync();

            _logger.LogInfo("Pago {Id} actualizado exitosamente", id);
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            _logger.LogInfo("Desactivando pago ID: {Id}", id);

            var payment = await _db.Payments.FindAsync(id);
            if (payment == null)
            {
                _logger.LogWarning("Pago con ID {Id} no encontrado", id);
                return false;
            }

            payment.IsActive = false;
            await _db.SaveChangesAsync();

            _logger.LogInfo("Pago {Id} desactivado exitosamente", id);
            return true;
        }

        private static PaymentReadDto MapToReadDto(Payment payment)
        {
            return new PaymentReadDto
            {
                PaymentId = payment.PaymentId,
                AppointmentId = payment.AppointmentId,
                Amount = payment.Amount,
                PaymentDate = payment.PaymentDate,
                PaymentMethod = payment.PaymentMethod,
                PaymentStatus = payment.PaymentStatus,
                IsActive = payment.IsActive
            };
        }
    }
}