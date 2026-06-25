using System.ComponentModel.DataAnnotations;

namespace Brittany_Salon_Backend.Application.DTOs.Review
{
    public class ReviewEmployeeUpdateDto
    {
        [Required(ErrorMessage = "La respuesta es obligatoria.")]
        [MaxLength(255, ErrorMessage = "La respuesta no puede exceder 255 caracteres.")]
        public string Response { get; set; } = string.Empty;
    }
}
