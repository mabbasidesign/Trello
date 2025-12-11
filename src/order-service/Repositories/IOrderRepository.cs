using order_service.Models;

namespace order_service.Repositories;

public interface IOrderRepository
{
    Task<PagedResult<Order>> GetAllAsync(int page, int pageSize, string? status = null);
    Task<Order?> GetByIdAsync(int id);
    Task<Order> CreateAsync(Order order);
    Task<Order?> UpdateAsync(int id, Order order);
    Task<bool> DeleteAsync(int id);
    Task<Order?> UpdateStatusAsync(int id, string status);
}
