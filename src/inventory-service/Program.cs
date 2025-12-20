
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using InventoryService.Models;
using InventoryService.Repositories;
using InventoryService.DTOs;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IInventoryRepository, InventoryRepository>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();



var repo = app.Services.GetRequiredService<IInventoryRepository>();

// Seed initial data
await repo.AddAsync(new InventoryItem {
    ProductName = "Laptop",
    Quantity = 10,
    Location = "Warehouse A"
});
await repo.AddAsync(new InventoryItem {
    ProductName = "Mouse",
    Quantity = 50,
    Location = "Warehouse B"
});
await repo.AddAsync(new InventoryItem {
    ProductName = "Keyboard",
    Quantity = 30,
    Location = "Warehouse A"
});

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