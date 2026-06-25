using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Brittany_Salon_Backend.Application.DTOs.Client
{
    public class ClientCreateDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [MinLength(2, ErrorMessage = "El nombre debe tener al menos 2 caracteres.")]
        [MaxLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [MaxLength(150, ErrorMessage = "El correo no puede exceder 150 caracteres.")]
        [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "El formato del teléfono no es válido.")]
        [MaxLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres.")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        [MaxLength(255, ErrorMessage = "La contraseña no puede exceder 255 caracteres.")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Archivo de imagen (multipart/form-data)
        /// Tamaño máximo: 5MB
        /// Formatos permitidos: JPEG, PNG, GIF, WebP
        /// </summary>
        public IFormFile? Image { get; set; }

        public bool? IsActive { get; set; } = true;
    }
}