namespace Brittany_Salon_Backend.Application.DTOs.Report
{
    public class DailyTotalDto
    {
        public DateTime Date { get; set; }
        public int TotalPayments { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
