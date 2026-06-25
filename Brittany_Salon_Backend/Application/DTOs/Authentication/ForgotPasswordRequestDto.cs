using System.ComponentModel.DataAnnotations;

namespace Brittany_Salon_Backend.Application.DTOs.Authentication
{
    public class ForgotPasswordRequestDto
    {
        [Required(ErrorMessage = "El correo electronico es requerido.")]
        [EmailAddress(ErrorMessage = "El formato del correo electronico no es valido.")]
        public string Email { get; set; } = string.Empty;
    }
}
