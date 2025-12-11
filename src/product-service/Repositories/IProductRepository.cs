using product_service.Models;

namespace product_service.Repositories;

public interface IProductRepository
{
    Task<PagedResult<Product>> GetAllAsync(int page = 1, int pageSize = 10);
    Task<Product?> GetByIdAsync(int id);
    Task<Product> CreateAsync(Product product);
    Task<Product?> UpdateAsync(int id, Product product);
    Task<bool> DeleteAsync(int id);
}
