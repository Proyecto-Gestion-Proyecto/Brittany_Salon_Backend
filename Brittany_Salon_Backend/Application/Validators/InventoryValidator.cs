using Brittany_Salon_Backend.Application.DTOs.Inventory;
using Brittany_Salon_Backend.Infrastructure.Logging;

namespace Brittany_Salon_Backend.Application.Validators
{
    public static class InventoryValidator
    {
        public static List<string> ValidateCreate(InventoryCreateDto dto, IDevLogger? logger = null)
        {
            var errors = new List<string>();

            logger?.LogDebug("Iniciando validación de inventario (create)");

            errors.AddRange(ValidateProductId(dto.ProductId));
            errors.AddRange(ValidateQuantity(dto.Quantity));
            errors.AddRange(ValidateMinimumStock(dto.MinimumStock));
            errors.AddRange(ValidateMaximumStock(dto.MaximumStock, dto.MinimumStock));
            errors.AddRange(ValidateLocation(dto.Location));
            errors.AddRange(ValidateNotes(dto.Notes));

            if (errors.Count > 0)
                logger?.LogWarning("Validación de inventario (create) fallida: {Errors}", string.Join(", ", errors));

            return errors;
        }

        public static List<string> ValidateProductId(int productId)
        {
            var errors = new List<string>();

            if (productId <= 0)
                errors.Add("El ID del producto debe ser mayor a 0.");

            return errors;
        }

        public static List<string> ValidateQuantity(int quantity)
        {
            var errors = new List<string>();

            if (quantity < 0)
                errors.Add("La cantidad no puede ser negativa.");

            return errors;
        }

        public static List<string> ValidateMinimumStock(int minimumStock)
        {
            var errors = new List<string>();

            if (minimumStock < 0)
                errors.Add("El stock mínimo no puede ser negativo.");

            return errors;
        }

        public static List<string> ValidateMaximumStock(int maximumStock, int minimumStock)
        {
            var errors = new List<string>();

            if (maximumStock < 0)
                errors.Add("El stock máximo no puede ser negativo.");

            if (maximumStock < minimumStock)
                errors.Add("El stock máximo debe ser mayor o igual al stock mínimo.");

            return errors;
        }

        public static List<string> ValidateLocation(string? location)
        {
            var errors = new List<string>();

            if (!string.IsNullOrWhiteSpace(location))
            {
                var trimmed = location.Trim();
                if (trimmed.Length > 100)
                    errors.Add("La ubicación no puede exceder 100 caracteres.");
            }

            return errors;
        }

        public static List<string> ValidateNotes(string? notes)
        {
            var errors = new List<string>();

            if (!string.IsNullOrWhiteSpace(notes))
            {
                var trimmed = notes.Trim();
                if (trimmed.Length > 255)
                    errors.Add("Las notas no pueden exceder 255 caracteres.");
            }

            return errors;
        }
    }
}
