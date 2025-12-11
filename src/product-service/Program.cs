using product_service.Models;
using product_service.Repositories;
using product_service.Middleware;
using MiniValidation;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddSingleton<IProductRepository, InMemoryProductRepository>();

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

// GET all products with pagination and search
app.MapGet("/api/products", async (IProductRepository repository, int page = 1, int pageSize = 10, string? search = null) =>
{
    if (page < 1) page = 1;
    if (pageSize < 1 || pageSize > 100) pageSize = 10;
    
    var pagedProducts = await repository.GetAllAsync(page, pageSize, search);
    return Results.Ok(pagedProducts);
})
    .WithName("GetProducts")
    .WithDescription("Retrieves a paginated list of products. Use 'page', 'pageSize', and 'search' query parameters.")
    .WithSummary("Get all products (paginated with search)")
    .WithTags("Products")
    .Produces<PagedResult<Product>>(StatusCodes.Status200OK);

// GET product by ID
app.MapGet("/api/products/{id}", async (int id, IProductRepository repository) =>
{
    var product = await repository.GetByIdAsync(id);
    return product is not null ? Results.Ok(product) : Results.NotFound();
})
.WithName("GetProductById")
.WithDescription("Retrieves a specific product by its unique identifier")
.WithSummary("Get product by ID")
.WithTags("Products")
.Produces<Product>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

// POST create product
app.MapPost("/api/products", async (Product product, IProductRepository repository) =>
{
    if (!MiniValidator.TryValidate(product, out var errors))
        return Results.ValidationProblem(errors);
    
    var createdProduct = await repository.CreateAsync(product);
    return Results.Created($"/api/products/{createdProduct.Id}", createdProduct);
})
.WithName("CreateProduct")
.WithDescription("Creates a new product with the provided details")
.WithSummary("Create a new product")
.WithTags("Products")
.Produces<Product>(StatusCodes.Status201Created)
.ProducesValidationProblem();

// PUT update product
app.MapPut("/api/products/{id}", async (int id, Product updatedProduct, IProductRepository repository) =>
{
    if (!MiniValidator.TryValidate(updatedProduct, out var errors))
        return Results.ValidationProblem(errors);
    
    var product = await repository.UpdateAsync(id, updatedProduct);
    return product is not null ? Results.Ok(product) : Results.NotFound();
})
.WithName("UpdateProduct")
.WithDescription("Updates all fields of an existing product")
.WithSummary("Update an existing product")
.WithTags("Products")
.Produces<Product>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.ProducesValidationProblem();

// DELETE product
app.MapDelete("/api/products/{id}", async (int id, IProductRepository repository) =>
{
    var deleted = await repository.DeleteAsync(id);
    return deleted ? Results.NoContent() : Results.NotFound();
})
.WithName("DeleteProduct")
.WithDescription("Permanently deletes a product by its ID")
.WithSummary("Delete a product")
.WithTags("Products")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound);

app.Run();
