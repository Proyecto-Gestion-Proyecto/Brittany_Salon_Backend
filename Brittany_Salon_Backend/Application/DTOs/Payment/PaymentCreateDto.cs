using System.ComponentModel.DataAnnotations;

namespace Brittany_Salon_Backend.Application.DTOs.Payment
{
    public class PaymentCreateDto
    {
        [Required(ErrorMessage = "El ID de la cita es obligatorio.")]
        public int AppointmentId { get; set; }

        [Required(ErrorMessage = "El monto es obligatorio.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a 0.")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "El método de pago es obligatorio.")]
        [MaxLength(50, ErrorMessage = "El método de pago no puede exceder 50 caracteres.")]
        [AllowedValues("Efectivo", "SINPE", ErrorMessage = "El método de pago debe ser 'Efectivo' o 'SINPE'.")]
        public string PaymentMethod { get; set; } = string.Empty;

        [MaxLength(50, ErrorMessage = "El estado del pago no puede exceder 50 caracteres.")]
        public string? PaymentStatus { get; set; }

        public bool? IsActive { get; set; } = true;
    }
}