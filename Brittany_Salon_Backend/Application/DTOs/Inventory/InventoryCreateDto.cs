using System.ComponentModel.DataAnnotations;

namespace Brittany_Salon_Backend.Application.DTOs.Inventory
{
    public class InventoryCreateDto
    {
        [Required(ErrorMessage = "El ID del producto es obligatorio.")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "La cantidad es obligatoria.")]
        [Range(0, int.MaxValue, ErrorMessage = "La cantidad no puede ser negativa.")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "El stock mínimo es obligatorio.")]
        [Range(0, int.MaxValue, ErrorMessage = "El stock mínimo no puede ser negativo.")]
        public int MinimumStock { get; set; }

        [Required(ErrorMessage = "El stock máximo es obligatorio.")]
        [Range(0, int.MaxValue, ErrorMessage = "El stock máximo no puede ser negativo.")]
        public int MaximumStock { get; set; }

        [MaxLength(100, ErrorMessage = "La ubicación no puede exceder 100 caracteres.")]
        public string? Location { get; set; }

        [MaxLength(255, ErrorMessage = "Las notas no pueden exceder 255 caracteres.")]
        public string? Notes { get; set; }
    }
}
