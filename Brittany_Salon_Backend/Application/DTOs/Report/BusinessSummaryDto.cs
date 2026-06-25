namespace Brittany_Salon_Backend.Application.DTOs.Report
{
    public class BusinessSummaryDto
    {
        public decimal TotalRevenue { get; set; }
        public int CompletedAppointments { get; set; }
        public int ActiveClients { get; set; }
        public int ActiveServices { get; set; }
    } 
}