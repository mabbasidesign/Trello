
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using InventoryService.Models;
using InventoryService.Repositories;
using Microsoft.Azure.Cosmos;
using InventoryService.DTOs;

var builder = WebApplication.CreateBuilder(args);

// Cosmos DB configuration (Managed Identity)
using Azure.Identity;
var accountEndpoint = builder.Configuration["CosmosDb:AccountEndpoint"] ?? "<YOUR_COSMOS_ACCOUNT_ENDPOINT>";
var databaseId = builder.Configuration["CosmosDb:DatabaseId"] ?? "InventoryDb";
var containerId = builder.Configuration["CosmosDb:ContainerId"] ?? "InventoryItems";

builder.Services.AddSingleton<IInventoryRepository>(sp => {
    var client = new CosmosClient(accountEndpoint, new DefaultAzureCredential());
    return new CosmosDbInventoryRepository(client, databaseId, containerId);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();

var repo = app.Services.GetRequiredService<IInventoryRepository>();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/inventory", async () =>
    (await repo.GetAllAsync()).Select(ToDto)
);

app.MapGet("/inventory/{id}", async (string id) => {
    var item = await repo.GetByIdAsync(id);
    return item is not null ? Results.Ok(ToDto(item)) : Results.NotFound();
});

app.MapPost("/inventory", async (InventoryItemDto dto) => {
    var item = FromDto(dto);
    await repo.AddAsync(item);
    return Results.Created($"/inventory/{item.Id}", ToDto(item));
});

app.MapPut("/inventory/{id}", async (string id, InventoryItemDto dto) => {
    var existing = await repo.GetByIdAsync(id);
    if (existing is null) return Results.NotFound();
    var updated = FromDto(dto);
    updated.Id = id;
    await repo.UpdateAsync(updated);
    return Results.NoContent();
});

app.MapDelete("/inventory/{id}", async (string id) => {
    var existing = await repo.GetByIdAsync(id);
    if (existing is null) return Results.NotFound();
    await repo.DeleteAsync(id);
    return Results.NoContent();
});

app.Run();

// Mapping helpers
InventoryItemDto ToDto(InventoryItem item) => new InventoryItemDto {
    Id = item.Id,
    ProductName = item.ProductName,
    Quantity = item.Quantity,
    Location = item.Location
};

InventoryItem FromDto(InventoryItemDto dto) => new InventoryItem {
    Id = string.IsNullOrEmpty(dto.Id) ? Guid.NewGuid().ToString() : dto.Id,
    ProductName = dto.ProductName,
    Quantity = dto.Quantity,
    Location = dto.Location
};

app.Run();