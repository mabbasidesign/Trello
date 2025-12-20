namespace InventoryService.DTOs {
    public class InventoryItemDto {
        public string Id { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Location { get; set; } = string.Empty;
    }
}