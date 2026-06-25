using System.ComponentModel.DataAnnotations;

namespace Brittany_Salon_Backend.Application.DTOs.Review
{
    public class ReviewCreateDto
    {
        [MaxLength(255, ErrorMessage = "El comentario no puede exceder 255 caracteres.")]
        public string? Comment { get; set; }

        [Required(ErrorMessage = "La calificación es obligatoria.")]
        [Range(1, 5, ErrorMessage = "La calificación debe estar entre 1 y 5.")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "El ID del cliente es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El ID del cliente debe ser mayor a 0.")]
        public int ClientId { get; set; }

        [Required(ErrorMessage = "El ID del empleado es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El ID del empleado debe ser mayor a 0.")]
        public int EmployeeId { get; set; }
    }
}
