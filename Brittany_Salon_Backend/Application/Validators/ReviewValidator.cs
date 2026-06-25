using Brittany_Salon_Backend.Application.DTOs.Review;
using Brittany_Salon_Backend.Infrastructure.Logging;

namespace Brittany_Salon_Backend.Application.Validators
{
    public static class ReviewValidator
    {
        public static List<string> ValidateCreate(ReviewCreateDto dto, IDevLogger? logger = null)
        {
            var errors = new List<string>();

            logger?.LogDebug("Iniciando validación de reseña...");

            errors.AddRange(ValidateRating(dto.Rating));

            if (!string.IsNullOrWhiteSpace(dto.Comment))
            {
                errors.AddRange(ValidateComment(dto.Comment));
            }
            errors.AddRange(ValidateClientId(dto.ClientId));
            errors.AddRange(ValidateEmployeeId(dto.EmployeeId));

            if (errors.Count > 0)
            {
                logger?.LogWarning("Validación fallida con {Count} errores: {Errors}", errors.Count, string.Join(", ", errors));
            }
            else
            {
                logger?.LogInfo("Validación exitosa para reseña del cliente: {ClientId}", dto.ClientId);
            }

            return errors;
        }

        public static List<string> ValidateResponse(ReviewResponseDto dto, IDevLogger? logger = null)
        {
            var errors = new List<string>();

            logger?.LogDebug("Iniciando validación de respuesta a reseña...");

            errors.AddRange(ValidateEmployeeId(dto.EmployeeId));

            if (!string.IsNullOrWhiteSpace(dto.Response))
            {
                errors.AddRange(ValidateResponseText(dto.Response));
            }

            if (errors.Count > 0)
            {
                logger?.LogWarning("Validación fallida con {Count} errores: {Errors}", errors.Count, string.Join(", ", errors));
            }
            else
            {
                logger?.LogInfo("Validación exitosa para respuesta de empleado: {EmployeeId}", dto.EmployeeId);
            }

            return errors;
        }

        public static List<string> ValidateUpdate(ReviewUpdateDto dto, IDevLogger? logger = null)
        {
            var errors = new List<string>();

            logger?.LogDebug("Iniciando validación de actualización de reseña...");

            if (dto.Rating.HasValue)
            {
                errors.AddRange(ValidateRating(dto.Rating.Value));
            }

            if (!string.IsNullOrWhiteSpace(dto.Comment))
            {
                errors.AddRange(ValidateComment(dto.Comment));
            }

            if (!string.IsNullOrWhiteSpace(dto.Response))
            {
                errors.AddRange(ValidateResponse(dto.Response));
            }

            if (errors.Count > 0)
            {
                logger?.LogWarning("Validación fallida con {Count} errores: {Errors}", errors.Count, string.Join(", ", errors));
            }
            else
            {
                logger?.LogInfo("Validación exitosa para actualización de reseña");
            }

            return errors;
        }

        private static List<string> ValidateRating(int rating)
        {
            var errors = new List<string>();

            if (rating < 1 || rating > 5)
                errors.Add("La calificación debe estar entre 1 y 5.");

            return errors;
        }

        private static List<string> ValidateComment(string comment)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(comment))
                return errors;

            var trimmed = comment.Trim();

            if (trimmed.Length > 255)
                errors.Add("El comentario no puede exceder 255 caracteres.");

            return errors;
        }

        private static List<string> ValidateResponse(string response)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(response))
                return errors;

            var trimmed = response.Trim();

            if (trimmed.Length > 255)
                errors.Add("La respuesta no puede exceder 255 caracteres.");

            return errors;
        }

        private static List<string> ValidateResponseText(string response)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(response))
                return errors;

            var trimmed = response.Trim();

            if (trimmed.Length > 500)
                errors.Add("La respuesta no puede exceder 500 caracteres.");

            return errors;
        }

        private static List<string> ValidateClientId(int clientId)
        {
            var errors = new List<string>();

            if (clientId <= 0)
                errors.Add("El ID del cliente debe ser mayor a 0.");

            return errors;
        }

        private static List<string> ValidateEmployeeId(int employeeId)
        {
            var errors = new List<string>();

            if (employeeId <= 0)
                errors.Add("El ID del empleado debe ser mayor a 0.");

            return errors;
        }
    }
}
