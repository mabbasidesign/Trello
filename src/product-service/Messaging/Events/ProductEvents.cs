namespace ProductService.Messaging.Events;

public class ProductCreatedEvent
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ProductUpdatedEvent
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ProductDeletedEvent
{
    public int ProductId { get; set; }
    public DateTime DeletedAt { get; set; }
}
