using InventoryService.Models;
using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InventoryService.Repositories {
    public class CosmosDbInventoryRepository : IInventoryRepository {
        private readonly Container _container;

        public CosmosDbInventoryRepository(CosmosClient client, string databaseId, string containerId) {
            _container = client.GetContainer(databaseId, containerId);
        }

        public async Task<IEnumerable<InventoryItem>> GetAllAsync() {
            var query = _container.GetItemQueryIterator<InventoryItem>("SELECT * FROM c");
            var results = new List<InventoryItem>();
            while (query.HasMoreResults) {
                var response = await query.ReadNextAsync();
                results.AddRange(response);
            }
            return results;
        }

        public async Task<InventoryItem?> GetByIdAsync(string id) {
            try {
                var response = await _container.ReadItemAsync<InventoryItem>(id, new PartitionKey(id));
                return response.Resource;
            } catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) {
                return null;
            }
        }

        public async Task AddAsync(InventoryItem item) {
            await _container.CreateItemAsync(item, new PartitionKey(item.Id));
        }

        public async Task UpdateAsync(InventoryItem item) {
            await _container.UpsertItemAsync(item, new PartitionKey(item.Id));
        }

        public async Task DeleteAsync(string id) {
            await _container.DeleteItemAsync<InventoryItem>(id, new PartitionKey(id));
        }
    }
}