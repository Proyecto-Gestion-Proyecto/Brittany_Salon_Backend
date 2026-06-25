using Brittany_Salon_Backend.Application.DTOs.Appointment;
using Brittany_Salon_Backend.Infrastructure.Logging;

namespace Brittany_Salon_Backend.Application.Validators
{
    public static class AppointmentValidator
    {
        public static List<string> ValidateCreate(AppointmentCreateDto dto, IDevLogger? logger = null)
        {
            var errors = new List<string>();

            logger?.LogDebug("Inicio validación de cita (create)");

            if (dto is null)
            {
                errors.Add("El cuerpo de la solicitud es obligatorio.");
                return errors;
            }

            errors.AddRange(ValidateClientId(dto.ClientId));
            errors.AddRange(ValidateStartTime(dto.StartTime));
            errors.AddRange(ValidateAppointmentDateMatchesStart(dto.AppointmentDate, dto.StartTime));
            errors.AddRange(ValidateNotPastDate(dto.StartTime));
            errors.AddRange(ValidateServiceList(dto.Services));
            errors.AddRange(ValidateProductList(dto.Products));
            errors.AddRange(ValidateHairLengthOptionRange(dto.HairLengthOption));
            errors.AddRange(ValidateStatus(dto.AppointmentStatus, allowNull: true));

            if (errors.Count > 0)
                logger?.LogWarning("Validación de cita (create) fallida: {Errors}", string.Join(", ", errors));

            return errors;
        }

        public static List<string> ValidateUpdate(AppointmentUpdateDto dto, IDevLogger? logger = null)
        {
            var errors = new List<string>();

            logger?.LogDebug("Inicio validación de cita (update)");

            if (dto is null)
            {
                errors.Add("El cuerpo de la solicitud es obligatorio.");
                return errors;
            }

            errors.AddRange(ValidateStartTime(dto.StartTime));
            errors.AddRange(ValidateNotPastDate(dto.StartTime));
            errors.AddRange(ValidateServiceList(dto.Services));
            errors.AddRange(ValidateProductList(dto.Products));
            errors.AddRange(ValidateHairLengthOptionRange(dto.HairLengthOption));

            if (errors.Count > 0)
                logger?.LogWarning("Validación de cita (update) fallida: {Errors}", string.Join(", ", errors));

            return errors;
        }

        public static List<string> ValidateClientId(int clientId)
        {
            var errors = new List<string>();

            if (clientId <= 0)
                errors.Add("ClientId inválido.");

            return errors;
        }

        public static List<string> ValidateStartTime(DateTime startTime)
        {
            var errors = new List<string>();

            if (startTime == default)
                errors.Add("StartTime inválido.");

            return errors;
        }

        public static List<string> ValidateAppointmentDateMatchesStart(DateTime appointmentDate, DateTime startTime)
        {
            var errors = new List<string>();

            if (appointmentDate == default || startTime == default)
                return errors;

            if (appointmentDate.Date != startTime.Date)
                errors.Add("AppointmentDate debe coincidir con la fecha de StartTime.");

            return errors;
        }
        private static DateTime GetNowCostaRica()
        {
            var utcNow = DateTime.UtcNow;

            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById("America/Costa_Rica");
                return TimeZoneInfo.ConvertTimeFromUtc(utcNow, tz);
            }
            catch
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(utcNow, tz);
            }
        }


        public static List<string> ValidateNotPastDate(DateTime startTime)
        {
            var errors = new List<string>();

            if (startTime == default)
                return errors;

            var now = GetNowCostaRica();

            if (startTime.Date < now.Date)
                errors.Add("No se puede agendar una cita en días anteriores.");

            if (startTime.Date == now.Date && startTime <= now)
                errors.Add("La hora de inicio debe ser futura.");

            return errors;
        }


        public static List<string> ValidateServiceList(List<AppointmentServiceCreateDto>? services)
        {
            var errors = new List<string>();

            if (services is null || services.Count == 0)
            {
                errors.Add("Debe seleccionar al menos un servicio.");
                return errors;
            }

            if (services.Any(s => s == null || s.ServiceId <= 0))
                errors.Add("La lista de servicios contiene elementos inválidos.");

            var duplicates = services
                .Where(s => s != null)
                .GroupBy(s => s.ServiceId)
                .Any(g => g.Key > 0 && g.Count() > 1);

            if (duplicates)
                errors.Add("No se permiten servicios duplicados.");

            return errors;
        }

        public static List<string> ValidateProductList(List<AppointmentProductCreateDto>? products)
        {
            var errors = new List<string>();

            if (products is null || products.Count == 0)
                return errors;

            if (products.Any(p => p == null || p.ProductId <= 0))
                errors.Add("La lista de productos contiene elementos inválidos.");

            // Permite productos duplicados
            //var duplicates = products
            //    .Where(p => p != null)
            //    .GroupBy(p => p.ProductId)
            //    .Any(g => g.Key > 0 && g.Count() > 1);

            //if (duplicates)
            //    errors.Add("No se permiten productos duplicados.");

            return errors;
        }

        public static List<string> ValidateHairLengthOptionRange(int? hairLengthOption)
        {
            var errors = new List<string>();

            if (!hairLengthOption.HasValue)
                return errors;

            var value = hairLengthOption.Value;

            if (value < 0 || value > 9)
                errors.Add("Opción de largo de pelo inválida.");

            return errors;
        }

        public static List<string> ValidateStatus(string? status, bool allowNull)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(status))
            {
                if (!allowNull)
                    errors.Add("El estado de la cita es obligatorio.");
                return errors;
            }

            if (status.Length > 50)
                errors.Add("El estado de la cita no puede exceder 50 caracteres.");

            var normalized = status.Trim().ToLowerInvariant();

            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "pendiente",
                "completada",
                "cancelada",
                "finalizada"
            };

            if (!allowed.Contains(normalized))
                errors.Add("El estado de la cita no es válido.");

            return errors;
        }
    }
}
