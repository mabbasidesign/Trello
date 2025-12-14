# Trello Microservices

A production-ready microservices architecture built with .NET 10.0, demonstrating modern cloud-native development practices including event-driven communication, containerization, automated CI/CD, and enterprise-grade security.

## Architecture

This project implements a microservices-based system with two independent services deployed on Azure:

- **Product Service** - Manages product catalog (CRUD operations, search, pagination)
- **Order Service** - Handles orders with items and event-driven product synchronization

### Key Architectural Features

- **Database Per Service** - Each microservice has its own isolated Azure SQL Database (TrelloProducts, TrelloOrders)
- **Event-Driven Communication** - Services communicate via Azure Service Bus using pub/sub pattern
- **API Versioning** - URL-based versioning with Swagger integration (Asp.Versioning 8.1.0)
- **CI/CD Pipeline** - Fully automated Azure DevOps pipeline (build, Docker, infrastructure deployment)
- **Infrastructure as Code** - Complete Azure infrastructure defined in Bicep templates
- **Enterprise Security** - 3-tier secret management (Docker .env, .NET User Secrets, Azure Key Vault)
- **Docker Support** - Full containerization with multi-service Docker Compose
- **Zero-Trust Security** - Managed identity authentication, RBAC, no hardcoded credentials

## Technologies

### Core Framework
- **.NET 10.0** - Minimal APIs with WebApplication
- **C# 12** - Latest language features
- **Entity Framework Core 10.0.1** - Code-first with migrations

### Azure Cloud Services
- **Azure SQL Database** - Managed relational database (S0 tier)
- **Azure Service Bus** - Enterprise messaging (queues and topics)
- **Azure App Services** - Container hosting (B1 Linux)
- **Azure Container Registry** - Private Docker image registry
- **Azure Key Vault** - Centralized secret management with RBAC
- **Azure DevOps** - CI/CD pipelines and Git repositories

### DevOps & Infrastructure
- **Docker** - Containerization and orchestration
- **Azure Bicep** - Infrastructure as Code
- **Azure DevOps Pipelines** - Multi-stage CI/CD automation
- **Git** - Version control with GitHub integration

### Security
- **.env Files** - Docker environment variable management
- **.NET User Secrets** - Encrypted local development secrets
- **Azure Key Vault** - Production secret storage
- **Managed Identity** - Password-less Azure authentication

### NuGet Packages

**API & Versioning:**
- `Asp.Versioning.Http` (8.1.0)
- `Asp.Versioning.Mvc.ApiExplorer` (8.1.0)
- `Microsoft.AspNetCore.OpenApi` (10.0.1)
- `Swashbuckle.AspNetCore` (10.0.1)

**Database:**
- `Microsoft.EntityFrameworkCore.SqlServer` (10.0.1)
- `Microsoft.EntityFrameworkCore.Design` (10.0.1)
- `Microsoft.EntityFrameworkCore.Tools` (10.0.1)

**Messaging:**
- `Azure.Messaging.ServiceBus` (7.20.1)

**Validation & Health:**
- `MiniValidation` (0.9.2)
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

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for local development)
- [Azure Subscription](https://azure.microsoft.com/free/) (for cloud deployment)
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) (optional, for manual deployments)

## Getting Started

### Production Deployment (Azure)

The application is deployed and running on Azure:

- **Product Service API:** https://app-trello-product-prod.azurewebsites.net/swagger
- **Order Service API:** https://app-trello-order-prod.azurewebsites.net/swagger

### Local Development with Docker

1. **Clone the repository**
   ```powershell
   git clone https://github.com/mabbasidesign/Trello.git
   cd Trello
   ```

2. **Create .env file** (copy from template)
   ```powershell
   cp .env.example .env
   ```
   
3. **Update .env with your local credentials:**
   ```env
   SQL_SERVER=sqlserver
   SQL_USER=sa
   SQL_PASSWORD=YourStrong@Passw0rd
   SERVICEBUS_CONNECTION_STRING=<optional-for-local>
   ```

4. **Start infrastructure** (SQL Server, Service Bus emulator - optional)
   ```powershell
   cd docker
   docker-compose -f docker-compose.infrastructure.yml up -d
   ```

5. **Start the services**
   ```powershell
   docker-compose -f docker-compose.product.yml up -d --build
   docker-compose -f docker-compose.order.yml up -d --build
   ```

