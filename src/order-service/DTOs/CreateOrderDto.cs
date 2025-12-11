using System.ComponentModel.DataAnnotations;

namespace order_service.DTOs;

public class CreateOrderDto
{
    [Required(ErrorMessage = "Customer name is required")]
    [StringLength(100, MinimumLength = 2)]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Customer email is required")]
    [EmailAddress]
    public string CustomerEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Shipping address is required")]
    public string ShippingAddress { get; set; } = string.Empty;

    [Required]
    public List<CreateOrderItemDto> OrderItems { get; set; } = new();
}

public class CreateOrderItemDto
{
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
}
