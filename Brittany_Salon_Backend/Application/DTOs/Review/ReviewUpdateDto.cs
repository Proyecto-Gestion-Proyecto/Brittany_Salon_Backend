using System.ComponentModel.DataAnnotations;

namespace Brittany_Salon_Backend.Application.DTOs.Review
{
    public class ReviewUpdateDto
    {
        [MaxLength(255, ErrorMessage = "El comentario no puede exceder 255 caracteres.")]
        public string? Comment { get; set; }

        [Range(1, 5, ErrorMessage = "La calificación debe estar entre 1 y 5.")]
        public int? Rating { get; set; }

        [MaxLength(255, ErrorMessage = "La respuesta no puede exceder 255 caracteres.")]
        public string? Response { get; set; }
    }
}
