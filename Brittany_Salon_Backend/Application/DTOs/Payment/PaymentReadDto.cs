namespace Brittany_Salon_Backend.Application.DTOs.Payment
{
    public class PaymentReadDto
    {
        public int PaymentId { get; set; }
        public int AppointmentId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? PaymentStatus { get; set; }
        public bool IsActive { get; set; }
        public string? CancellationReason { get; set; }
    }
}