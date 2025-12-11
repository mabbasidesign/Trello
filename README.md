# Trello Microservices

A production-ready microservices architecture built with .NET 10.0, demonstrating modern cloud-native development practices including event-driven communication, containerization, and infrastructure as code.

##  Architecture

This project implements a microservices-based e-commerce system with two independent services:

- **Product Service** - Manages product catalog (CRUD operations, search, pagination)
- **Order Service** - Handles orders with items and status workflow

### Key Architectural Features

- **Database Per Service** - Each microservice has its own isolated database (TrelloProducts, TrelloOrders)
- **Event-Driven Communication** - Services communicate via Azure Service Bus using pub/sub pattern
- **API Versioning** - Support for URL segment, header, and query string versioning
- **Health Checks** - Comprehensive health monitoring endpoints for Kubernetes/container orchestration
- **Structured Logging** - Serilog with console and file sinks
- **Docker Support** - Full containerization with Docker Compose
- **Infrastructure as Code** - Azure deployment templates using Bicep

## Technologies

- **.NET 10.0** - Minimal APIs with WebApplication
- **Entity Framework Core 10.0.1** - Code-first with SQL Server
- **SQL Server 2022** - Database (Docker container)
- **Serilog 10.0.0** - Structured logging
- **Swashbuckle/OpenAPI** - API documentation
- **Azure Service Bus 7.20.1** - Async messaging (optional)
- **Docker & Docker Compose** - Containerization
- **Azure Bicep** - Infrastructure as Code

### NuGet Packages

- `Microsoft.EntityFrameworkCore.SqlServer` (10.0.1)
- `Microsoft.EntityFrameworkCore.Design` (10.0.1)
- `Serilog.AspNetCore` (10.0.0)
- `Serilog.Sinks.File` (6.0.0)
- `Asp.Versioning.Http` (8.1.0)
- `MiniValidation` (0.9.2)
- `Azure.Messaging.ServiceBus` (7.20.1)
- `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` (10.0.1)

## Features

### Product Service (Port 5217)

- Full CRUD operations for products
- Pagination and search functionality
- Publishes events (ProductCreated, ProductUpdated, ProductDeleted)
- Repository pattern with dependency injection
- Input validation with MiniValidation
- Global exception handling

### Order Service (Port 5037)

- Create orders with multiple items
- Order status workflow (Pending → Processing → Shipped → Delivered → Cancelled)
- Consumes product events from Product Service
- Publishes order events (OrderCreated, OrderStatusChanged)
- Background service for event processing

### Common Features

- API versioning (v1)
- Swagger/OpenAPI documentation
- Health check endpoints (/health, /health/ready, /health/live)
- CORS enabled
- DTOs for clean API contracts
- Structured logging with Serilog

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (with Linux containers)
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) (optional, for Azure deployment)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (or use Docker)

## Getting Started

### Option 1: Docker (Recommended)

1. **Clone the repository**
   ```powershell
   git clone https://github.com/mabbasidesign/Trello.git
   cd Trello\docker
   ```

2. **Start all services**
   ```powershell
   docker-compose up -d --build
   ```

3. **Access the services**
   - Product Service: http://localhost:5217/swagger
   - Order Service: http://localhost:5037/swagger
   - Product Health: http://localhost:5217/health
   - Order Health: http://localhost:5037/health

### Option 2: Local Development

1. **Clone the repository**
   ```powershell
   git clone https://github.com/mabbasidesign/Trello.git
   cd Trello
   ```

2. **Update connection strings** in `appsettings.Development.json` for both services

3. **Run database migrations**
   ```powershell
   cd src\product-service
   dotnet ef database update
   
   cd ..\order-service
   dotnet ef database update
   ```

4. **Run the services**
   ```powershell
   # Terminal 1 - Product Service
   cd src\product-service
   dotnet run
   
   # Terminal 2 - Order Service
   cd src\order-service
   dotnet run
   ```

## Docker Commands

See [docker/DOCKER-COMMANDS.md](docker/DOCKER-COMMANDS.md) for comprehensive Docker usage guide.

**Quick Reference:**
```powershell
# Start all services
cd docker
docker-compose up -d --build

# View logs
docker logs product-service -f
docker logs order-service -f

# Stop all services
docker-compose down

# Rebuild after code changes
docker-compose up -d --build
```

## API Documentation

### Product Service Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/products` | Get all products (paginated) |
| GET | `/api/v1/products/{id}` | Get product by ID |
| GET | `/api/v1/products/search` | Search products by name |
| POST | `/api/v1/products` | Create new product |
| PUT | `/api/v1/products/{id}` | Update product |
| DELETE | `/api/v1/products/{id}` | Delete product |
| GET | `/health` | Health check (all) |
| GET | `/health/ready` | Readiness check |
| GET | `/health/live` | Liveness check |

### Order Service Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/orders` | Get all orders (paginated) |
| GET | `/api/v1/orders/{id}` | Get order by ID |
| POST | `/api/v1/orders` | Create new order |
| PATCH | `/api/v1/orders/{id}/status` | Update order status |
| GET | `/health` | Health check (all) |
| GET | `/health/ready` | Readiness check |
| GET | `/health/live` | Liveness check |

**Interactive API Documentation:** Visit `/swagger` on each service for full OpenAPI documentation.

## Project Structure

