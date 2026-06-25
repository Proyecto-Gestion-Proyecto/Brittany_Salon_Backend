namespace Brittany_Salon_Backend.Application.DTOs.Review
{
    public class ReviewReadDto
    {
        public int ReviewId { get; set; }
        public string? Comment { get; set; }
        public int Rating { get; set; }
        public string? ImageUrl { get; set; }
        public string? Response { get; set; }
        public DateTime ReviewDate { get; set; }
        public int ClientId { get; set; }
        public int? EmployeeId { get; set; }
        public string? ClientName { get; set; }
        public string? EmployeeName { get; set; }
        public string? ClientImageUrl { get; set; }
        public string? EmployeeImageUrl { get; set; }
    }
}
