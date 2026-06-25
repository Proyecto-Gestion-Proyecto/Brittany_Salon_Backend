using System.ComponentModel.DataAnnotations;

namespace Brittany_Salon_Backend.Application.DTOs.Review
{
    public class ReviewResponseDto
    {
        [Required(ErrorMessage = "El ID del empleado es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El ID del empleado debe ser mayor a 0.")]
        public int EmployeeId { get; set; }

        [Required(ErrorMessage = "La respuesta es obligatoria.")]
        [MaxLength(500, ErrorMessage = "La respuesta no puede exceder 500 caracteres.")]
        public string Response { get; set; } = string.Empty;
    }
}
