using Brittany_Salon_Backend.Application.DTOs.Category;
using Brittany_Salon_Backend.Infrastructure.Logging;
using System.Text.RegularExpressions;

namespace Brittany_Salon_Backend.Application.Validators
{
    public static partial class CategoryValidator
    {
        public static List<string> ValidateCreate(CategoryCreateDto dto, IDevLogger? logger = null)
        {
            var errors = new List<string>();

            logger?.LogDebug("Iniciando validación de categoría (create)");

            errors.AddRange(ValidateCategoryName(dto.CategoryName));
            errors.AddRange(ValidateDescription(dto.CategoryDescription));

            if (errors.Count > 0)
                logger?.LogWarning("Validación de categoría (create) fallida: {Errors}", string.Join(", ", errors));

            return errors;
        }

        public static List<string> ValidateUpdate(CategoryUpdateDto dto, IDevLogger? logger = null)
        {
            var errors = new List<string>();

            logger?.LogDebug("Iniciando validación de categoría (update)");

            errors.AddRange(ValidateCategoryName(dto.CategoryName));
            errors.AddRange(ValidateDescription(dto.CategoryDescription));
            errors.AddRange(ValidateIsActive(dto.IsActive));

            if (errors.Count > 0)
                logger?.LogWarning("Validación de categoría (update) fallida: {Errors}", string.Join(", ", errors));

            return errors;
        }

        public static List<string> ValidateCategoryName(string? name)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add("El nombre de la categoría es obligatorio.");
                return errors;
            }

            var trimmed = name.Trim();

            if (trimmed.Length < 2)
                errors.Add("El nombre de la categoría debe tener al menos 2 caracteres.");

            if (trimmed.Length > 50)
                errors.Add("El nombre de la categoría no puede exceder 50 caracteres.");

            if (!CategoryNameRegex().IsMatch(trimmed))
                errors.Add("El nombre de la categoría contiene caracteres no permitidos.");

            if (trimmed.Contains("  "))
                errors.Add("El nombre de la categoría no puede contener espacios múltiples consecutivos.");

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

        public static List<string> ValidateIsActive(bool isActive)
        {
            return new List<string>();
        }

        [GeneratedRegex(@"^[a-zA-Z0-9áéíóúÁÉÍÓÚñÑüÜ\s\-\.,'\/&()]+$")]
        private static partial Regex CategoryNameRegex();

        [GeneratedRegex(@"^[a-zA-Z0-9áéíóúÁÉÍÓÚñÑüÜ\s\-\.,'\/&():;]+$")]
        private static partial Regex DescriptionRegex();
    }
}
