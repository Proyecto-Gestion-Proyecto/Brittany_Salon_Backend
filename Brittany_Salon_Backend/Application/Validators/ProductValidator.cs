using Brittany_Salon_Backend.Application.DTOs.Product;
using Brittany_Salon_Backend.Infrastructure.Logging;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Brittany_Salon_Backend.Infrastructure.Persistence;

namespace Brittany_Salon_Backend.Application.Validators
{
    public static partial class ProductValidator
    {
        private const long MaxImageSize = 5 * 1024 * 1024;

        private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];
        private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp"];

        public static List<string> ValidateCreate(ProductCreateDto dto, IDevLogger? logger = null)
        {
            var errors = new List<string>();

            logger?.LogDebug("Iniciando validación de producto (create)");

            errors.AddRange(ValidateProductName(dto.ProductName));
            errors.AddRange(ValidateDescription(dto.ProductDescription));
            errors.AddRange(ValidatePrice(dto.Price));
            errors.AddRange(ValidateCategoryId(dto.CategoryId));

            if (dto.Image == null || dto.Image.Length == 0)
                errors.Add("La imagen del producto es obligatoria.");
            else
                errors.AddRange(ValidateImageFile(dto.Image, logger));

            if (errors.Count > 0)
                logger?.LogWarning("Validación de producto (create) fallida: {Errors}", string.Join(", ", errors));

            return errors;
        }

        public static List<string> ValidateUpdate(ProductUpdateDto dto, IDevLogger? logger = null)
        {
            var errors = new List<string>();

            logger?.LogDebug("Iniciando validación de producto (update)");

            errors.AddRange(ValidateProductName(dto.ProductName));
            errors.AddRange(ValidateDescription(dto.ProductDescription));
            errors.AddRange(ValidatePrice(dto.Price));
            errors.AddRange(ValidateCategoryId(dto.CategoryId));
            errors.AddRange(ValidateIsActive(dto.IsActive));

            if (dto.Image != null)
                errors.AddRange(ValidateImageFile(dto.Image, logger));

            if (errors.Count > 0)
                logger?.LogWarning("Validación de producto (update) fallida: {Errors}", string.Join(", ", errors));

            return errors;
        }

        public static List<string> ValidateProductName(string? name)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add("El nombre del producto es obligatorio.");
                return errors;
            }

            var trimmed = name.Trim();

            if (trimmed.Length < 2)
                errors.Add("El nombre del producto debe tener al menos 2 caracteres.");

            if (trimmed.Length > 100)
                errors.Add("El nombre del producto no puede exceder 100 caracteres.");

            if (!ProductNameRegex().IsMatch(trimmed))
                errors.Add("El nombre del producto contiene caracteres no permitidos.");

            if (trimmed.Contains("  "))
                errors.Add("El nombre del producto no puede contener espacios múltiples consecutivos.");

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

        public static List<string> ValidateCategoryId(int categoryId)
        {
            var errors = new List<string>();

            if (categoryId <= 0)
                errors.Add("La categoría del producto es obligatoria.");

            return errors;
        }

        public static List<string> ValidateIsActive(bool isActive)
        {
            return new List<string>();
        }

        public static List<string> ValidateImageFile(IFormFile? file, IDevLogger? logger = null)
        {
            var errors = new List<string>();

            if (file == null || file.Length == 0)
                return errors;

            logger?.LogDebug("Validando imagen de producto: {FileName} {ContentType} {Size}",
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

        public static async Task<List<string>> ValidateCategoryExistsAsync(int categoryId, AppDbContext db, IDevLogger? logger = null)
        {
            var errors = new List<string>();

            if (categoryId <= 0)
            {
                errors.Add("La categoría del producto es obligatoria.");
                return errors;
            }

            var exists = await db.Categories
                .AsNoTracking()
                .AnyAsync(c => c.CategoryId == categoryId && c.IsActive);

            if (!exists)
                errors.Add("La categoría no existe o está inactiva.");

            if (errors.Count > 0)
                logger?.LogWarning("Validación de categoría fallida: {Errors}", string.Join(", ", errors));

            return errors;
        }

        [GeneratedRegex(@"^[a-zA-Z0-9áéíóúÁÉÍÓÚñÑüÜ\s\-\.,'\/&()]+$")]
        private static partial Regex ProductNameRegex();

        [GeneratedRegex(@"^[a-zA-Z0-9áéíóúÁÉÍÓÚñÑüÜ\s\-\.,'\/&():;]+$")]
        private static partial Regex DescriptionRegex();
    }
}
