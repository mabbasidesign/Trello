using product_service.Models;
using product_service.Repositories;
using MiniValidation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IProductRepository, InMemoryProductRepository>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");
app.UseHttpsRedirection();

// GET all products
app.MapGet("/api/products", async (IProductRepository repository) =>
{
    var products = await repository.GetAllAsync();
    return Results.Ok(products);
})
    .WithName("GetProducts")
    .WithTags("Products");

// GET product by ID
app.MapGet("/api/products/{id}", async (int id, IProductRepository repository) =>
{
    var product = await repository.GetByIdAsync(id);
    return product is not null ? Results.Ok(product) : Results.NotFound();
})
.WithName("GetProductById")
.WithTags("Products");

// POST create product
app.MapPost("/api/products", async (Product product, IProductRepository repository) =>
{
    if (!MiniValidator.TryValidate(product, out var errors))
        return Results.ValidationProblem(errors);
    
    var createdProduct = await repository.CreateAsync(product);
    return Results.Created($"/api/products/{createdProduct.Id}", createdProduct);
})
.WithName("CreateProduct")
.WithTags("Products");

// PUT update product
app.MapPut("/api/products/{id}", async (int id, Product updatedProduct, IProductRepository repository) =>
{
    if (!MiniValidator.TryValidate(updatedProduct, out var errors))
        return Results.ValidationProblem(errors);
    
    var product = await repository.UpdateAsync(id, updatedProduct);
    return product is not null ? Results.Ok(product) : Results.NotFound();
})
.WithName("UpdateProduct")
.WithTags("Products");

// DELETE product
app.MapDelete("/api/products/{id}", async (int id, IProductRepository repository) =>
{
    var deleted = await repository.DeleteAsync(id);
    return deleted ? Results.NoContent() : Results.NotFound();
})
.WithName("DeleteProduct")
.WithTags("Products");

app.Run();
