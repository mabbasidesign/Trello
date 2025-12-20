using InventoryService.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InventoryService.Repositories {
    public class InventoryRepository : IInventoryRepository {
        private readonly List<InventoryItem> _items = new();

        public Task<IEnumerable<InventoryItem>> GetAllAsync() => Task.FromResult(_items.AsEnumerable());

        public Task<InventoryItem?> GetByIdAsync(string id) => Task.FromResult(_items.FirstOrDefault(i => i.Id == id));

        public Task AddAsync(InventoryItem item) {
            _items.Add(item);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(InventoryItem item) {
            var existing = _items.FirstOrDefault(i => i.Id == item.Id);
            if (existing != null) {
                existing.ProductName = item.ProductName;
                existing.Quantity = item.Quantity;
                existing.Location = item.Location;
            }
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id) {
            var item = _items.FirstOrDefault(i => i.Id == id);
            if (item != null) _items.Remove(item);
            return Task.CompletedTask;
        }
    }
}