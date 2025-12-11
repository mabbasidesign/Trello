using product_service.Models;
using product_service.Repositories;
using product_service.Middleware;
using product_service.Data;
using MiniValidation;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

try
{
    Log.Information("Starting web application");

// Add Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultSQLConnection")));

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Product Service API",
        Version = "v1",
        Description = "A REST API for managing products in the Trello system"
    });
});
builder.Services.AddScoped<IProductRepository, ProductRepository>();

// Add global exception handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

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
app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");
app.UseHttpsRedirection();

// Group product endpoints
var productsGroup = app.MapGroup("/api/products")
    .WithTags("Products");

// GET all products with pagination and search
productsGroup.MapGet("", async (IProductRepository repository, int page = 1, int pageSize = 10, string? search = null) =>
{
    Log.Information("Getting products: Page={Page}, PageSize={PageSize}, Search={Search}", page, pageSize, search ?? "none");
    
    if (page < 1) page = 1;
    if (pageSize < 1 || pageSize > 100) pageSize = 10;
    
    var pagedProducts = await repository.GetAllAsync(page, pageSize, search);
    Log.Information("Retrieved {Count} products out of {Total} total", pagedProducts.Items.Count(), pagedProducts.TotalCount);
    
    return Results.Ok(pagedProducts);
})
    .WithName("GetProducts")
    .WithDescription("Retrieves a paginated list of products. Use 'page', 'pageSize', and 'search' query parameters.")
    .WithSummary("Get all products (paginated with search)")
    .Produces<PagedResult<Product>>(StatusCodes.Status200OK);

// GET product by ID
productsGroup.MapGet("{id:int}", async (int id, IProductRepository repository) =>
{
    Log.Information("Getting product with ID: {ProductId}", id);
    
    var product = await repository.GetByIdAsync(id);
    
    if (product is not null)
    {
        Log.Information("Product found: {ProductId} - {ProductName}", id, product.Name);
        return Results.Ok(product);
    }
    
    Log.Warning("Product not found: {ProductId}", id);
    return Results.NotFound();
})
.WithName("GetProductById")
.WithDescription("Retrieves a specific product by its unique identifier")
.WithSummary("Get product by ID")
.Produces<Product>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

// POST create product
productsGroup.MapPost("", async (Product product, IProductRepository repository) =>
{
    Log.Information("Creating new product: {ProductName}", product.Name);
    
    if (!MiniValidator.TryValidate(product, out var errors))
    {
        Log.Warning("Validation failed for product creation: {ProductName}, Errors: {@Errors}", product.Name, errors);
        return Results.ValidationProblem(errors);
    }
    
    var createdProduct = await repository.CreateAsync(product);
    Log.Information("Product created successfully: ID={ProductId}, Name={ProductName}, Price={Price}", 
        createdProduct.Id, createdProduct.Name, createdProduct.Price);
    
    return Results.Created($"/api/products/{createdProduct.Id}", createdProduct);
})
.WithName("CreateProduct")
.WithDescription("Creates a new product with the provided details")
.WithSummary("Create a new product")
.Produces<Product>(StatusCodes.Status201Created)
.ProducesValidationProblem();

// PUT update product
productsGroup.MapPut("{id:int}", async (int id, Product updatedProduct, IProductRepository repository) =>
{
    Log.Information("Updating product: ID={ProductId}, Name={ProductName}", id, updatedProduct.Name);
    
    if (!MiniValidator.TryValidate(updatedProduct, out var errors))
    {
        Log.Warning("Validation failed for product update: ID={ProductId}, Errors: {@Errors}", id, errors);
        return Results.ValidationProblem(errors);
    }
    
    var product = await repository.UpdateAsync(id, updatedProduct);
    
    if (product is not null)
    {
        Log.Information("Product updated successfully: ID={ProductId}, Name={ProductName}, Price={Price}", 
            product.Id, product.Name, product.Price);
        return Results.Ok(product);
    }
    
    Log.Warning("Product not found for update: ID={ProductId}", id);
    return Results.NotFound();
})
.WithName("UpdateProduct")
.WithDescription("Updates all fields of an existing product")
.WithSummary("Update an existing product")
.Produces<Product>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.ProducesValidationProblem();

// DELETE product
productsGroup.MapDelete("{id:int}", async (int id, IProductRepository repository) =>
{
    Log.Information("Deleting product: ID={ProductId}", id);
    
    var deleted = await repository.DeleteAsync(id);
    
    if (deleted)
    {
        Log.Information("Product deleted successfully: ID={ProductId}", id);
        return Results.NoContent();
    }
    
    Log.Warning("Product not found for deletion: ID={ProductId}", id);
    return Results.NotFound();
})
.WithName("DeleteProduct")
.WithDescription("Permanently deletes a product by its ID")
.WithSummary("Delete a product")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
