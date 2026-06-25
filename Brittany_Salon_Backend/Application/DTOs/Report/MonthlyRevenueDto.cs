namespace Brittany_Salon_Backend.Application.DTOs.Report
{
    public class MonthlyRevenueDto
    { 
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public int TotalPayments { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AveragePayment { get; set; }
    }
}