using Brittany_Salon_Backend.Application.DTOs.Service;
using Brittany_Salon_Backend.Infrastructure.Logging;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;

namespace Brittany_Salon_Backend.Application.Validators
{
    public static partial class ServiceValidator
    {
        private const long MaxImageSize = 5 * 1024 * 1024;

        private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];
        private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp"];

        public static List<string> ValidateCreate(ServiceCreateDto dto, IDevLogger? logger = null)
        {
            var errors = new List<string>();

            logger?.LogDebug("Iniciando validación de servicio (create)");

            errors.AddRange(ValidateServiceName(dto.ServiceName));
            errors.AddRange(ValidateDescription(dto.ServiceDescription));
            errors.AddRange(ValidatePrice(dto.Price));
            errors.AddRange(ValidateDuration(dto.DurationMinutes));
            errors.AddRange(ValidateServiceType(dto.ServiceType));

            if (dto.Image != null)
                errors.AddRange(ValidateImageFile(dto.Image, logger));

            if (errors.Count > 0)
                logger?.LogWarning("Validación de servicio (create) fallida: {Errors}", string.Join(", ", errors));

            return errors;
        }

        public static List<string> ValidateUpdate(ServiceUpdateDto dto, IDevLogger? logger = null)
        {
            var errors = new List<string>();

            logger?.LogDebug("Iniciando validación de servicio (update)");

            errors.AddRange(ValidateServiceName(dto.ServiceName));
            errors.AddRange(ValidateDescription(dto.ServiceDescription));
            errors.AddRange(ValidatePrice(dto.Price));
            errors.AddRange(ValidateDuration(dto.DurationMinutes));
            errors.AddRange(ValidateServiceType(dto.ServiceType));

            if (dto.Image != null)
                errors.AddRange(ValidateImageFile(dto.Image, logger));

            if (errors.Count > 0)
                logger?.LogWarning("Validación de servicio (update) fallida: {Errors}", string.Join(", ", errors));

            return errors;
        }

        public static List<string> ValidateServiceName(string? name)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add("El nombre del servicio es obligatorio.");
                return errors;
            }

            var trimmed = name.Trim();

            if (trimmed.Length < 2)
                errors.Add("El nombre del servicio debe tener al menos 2 caracteres.");

            if (trimmed.Length > 100)
                errors.Add("El nombre del servicio no puede exceder 100 caracteres.");

            if (!ServiceNameRegex().IsMatch(trimmed))
                errors.Add("El nombre del servicio contiene caracteres no permitidos.");

            if (trimmed.Contains("  "))
                errors.Add("El nombre del servicio no puede contener espacios múltiples consecutivos.");

            return errors;
        }

        public static List<string> ValidateDescription(string? description)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(description))
                return errors;

            var trimmed = description.Trim();

            if (trimmed.Length > 255)
                errors.Add("La descripción no puede exceder 255 caracteres.");

            if (!DescriptionRegex().IsMatch(trimmed))
                errors.Add("La descripción contiene caracteres no permitidos.");

            return errors;
        }

        public static List<string> ValidatePrice(decimal price)
        {
            var errors = new List<string>();

            if (price <= 0)
                errors.Add("El precio debe ser mayor a 0.");

            if (price > 999999.99m)
                errors.Add("El precio excede el máximo permitido.");

            var rounded = decimal.Round(price, 2);
            if (price != rounded)
                errors.Add("El precio debe tener máximo 2 decimales.");

            return errors;
        }

        public static List<string> ValidateDuration(int durationMinutes)
        {
            var errors = new List<string>();

            if (durationMinutes <= 0)
                errors.Add("La duración debe ser mayor a 0 minutos.");

            if (durationMinutes > 24 * 60)
                errors.Add("La duración no puede exceder 1440 minutos.");

            if (durationMinutes % 5 != 0)
                errors.Add("La duración debe estar en múltiplos de 5 minutos.");

            return errors;
        }

        public static List<string> ValidateServiceType(string? serviceType)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(serviceType))
                return errors;

            var trimmed = serviceType.Trim();

            if (trimmed.Length < 3)
                errors.Add("El tipo de servicio debe tener al menos 3 caracteres.");

            if (trimmed.Length > 50)
                errors.Add("El tipo de servicio no puede exceder 50 caracteres.");

            if (!ServiceTypeRegex().IsMatch(trimmed))
                errors.Add("El tipo de servicio contiene caracteres no permitidos.");

            return errors;
        }

        public static List<string> ValidateImageFile(IFormFile? file, IDevLogger? logger = null)
        {
            var errors = new List<string>();

            if (file == null || file.Length == 0)
                return errors;

            logger?.LogDebug("Validando imagen de servicio: {FileName} {ContentType} {Size}",
                file.FileName, file.ContentType, file.Length);

            if (file.Length > MaxImageSize)
                errors.Add("La imagen no puede exceder 5MB.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            bool isValidExtension = AllowedExtensions.Contains(extension);
            if (!isValidExtension)
                errors.Add($"Extensión no permitida. Formatos válidos: {string.Join(", ", AllowedExtensions)}");

            var contentType = file.ContentType.ToLowerInvariant();
            bool isValidContentType =
                AllowedContentTypes.Contains(contentType) ||
                contentType.StartsWith("image/") ||
                contentType == "application/octet-stream";

            if (!isValidContentType && isValidExtension)
            {
                logger?.LogWarning("ContentType no estándar: {ContentType}. Se acepta por extensión válida: {Extension}",
                    contentType, extension);
            }

            return errors;
        }

        [GeneratedRegex(@"^[a-zA-Z0-9áéíóúÁÉÍÓÚñÑüÜ\s\-\.,'\/&()]+$")]
        private static partial Regex ServiceNameRegex();

        [GeneratedRegex(@"^[a-zA-Z0-9áéíóúÁÉÍÓÚñÑüÜ\s\-\.,'\/&():;]+$")]
        private static partial Regex DescriptionRegex();

        [GeneratedRegex(@"^[a-zA-Z0-9áéíóúÁÉÍÓÚñÑüÜ\s\-\.,'\/&()]+$")]
        private static partial Regex ServiceTypeRegex();
    }
}
