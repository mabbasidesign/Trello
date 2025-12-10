using product_service.Models;
using MiniValidation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// In-memory product list for demo
var products = new List<Product>
{
    new() { Id = 1, Name = "Laptop", Description = "High-performance laptop", Price = 999.99m, Stock = 10, CreatedAt = DateTime.UtcNow },
    new() { Id = 2, Name = "Mouse", Description = "Wireless mouse", Price = 29.99m, Stock = 50, CreatedAt = DateTime.UtcNow },
    new() { Id = 3, Name = "Keyboard", Description = "Mechanical keyboard", Price = 79.99m, Stock = 25, CreatedAt = DateTime.UtcNow }
};

// GET all products
app.MapGet("/api/products", () => Results.Ok(products))
    .WithName("GetProducts")
    .WithTags("Products");

// GET product by ID
app.MapGet("/api/products/{id}", (int id) =>
{
    var product = products.FirstOrDefault(p => p.Id == id);
    return product is not null ? Results.Ok(product) : Results.NotFound();
})
.WithName("GetProductById")
.WithTags("Products");

// POST create product
app.MapPost("/api/products", (Product product) =>
{
    if (!MiniValidator.TryValidate(product, out var errors))
        return Results.ValidationProblem(errors);
    
    product.Id = products.Any() ? products.Max(p => p.Id) + 1 : 1;
    product.CreatedAt = DateTime.UtcNow;
    products.Add(product);
    return Results.Created($"/api/products/{product.Id}", product);
})
.WithName("CreateProduct")
.WithTags("Products");

// PUT update product
app.MapPut("/api/products/{id}", (int id, Product updatedProduct) =>
{
    if (!MiniValidator.TryValidate(updatedProduct, out var errors))
        return Results.ValidationProblem(errors);
    
    var product = products.FirstOrDefault(p => p.Id == id);
    if (product is null) return Results.NotFound();

    product.Name = updatedProduct.Name;
    product.Description = updatedProduct.Description;
    product.Price = updatedProduct.Price;
    product.Stock = updatedProduct.Stock;
    product.UpdatedAt = DateTime.UtcNow;

    return Results.Ok(product);
})
.WithName("UpdateProduct")
.WithTags("Products");

// DELETE product
app.MapDelete("/api/products/{id}", (int id) =>
{
    var product = products.FirstOrDefault(p => p.Id == id);
    if (product is null) return Results.NotFound();

    products.Remove(product);
    return Results.NoContent();
})
.WithName("DeleteProduct")
.WithTags("Products");

app.Run();
