using order_service.Models;
using order_service.Repositories;
using order_service.Middleware;
using order_service.Data;
using OrderService.Messaging;
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
    Log.Information("Starting order service web application");

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
        Title = "Order Service API",
        Version = "v1",
        Description = "A REST API for managing orders in the Trello system (Version 1.0)"
    });
});
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// Add Service Bus Publisher (optional - only if connection string is configured)
var serviceBusConnectionString = builder.Configuration["ServiceBus:ConnectionString"];
if (!string.IsNullOrEmpty(serviceBusConnectionString))
{
    builder.Services.AddSingleton<IMessagePublisher>(sp =>
    {
        var logger = sp.GetRequiredService<ILogger<ServiceBusPublisher>>();
        return new ServiceBusPublisher(serviceBusConnectionString, logger);
    });

    // Add Product Event Consumer (background service)
    builder.Services.AddHostedService<ProductEventConsumer>(sp =>
    {
        var queueName = builder.Configuration["ServiceBus:ProductsQueue"] ?? "products";
        var logger = sp.GetRequiredService<ILogger<ProductEventConsumer>>();
        return new ProductEventConsumer(serviceBusConnectionString, queueName, logger);
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

// Group order endpoints for v1
var ordersGroupV1 = app.MapGroup("/api/v{version:apiVersion}/orders")
    .WithApiVersionSet(apiVersionSet)
    .WithTags("Orders");

// GET all orders with pagination and status filter
ordersGroupV1.MapGet("", async (IOrderRepository repository, int page = 1, int pageSize = 10, string? status = null) =>
{
    Log.Information("Getting orders: Page={Page}, PageSize={PageSize}, Status={Status}", page, pageSize, status ?? "all");
    
    if (page < 1) page = 1;
    if (pageSize < 1 || pageSize > 100) pageSize = 10;
    
    var pagedOrders = await repository.GetAllAsync(page, pageSize, status);
    Log.Information("Retrieved {Count} orders out of {Total} total", pagedOrders.Items.Count(), pagedOrders.TotalCount);
    
    return Results.Ok(pagedOrders);
})
    .WithName("GetOrders")
    .WithDescription("Retrieves a paginated list of orders. Use 'page', 'pageSize', and 'status' query parameters.")
    .WithSummary("Get all orders (paginated with status filter)")
    .Produces<PagedResult<Order>>(StatusCodes.Status200OK);

// GET order by ID
ordersGroupV1.MapGet("{id:int}", async (int id, IOrderRepository repository) =>
{
    Log.Information("Getting order with ID: {OrderId}", id);
    
    var order = await repository.GetByIdAsync(id);
    
    if (order is not null)
    {
        Log.Information("Order found: {OrderId} - Customer: {CustomerName}", id, order.CustomerName);
        return Results.Ok(order);
    }
    
    Log.Warning("Order not found: {OrderId}", id);
    return Results.NotFound();
})
.WithName("GetOrderById")
.WithDescription("Retrieves a specific order by its unique identifier")
.WithSummary("Get order by ID")
.Produces<Order>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

// POST create order
ordersGroupV1.MapPost("", async (Order order, IOrderRepository repository, IMessagePublisher messagePublisher) =>
{
    Log.Information("Creating new order for customer: {CustomerName}", order.CustomerName);
    
    if (!MiniValidator.TryValidate(order, out var errors))
    {
        Log.Warning("Validation failed for order creation: {CustomerName}, Errors: {@Errors}", order.CustomerName, errors);
        return Results.ValidationProblem(errors);
    }
    
    var createdOrder = await repository.CreateAsync(order);
    Log.Information("Order created successfully: ID={OrderId}, Customer={CustomerName}, Total={TotalAmount}", 
        createdOrder.Id, createdOrder.CustomerName, createdOrder.TotalAmount);
    
    // Publish order created event
    var orderEvent = new OrderService.Messaging.Events.OrderCreatedEvent
    {
        OrderId = createdOrder.Id,
        CustomerName = createdOrder.CustomerName,
        TotalAmount = createdOrder.TotalAmount,
        CreatedAt = DateTime.UtcNow,
        Items = createdOrder.OrderItems.Select(item => new OrderService.Messaging.Events.OrderItemInfo
        {
            ProductId = item.ProductId,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice
        }).ToList()
    };
    await messagePublisher.PublishAsync(builder.Configuration["ServiceBus:OrdersQueue"]!, orderEvent);
    
    // Also publish to topic for notifications
    await messagePublisher.PublishAsync(builder.Configuration["ServiceBus:OrderNotificationsTopic"]!, orderEvent);
    
    return Results.Created($"/api/orders/{createdOrder.Id}", createdOrder);
})
.WithName("CreateOrder")
.WithDescription("Creates a new order with the provided details")
.WithSummary("Create a new order")
.Produces<Order>(StatusCodes.Status201Created)
.ProducesValidationProblem();

// PUT update order
ordersGroupV1.MapPut("{id:int}", async (int id, Order updatedOrder, IOrderRepository repository) =>
{
    Log.Information("Updating order: ID={OrderId}, Customer={CustomerName}", id, updatedOrder.CustomerName);
    
    if (!MiniValidator.TryValidate(updatedOrder, out var errors))
    {
        Log.Warning("Validation failed for order update: ID={OrderId}, Errors: {@Errors}", id, errors);
        return Results.ValidationProblem(errors);
    }
    
    var order = await repository.UpdateAsync(id, updatedOrder);
    
    if (order is not null)
    {
        Log.Information("Order updated successfully: ID={OrderId}, Customer={CustomerName}, Total={TotalAmount}", 
            order.Id, order.CustomerName, order.TotalAmount);
        return Results.Ok(order);
    }
    
    Log.Warning("Order not found for update: ID={OrderId}", id);
    return Results.NotFound();
})
.WithName("UpdateOrder")
.WithDescription("Updates all fields of an existing order")
.WithSummary("Update an existing order")
.Produces<Order>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.ProducesValidationProblem();

// PATCH update order status
ordersGroupV1.MapPatch("{id:int}/status", async (int id, string status, IOrderRepository repository, IMessagePublisher messagePublisher) =>
{
    Log.Information("Updating order status: ID={OrderId}, NewStatus={Status}", id, status);
    
    var (order, oldStatus) = await repository.UpdateStatusAsync(id, status);
    
    if (order is not null)
    {
        Log.Information("Order status updated successfully: ID={OrderId}, OldStatus={OldStatus}, NewStatus={NewStatus}", 
            order.Id, oldStatus, order.Status);
        
        // Publish status change event to topic for notifications
        var statusEvent = new OrderService.Messaging.Events.OrderStatusChangedEvent
        {
            OrderId = order.Id,
            OldStatus = oldStatus ?? "Unknown",
            NewStatus = order.Status,
            ChangedAt = DateTime.UtcNow
        };
        await messagePublisher.PublishAsync(builder.Configuration["ServiceBus:OrderNotificationsTopic"]!, statusEvent);
        
        return Results.Ok(order);
    }
    
    Log.Warning("Order not found for status update: ID={OrderId}", id);
    return Results.NotFound();
})
.WithName("UpdateOrderStatus")
.WithDescription("Updates only the status of an existing order")
.WithSummary("Update order status")
.Produces<Order>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

// DELETE order
ordersGroupV1.MapDelete("{id:int}", async (int id, IOrderRepository repository) =>
{
    Log.Information("Deleting order: ID={OrderId}", id);
    
    var deleted = await repository.DeleteAsync(id);
    
    if (deleted)
    {
        Log.Information("Order deleted successfully: ID={OrderId}", id);
        return Results.NoContent();
    }
    
    Log.Warning("Order not found for deletion: ID={OrderId}", id);
    return Results.NotFound();
})
.WithName("DeleteOrder")
.WithDescription("Permanently deletes an order by its ID")
.WithSummary("Delete an order")
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
    Log.Fatal(ex, "Order service application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
