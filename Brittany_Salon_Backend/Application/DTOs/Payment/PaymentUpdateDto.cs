using System.ComponentModel.DataAnnotations;

namespace Brittany_Salon_Backend.Application.DTOs.Payment
{
    public class PaymentUpdateDto
    {
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a 0.")]
        public decimal? Amount { get; set; }

        [MaxLength(50, ErrorMessage = "El método de pago no puede exceder 50 caracteres.")]
        public string? PaymentMethod { get; set; }

        [MaxLength(255, ErrorMessage = "Las notas no pueden exceder 255 caracteres.")]
        public string? Notes { get; set; }

        [MaxLength(50, ErrorMessage = "El estado del pago no puede exceder 50 caracteres.")]
        public string? PaymentStatus { get; set; }

        public bool? IsActive { get; set; }
    }
}