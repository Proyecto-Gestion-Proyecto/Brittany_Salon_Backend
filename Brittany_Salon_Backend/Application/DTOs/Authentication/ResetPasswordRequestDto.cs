using System.ComponentModel.DataAnnotations;

namespace Brittany_Salon_Backend.Application.DTOs.Authentication
{
    public class ResetPasswordRequestDto
    {
        [Required(ErrorMessage = "El correo electronico es requerido.")]
        [EmailAddress(ErrorMessage = "El formato del correo electronico no es valido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "El codigo de verificacion es requerido.")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva contrasena es requerida.")]
        [MinLength(8, ErrorMessage = "La contrasena debe tener al menos 8 caracteres.")]
        public string NewPassword { get; set; } = string.Empty;
    }
}
