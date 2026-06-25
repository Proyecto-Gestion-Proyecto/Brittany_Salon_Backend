namespace Brittany_Salon_Backend.Application.DTOs.Category
{
    public class CategoryReadDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? CategoryDescription { get; set; }
        public bool IsActive { get; set; }
    }
}
