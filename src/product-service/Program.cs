using product_service.Models;
using product_service.Repositories;
using product_service.Middleware;
using product_service.Data;
using ProductService.Messaging;
using MiniValidation;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Asp.Versioning;
using Asp.Versioning.Builder;

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

// Add API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version"),
        new QueryStringApiVersionReader("api-version")
    );
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Product Service API",
        Version = "v1",
        Description = "A REST API for managing products in the Trello system (Version 1.0)"
    });
});
builder.Services.AddScoped<IProductRepository, ProductRepository>();

// Add Service Bus Publisher (optional - only if connection string is configured)
var serviceBusConnectionString = builder.Configuration["ServiceBus:ConnectionString"];
if (!string.IsNullOrEmpty(serviceBusConnectionString))
{
    builder.Services.AddSingleton<IMessagePublisher>(sp =>
    {
        var logger = sp.GetRequiredService<ILogger<ServiceBusPublisher>>();
        return new ServiceBusPublisher(serviceBusConnectionString, logger);
    });
    Log.Information("Service Bus messaging enabled");
}
else
{
    builder.Services.AddSingleton<IMessagePublisher, NullMessagePublisher>();
    Log.Warning("Service Bus not configured - messages will not be published");
}

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

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database", tags: new[] { "db", "sql" });

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");
app.UseHttpsRedirection();

// API Versioning
var apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1, 0))
    .ReportApiVersions()
    .Build();

// Group product endpoints for v1
var productsGroupV1 = app.MapGroup("/api/v{version:apiVersion}/products")
    .WithApiVersionSet(apiVersionSet)
    .WithTags("Products");

// GET all products with pagination and search
productsGroupV1.MapGet("", async (IProductRepository repository, int page = 1, int pageSize = 10, string? search = null) =>
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
productsGroupV1.MapGet("{id:int}", async (int id, IProductRepository repository) =>
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
productsGroupV1.MapPost("", async (Product product, IProductRepository repository, IMessagePublisher messagePublisher) =>
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
    
    // Publish product created event
    var productEvent = new ProductService.Messaging.Events.ProductCreatedEvent
    {
        ProductId = createdProduct.Id,
        Name = createdProduct.Name,
        Price = createdProduct.Price,
        CreatedAt = DateTime.UtcNow
    };
    await messagePublisher.PublishAsync(builder.Configuration["ServiceBus:ProductsQueue"]!, productEvent);
    
    return Results.Created($"/api/products/{createdProduct.Id}", createdProduct);
})
.WithName("CreateProduct")
.WithDescription("Creates a new product with the provided details")
.WithSummary("Create a new product")
.Produces<Product>(StatusCodes.Status201Created)
.ProducesValidationProblem();

// PUT update product
productsGroupV1.MapPut("{id:int}", async (int id, Product updatedProduct, IProductRepository repository, IMessagePublisher messagePublisher) =>
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
        
        // Publish product updated event
        var productEvent = new ProductService.Messaging.Events.ProductUpdatedEvent
        {
            ProductId = product.Id,
            Name = product.Name,
            Price = product.Price,
            UpdatedAt = DateTime.UtcNow
        };
        await messagePublisher.PublishAsync(builder.Configuration["ServiceBus:ProductsQueue"]!, productEvent);
        
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
productsGroupV1.MapDelete("{id:int}", async (int id, IProductRepository repository, IMessagePublisher messagePublisher) =>
{
    Log.Information("Deleting product: ID={ProductId}", id);
    
    var deleted = await repository.DeleteAsync(id);
    
    if (deleted)
    {
        Log.Information("Product deleted successfully: ID={ProductId}", id);
        
        // Publish product deleted event
        var productEvent = new ProductService.Messaging.Events.ProductDeletedEvent
        {
            ProductId = id,
            DeletedAt = DateTime.UtcNow
        };
        await messagePublisher.PublishAsync(builder.Configuration["ServiceBus:ProductsQueue"]!, productEvent);
        
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

// Health check endpoints
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = async (context, report) =>
    {
        Log.Information("Health check requested. Status: {Status}", report.Status);
        
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        });
        await context.Response.WriteAsync(result);
    }
})
.WithName("HealthCheck")
.WithDescription("Returns the health status of the application and its dependencies")
.WithSummary("Health check endpoint")
.WithTags("Health");

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("db")
})
.WithName("ReadinessCheck")
.WithDescription("Returns readiness status (database connectivity)")
.WithSummary("Readiness check")
.WithTags("Health");

app.MapHealthChecks("/health/live")
    .WithName("LivenessCheck")
    .WithDescription("Returns liveness status (application is running)")
    .WithSummary("Liveness check")
    .WithTags("Health");

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
