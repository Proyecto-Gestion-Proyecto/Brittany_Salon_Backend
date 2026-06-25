using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Brittany_Salon_Backend.Application.DTOs.Service
{
    public class ServiceCreateDto
    {
        [Required(ErrorMessage = "El nombre del servicio es obligatorio.")]
        [MinLength(2, ErrorMessage = "El nombre del servicio debe tener al menos 2 caracteres.")]
        [MaxLength(100, ErrorMessage = "El nombre del servicio no puede exceder 100 caracteres.")]
        public string ServiceName { get; set; } = string.Empty;

        [Required(ErrorMessage = "La descripción del servicio es obligatoria.")]
        [MaxLength(255, ErrorMessage = "La descripción no puede exceder 255 caracteres.")]
        public string ServiceDescription { get; set; } = string.Empty;

        [Required(ErrorMessage = "El precio es obligatorio.")]
        [Range(0.01, 999999.99, ErrorMessage = "El precio debe ser mayor a 0 y no exceder 999999.99.")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "La duración es obligatoria.")]
        [Range(5, 1440, ErrorMessage = "La duración debe estar entre 5 y 1440 minutos.")]
        public int DurationMinutes { get; set; }

        [Required(ErrorMessage = "El tipo de servicio es obligatorio.")]
        [MinLength(3, ErrorMessage = "El tipo de servicio debe tener al menos 3 caracteres.")]
        [MaxLength(50, ErrorMessage = "El tipo de servicio no puede exceder 50 caracteres.")]
        public string ServiceType { get; set; } = string.Empty;

        [Required(ErrorMessage = "El estado del servicio es obligatorio.")]
        public bool? IsActive { get; set; } = true;

        [Required(ErrorMessage = "La imagen del servicio es obligatoria.")]
        public IFormFile Image { get; set; } = default!;
    }
}
