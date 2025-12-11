using product_service.Models;

namespace product_service.Repositories;

public class InMemoryProductRepository : IProductRepository
{
    private readonly List<Product> _products = new()
    {
        new() { Id = 1, Name = "Laptop", Description = "High-performance laptop", Price = 999.99m, Stock = 10, CreatedAt = DateTime.UtcNow },
        new() { Id = 2, Name = "Mouse", Description = "Wireless mouse", Price = 29.99m, Stock = 50, CreatedAt = DateTime.UtcNow },
        new() { Id = 3, Name = "Keyboard", Description = "Mechanical keyboard", Price = 79.99m, Stock = 25, CreatedAt = DateTime.UtcNow }
    };

    public Task<PagedResult<Product>> GetAllAsync(int page = 1, int pageSize = 10)
    {
        var totalCount = _products.Count;
        var items = _products
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var result = new PagedResult<Product>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return Task.FromResult(result);
    }

    public Task<Product?> GetByIdAsync(int id)
    {
        var product = _products.FirstOrDefault(p => p.Id == id);
        return Task.FromResult(product);
    }

    public Task<Product> CreateAsync(Product product)
    {
        product.Id = _products.Any() ? _products.Max(p => p.Id) + 1 : 1;
        product.CreatedAt = DateTime.UtcNow;
        _products.Add(product);
        return Task.FromResult(product);
    }

    public Task<Product?> UpdateAsync(int id, Product updatedProduct)
    {
        var product = _products.FirstOrDefault(p => p.Id == id);
        if (product is null) return Task.FromResult<Product?>(null);

        product.Name = updatedProduct.Name;
        product.Description = updatedProduct.Description;
        product.Price = updatedProduct.Price;
        product.Stock = updatedProduct.Stock;
        product.UpdatedAt = DateTime.UtcNow;

        return Task.FromResult<Product?>(product);
    }

    public Task<bool> DeleteAsync(int id)
    {
        var product = _products.FirstOrDefault(p => p.Id == id);
        if (product is null) return Task.FromResult(false);

        _products.Remove(product);
        return Task.FromResult(true);
    }
}
