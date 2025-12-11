using Microsoft.EntityFrameworkCore;
using order_service.Data;
using order_service.Models;

namespace order_service.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _context;

    public OrderRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<Order>> GetAllAsync(int page, int pageSize, string? status = null)
    {
        var query = _context.Orders.Include(o => o.OrderItems).AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(o => o.Status.ToLower().Contains(status.ToLower()));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Order>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<Order?> GetByIdAsync(int id)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<Order> CreateAsync(Order order)
    {
        order.OrderDate = DateTime.UtcNow;
        order.CreatedAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;
        
        // Calculate total amount from order items
        order.TotalAmount = order.OrderItems.Sum(oi => oi.Quantity * oi.UnitPrice);

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }

    public async Task<Order?> UpdateAsync(int id, Order order)
    {
        var existingOrder = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (existingOrder == null) return null;

        existingOrder.CustomerName = order.CustomerName;
        existingOrder.CustomerEmail = order.CustomerEmail;
        existingOrder.ShippingAddress = order.ShippingAddress;
        existingOrder.Status = order.Status;
        existingOrder.UpdatedAt = DateTime.UtcNow;

        // Update order items
        _context.OrderItems.RemoveRange(existingOrder.OrderItems);
        existingOrder.OrderItems = order.OrderItems;
        existingOrder.TotalAmount = order.OrderItems.Sum(oi => oi.Quantity * oi.UnitPrice);

        await _context.SaveChangesAsync();
        return existingOrder;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return false;

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<(Order? order, string? oldStatus)> UpdateStatusAsync(int id, string status)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return (null, null);

        var oldStatus = order.Status;
        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;

        if (status == "Shipped" && order.ShippedDate == null)
        {
            order.ShippedDate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return (order, oldStatus);
    }
}
