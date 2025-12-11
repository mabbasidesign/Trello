using System.ComponentModel.DataAnnotations;

namespace order_service.Models;

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    [Required]
    public int ProductId { get; set; }

    [Required]
    [StringLength(100)]
    public string ProductName { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Required]
    [Range(0.01, 1000000)]
    public decimal UnitPrice { get; set; }

    public decimal TotalPrice => Quantity * UnitPrice;

    public Order Order { get; set; } = null!;
}
