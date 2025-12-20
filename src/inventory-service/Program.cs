using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using InventoryService.Models;
using InventoryService.Repositories;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IInventoryRepository, InventoryRepository>();
var app = builder.Build();

var repo = app.Services.GetRequiredService<IInventoryRepository>();

app.MapGet("/inventory", async () => await repo.GetAllAsync());
app.MapGet("/inventory/{id}", async (string id) => {
    var item = await repo.GetByIdAsync(id);
    return item is not null ? Results.Ok(item) : Results.NotFound();
});
app.MapPost("/inventory", async (InventoryItem item) => {
    await repo.AddAsync(item);
    return Results.Created($"/inventory/{item.Id}", item);
});
app.MapPut("/inventory/{id}", async (string id, InventoryItem updated) => {
    var existing = await repo.GetByIdAsync(id);
    if (existing is null) return Results.NotFound();
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