namespace Brittany_Salon_Backend.Application.DTOs.Report
{
    public class DailyPaymentDto 
    {
        public int PaymentId { get; set; }
        public int AppointmentId { get; set; }
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
    }
}
