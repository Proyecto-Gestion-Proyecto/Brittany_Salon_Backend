using System.ComponentModel.DataAnnotations;

namespace Brittany_Salon_Backend.Application.DTOs.Category
{
    public class CategoryUpdateDto
    {
        [Required(ErrorMessage = "El nombre de la categoría es obligatorio.")]
        [MinLength(2, ErrorMessage = "El nombre debe tener al menos 2 caracteres.")]
        [MaxLength(50, ErrorMessage = "El nombre no puede exceder 50 caracteres.")]
        public string CategoryName { get; set; } = string.Empty;

        [MaxLength(255, ErrorMessage = "La descripción no puede exceder 255 caracteres.")]
        public string? CategoryDescription { get; set; }

        [Required(ErrorMessage = "El estado de la categoría es obligatorio.")]
        public bool IsActive { get; set; }
    }
}
