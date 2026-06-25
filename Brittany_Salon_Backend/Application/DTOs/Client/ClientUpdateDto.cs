using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Brittany_Salon_Backend.Application.DTOs.Client
{
    public class ClientUpdateDto
    {
        [MinLength(2, ErrorMessage = "El nombre debe tener al menos 2 caracteres.")]
        [MaxLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres.")]
        public string? Name { get; set; }

        [MaxLength(150, ErrorMessage = "El correo no puede exceder 150 caracteres.")]
        [EmailAddress(ErrorMessage = "El formato del correo electr�nico no es v�lido.")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "El formato del tel�fono no es v�lido.")]
        [MaxLength(20, ErrorMessage = "El tel�fono no puede exceder 20 caracteres.")]
        public string? Phone { get; set; }

        [MinLength(8, ErrorMessage = "La contrase�a debe tener al menos 8 caracteres.")]
        [MaxLength(255, ErrorMessage = "La contrase�a no puede exceder 255 caracteres.")]
        public string? Password { get; set; }


        /// <summary>
        /// Archivo de imagen (multipart/form-data)
        /// Tama�o m�ximo: 5MB
        /// Formatos permitidos: JPEG, PNG, GIF, WebP
        /// </summary>
        public IFormFile? Image { get; set; }
    }
}