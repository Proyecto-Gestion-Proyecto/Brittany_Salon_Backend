namespace Brittany_Salon_Backend.Application.DTOs.Employee
{
    public class EmployeeReadDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Image { get; set; }
        public string? Specialty { get; set; }
        public DateTime? DateCreated { get; set; }
        public bool IsActive { get; set; }
    }
}