6. **Access the services**
   - Product Service: http://localhost:5217/swagger
   - Order Service: http://localhost:5037/swagger

### Local Development with .NET

1. **Configure User Secrets** (already initialized)
   ```powershell
   cd src\product-service
   dotnet user-secrets set "ConnectionStrings:DefaultSQLConnection" "Server=localhost;Database=TrelloProducts;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true"
   
   cd ..\order-service
   dotnet user-secrets set "ConnectionStrings:DefaultSQLConnection" "Server=localhost;Database=TrelloOrders;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true"
   ```

2. **Run database migrations**
   ```powershell
   cd src\product-service
   dotnet ef database update
   
   cd ..\order-service
   dotnet ef database update
   ```

3. **Run the services**
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
# Start infrastructure (SQL Server)
cd docker
docker-compose -f docker-compose.infrastructure.yml up -d

# Start product service
docker-compose -f docker-compose.product.yml up -d --build

# Start order service
docker-compose -f docker-compose.order.yml up -d --build

# View logs
docker logs product-service -f
docker logs order-service -f

# Stop all services
docker-compose -f docker-compose.product.yml down
docker-compose -f docker-compose.order.yml down
docker-compose -f docker-compose.infrastructure.yml down
```

## Security Architecture

This project implements a **3-tier security model** following Azure best practices:

### 1. Docker Development (.env files)
- **Location:** `Trello/.env` (gitignored)
- **Usage:** Docker Compose environment variables
- **Secrets:** SQL passwords, Service Bus connection strings
- **Template:** `.env.example` provided for developers

### 2. .NET Local Development (User Secrets)
- **Location:** `%APPDATA%\Microsoft\UserSecrets\{id}\secrets.json`
- **Usage:** Local .NET development (encrypted, outside repository)
- **Configuration:** Already initialized in both `.csproj` files
- **Secrets:** Database connection strings

### 3. Azure Production (Key Vault)
- **Resource:** `kv-Trello-key`
- **Authentication:** Managed Identity (password-less)
- **Access Control:** RBAC with audit logging
- **Secrets:** SQL passwords, Service Bus connection strings
- **Integration:** App Services configured to reference Key Vault

**Security Features:**
- ✅ No hardcoded passwords in source code
- ✅ All sensitive files excluded from Git (.gitignore)
- ✅ Managed identity for Azure resource authentication
- ✅ RBAC-based access control
- ✅ Centralized secret rotation capability
- ✅ Audit logs for secret access

## CI/CD Pipeline

Fully automated Azure DevOps pipeline with 3 stages:

### Stage 1: Build
- Restore NuGet packages
- Compile .NET projects
- Run unit tests (when available)
- Validate code quality

### Stage 2: BuildDockerImages
- Build Docker images for both services
- Tag with build number
- Push to Azure Container Registry (trelloacr.azurecr.io)
- Clean up old images

### Stage 3: DeployInfrastructure
- Deploy Bicep templates to Azure
- Provision/update Azure resources:
  - Service Bus namespace, queues, topics
  - SQL Server and databases
  - App Services
  - Key Vault (optional)
- Deploy container images to App Services
- Verify deployment health

**Pipeline Triggers:**
- Automatic on push to `main` or `develop` branch
- Path filters: `src/**`, `docker/**`, `infra/**`, `azure-pipelines.yml`

**View Pipeline:** [Azure DevOps Pipelines](https://dev.azure.com)

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

## Azure Resources

The application is deployed with the following Azure resources:

### Resource Group: rg-trello-microservices
- **Location:** Canada East (primary), West US 2 (database)
- **Subscription:** f2f7f036-0d40-48ca-a532-eb590c2eac8b

### Compute
- **App Service Plan:** B1 Linux (1 vCPU, 1.75 GB RAM)
- **App Services:**
  - `app-trello-product-prod` (Product microservice)
  - `app-trello-order-prod` (Order microservice)
- **Container Registry:** trelloacr.azurecr.io (Standard SKU)

### Data
- **SQL Server:** sql-trello-ziutkivc6smnk.database.windows.net (West US 2)
  - **Databases:** TrelloProducts (S0 tier), TrelloOrders (S0 tier)
  - **Admin User:** sqladmin
  - **Firewall:** Whitelisted IPs + Azure services

### Messaging
- **Service Bus Namespace:** trello-servicebus-fumbtexxi35ii (Canada East)
  - **Queues:** products, orders
  - **Topic:** order-notifications (with subscriptions)

### Security
- **Key Vault:** kv-Trello-key
  - **Secrets:** sql-admin-password, servicebus-connection-string
  - **Access:** Managed identity with RBAC
  - **Audit:** All secret access logged

### DevOps
- **Azure DevOps:** CI/CD pipeline with automated deployment
- **Git Repository:** GitHub integration

## Configuration

### Environment Variables

Both services support configuration via environment variables (Docker) or User Secrets (.NET):

**Required:**
- `ConnectionStrings__DefaultSQLConnection` - Database connection string

**Optional:**
- `ServiceBus__ConnectionString` - Azure Service Bus connection string (for messaging)
- `ASPNETCORE_ENVIRONMENT` - Development/Production

**Example (Docker .env):**
```env
SQL_SERVER=sqlserver
SQL_USER=sa
SQL_PASSWORD=YourStrong@Passw0rd
SERVICEBUS_CONNECTION_STRING=Endpoint=sb://...
ASPNETCORE_ENVIRONMENT=Development
```

**Example (Azure Key Vault Reference):**
```
ConnectionStrings__DefaultSQLConnection=Server=sql-trello-ziutkivc6smnk.database.windows.net;Database=TrelloProducts;User Id=sqladmin;Password=@Microsoft.KeyVault(VaultName=kv-Trello-key;SecretName=sql-admin-password);TrustServerCertificate=true
```

## Azure Deployment

The infrastructure is fully automated through Azure DevOps pipelines. For manual deployment:

### Prerequisites
- Azure CLI installed and logged in (`az login`)
- Contributor access to Azure subscription
- Azure DevOps project configured

### Automatic Deployment (Recommended)
1. Push code to `main` branch
2. Pipeline automatically triggers
3. Infrastructure deployed via Bicep
4. Docker images built and pushed to ACR
5. App Services updated with new images

### Manual Infrastructure Deployment
```powershell
# Deploy Service Bus
az deployment group create \
  --resource-group rg-trello-microservices \
  --template-file infra/servicebus.bicep

# Deploy SQL Server
az deployment group create \
  --resource-group rg-trello-microservices \
  --template-file infra/sqlserver.bicep

# Deploy App Services
az deployment group create \
  --resource-group rg-trello-microservices \
  --template-file infra/appservices.bicep

# Deploy Key Vault (optional)
az deployment group create \
  --resource-group rg-trello-microservices \
  --template-file infra/keyvault.bicep \
  --parameters sqlAdminPassword='***' serviceBusConnectionString='***'
```

See [infra/README.md](infra/README.md) for detailed deployment guide.

## Monitoring & Health Checks

### Health Endpoints
Each service exposes health check endpoints:

- `/health` - Overall health (all checks)
- `/health/ready` - Readiness probe (database connectivity)
- `/health/live` - Liveness probe (basic service availability)

### Logs
- **Azure App Service:** Navigate to App Service → Monitoring → Log stream
- **Local Docker:** `docker logs <container-name> -f`
- **Application Logs:** Console output with structured information

### Application Insights (Future)
- Performance metrics
- Request tracking
- Exception monitoring
- Custom telemetry

## Testing the APIs

### Production APIs (Azure)
- Product Service: https://app-trello-product-prod.azurewebsites.net/swagger
- Order Service: https://app-trello-order-prod.azurewebsites.net/swagger

### Local Development
Use Swagger UI at http://localhost:5217/swagger and http://localhost:5037/swagger

### Example API Calls

**Create a Product:**
```powershell
curl -X POST https://app-trello-product-prod.azurewebsites.net/api/v1/products `
  -H "Content-Type: application/json" `
  -d '{"name":"Laptop","description":"High-performance laptop","price":999.99}'
```

**Get All Products:**
```powershell
curl https://app-trello-product-prod.azurewebsites.net/api/v1/products
```

**Create an Order:**
```powershell
curl -X POST https://app-trello-order-prod.azurewebsites.net/api/v1/orders `
  -H "Content-Type: application/json" `
  -d '{
    "items": [
      {"productId":1,"productName":"Laptop","quantity":1,"unitPrice":999.99}
    ]
  }'
```

**Update Order Status:**
```powershell
curl -X PATCH https://app-trello-order-prod.azurewebsites.net/api/v1/orders/1/status?newStatus=Processing
```
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