```
Trello/
├── src/
│   ├── product-service/
│   │   ├── Data/                    # DbContext and migrations
│   │   ├── DTOs/                    # Data Transfer Objects
│   │   ├── Messaging/               # Service Bus publishers/events
│   │   ├── Middleware/              # Global exception handler
│   │   ├── Models/                  # Domain entities
│   │   ├── Repositories/            # Data access layer
│   │   ├── Program.cs               # Application entry point
│   │   ├── appsettings.json         # Configuration
│   │   └── Dockerfile               # Multi-stage Docker build
│   │
│   └── order-service/
│       ├── Data/                    # DbContext and migrations
│       ├── DTOs/                    # Data Transfer Objects
│       ├── Messaging/               # Service Bus publishers/consumers/events
│       ├── Middleware/              # Global exception handler
│       ├── Models/                  # Domain entities
│       ├── Repositories/            # Data access layer
│       ├── Program.cs               # Application entry point
│       ├── appsettings.json         # Configuration
│       └── Dockerfile               # Multi-stage Docker build
│
├── docker/
│   ├── docker-compose.yml                    # All-in-one compose
│   ├── docker-compose.infrastructure.yml     # SQL Server only
│   ├── docker-compose.product.yml            # Product service
│   ├── docker-compose.order.yml              # Order service
│   ├── README.md                             # Docker documentation
│   └── DOCKER-COMMANDS.md                    # Docker command reference
│
├── infra/
│   ├── main.bicep               # Main deployment template
│   ├── servicebus.bicep         # Service Bus resources
│   └── README.md                # Azure deployment guide
│
└── README.md                    # This file
```

## Database Schema

### Product Service (TrelloProducts)

**Products Table:**
- Id (int, PK)
- Name (nvarchar(100), required)
- Description (nvarchar(500))
- Price (decimal(18,2), required)
- CreatedDate (datetime2)
- UpdatedDate (datetime2)

### Order Service (TrelloOrders)

**Orders Table:**
- Id (int, PK)
- OrderDate (datetime2)
- Status (nvarchar(50))
- TotalAmount (decimal(18,2))
- CreatedDate (datetime2)
- UpdatedDate (datetime2)

**OrderItems Table:**
- Id (int, PK)
- OrderId (int, FK)
- ProductId (int)
- ProductName (nvarchar(100))
- Quantity (int)
- UnitPrice (decimal(18,2))
- TotalPrice (decimal(18,2))

## 🔧 Configuration

### Service Bus (Optional)

The application works without Azure Service Bus by using a NullMessagePublisher pattern. To enable Azure Service Bus:

1. Deploy the infrastructure:
   ```powershell
   az deployment sub create --location eastus --template-file infra/main.bicep --parameters environment=dev
   ```

2. Update `appsettings.json` in both services:
   ```json
   {
     "ServiceBus": {
       "ConnectionString": "your-connection-string",
       "ProductQueueName": "products",
       "OrderQueueName": "orders",
       "OrderNotificationTopicName": "order-notifications"
     }
   }
   ```

### Environment Variables (Docker)

Both services support configuration via environment variables:

- `ConnectionStrings__DefaultSQLConnection` - SQL Server connection string
- `ServiceBus__ConnectionString` - Azure Service Bus connection string
- `ServiceBus__ProductQueueName` - Product queue name
- `ServiceBus__OrderQueueName` - Order queue name
- `ServiceBus__OrderNotificationTopicName` - Order notifications topic

## Azure Deployment

See [infra/README.md](infra/README.md) for detailed Azure deployment instructions.

**Quick Deploy:**
```powershell
# Login to Azure
az login

# Deploy infrastructure
az deployment sub create \
  --location eastus \
  --template-file infra/main.bicep \
  --parameters environment=dev

# Get connection string from output and update appsettings.json
```

##  Logging

Logs are written to:
- **Console** - Colored output for development
- **Files** - `logs/log-YYYYMMDD.txt` (daily rolling)

Log levels: Information, Warning, Error

## Health Checks

Each service exposes three health endpoints:

- `/health` - Overall health (all checks)
- `/health/ready` - Readiness probe (database connectivity)
- `/health/live` - Liveness probe (basic service availability)

Useful for Kubernetes/container orchestration.

##  Testing the APIs

### Create a Product
```powershell
curl -X POST http://localhost:5217/api/v1/products `
  -H "Content-Type: application/json" `
  -d '{"name":"Laptop","description":"High-performance laptop","price":999.99}'
```

### Create an Order
```powershell
curl -X POST http://localhost:5037/api/v1/orders `
  -H "Content-Type: application/json" `
  -d '{"orderItems":[{"productId":1,"productName":"Laptop","quantity":2,"unitPrice":999.99}]}'
```

### Update Order Status
```powershell
curl -X PATCH http://localhost:5037/api/v1/orders/1/status?newStatus=Processing
```

##  Development

### Adding Database Migrations

```powershell
# Product Service
cd src\product-service
dotnet ef migrations add MigrationName
dotnet ef database update

# Order Service
cd src\order-service
dotnet ef migrations add MigrationName
dotnet ef database update
```

### Running Tests
```powershell
dotnet test
```

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

##  License

This project is licensed under the MIT License.

## Author

**mabbasidesign**
- GitHub: [@mabbasidesign](https://github.com/mabbasidesign)

## Acknowledgments

- Built with .NET 10.0 and modern microservices patterns
- Inspired by cloud-native architecture best practices
- Uses Azure Service Bus for reliable messaging

---

** If you find this project useful, please give it a star!**
