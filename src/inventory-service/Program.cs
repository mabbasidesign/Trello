using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var inventory = new List<InventoryItem>();

app.MapGet("/inventory", () => inventory);
app.MapGet("/inventory/{id}", (string id) => inventory.FirstOrDefault(i => i.Id == id) is InventoryItem item ? Results.Ok(item) : Results.NotFound());
app.MapPost("/inventory", (InventoryItem item) => { inventory.Add(item); return Results.Created($"/inventory/{item.Id}", item); });
app.MapPut("/inventory/{id}", (string id, InventoryItem updated) => {
    var item = inventory.FirstOrDefault(i => i.Id == id);
    if (item is null) return Results.NotFound();
    item.ProductName = updated.ProductName;
    item.Quantity = updated.Quantity;
    item.Location = updated.Location;
    return Results.NoContent();
});
app.MapDelete("/inventory/{id}", (string id) => {
    var item = inventory.FirstOrDefault(i => i.Id == id);
    if (item is null) return Results.NotFound();
    inventory.Remove(item);
    return Results.NoContent();
});

app.Run();

record InventoryItem(string Id, string ProductName, int Quantity, string Location);