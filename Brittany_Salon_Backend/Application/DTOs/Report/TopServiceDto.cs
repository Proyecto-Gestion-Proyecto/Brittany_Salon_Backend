namespace Brittany_Salon_Backend.Application.DTOs.Report
{
    public class TopServiceDto
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public int CompletedAppointments { get; set; } 
        public decimal TotalRevenue { get; set; }
        public int TotalMinutes { get; set; }
    }
}