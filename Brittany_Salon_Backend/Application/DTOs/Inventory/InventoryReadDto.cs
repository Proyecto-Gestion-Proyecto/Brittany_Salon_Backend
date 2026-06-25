namespace Brittany_Salon_Backend.Application.DTOs.Inventory
{
    public class InventoryReadDto
    {
        public int InventoryId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public int MinimumStock { get; set; }
        public int MaximumStock { get; set; }
        public string? Location { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
        public bool LowStockAlert { get; set; }
    }
}
