using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Brittany_Salon_Backend.Application.DTOs.Employee
{
    public class EmployeeUpdateDto
    {
        [MinLength(2, ErrorMessage = "El nombre debe tener al menos 2 caracteres.")]
        [MaxLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres.")]
        public string? Name { get; set; }

        [RegularExpression(@"^\d{8}$", ErrorMessage = "El telefono debe tener 8 digitos.")]
        public string? Phone { get; set; }

        [MaxLength(150, ErrorMessage = "El correo no puede exceder 150 caracteres.")]
        [EmailAddress(ErrorMessage = "El formato del correo electronico no es valido.")]
        public string? Email { get; set; }

        [MinLength(8, ErrorMessage = "La contrasena debe tener al menos 8 caracteres.")]
        [MaxLength(255, ErrorMessage = "La contrasena no puede exceder 255 caracteres.")]
        public string? Password { get; set; }

        /// <summary>
        /// Archivo de imagen (multipart/form-data)
        /// Si se proporciona, reemplaza la imagen anterior
        /// </summary>
        public IFormFile? Image { get; set; }

        /// <summary>
        /// Si es true, elimina la imagen actual sin reemplazarla
        /// </summary>
        public bool RemoveImage { get; set; } = false;

        [MaxLength(100, ErrorMessage = "La especialidad no puede exceder 100 caracteres.")]
        public string? Specialty { get; set; }

        public bool? IsActive { get; set; }
    }
}
