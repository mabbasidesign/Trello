# Docker Setup for Trello Microservices

## Services Included
- **SQL Server 2022** - Database for both microservices
- **Product Service** - Manages products (Port 5217)
- **Order Service** - Manages orders (Port 5037)

## Prerequisites
- Docker Desktop installed and running
- At least 4GB RAM allocated to Docker

## Docker Compose Files

- `docker-compose.infrastructure.yml` - SQL Server database
- `docker-compose.product.yml` - Product Service
- `docker-compose.order.yml` - Order Service
- `docker-compose.yml` - All services together (optional)

## Quick Start

### Option 1: Run All Services Together
```powershell
cd docker
docker-compose up -d --build
```

### Option 2: Run Services Separately

**Step 1: Start Infrastructure (SQL Server)**
```powershell
cd docker
docker-compose -f docker-compose.infrastructure.yml up -d
```

**Step 2: Start Product Service**
```powershell
docker-compose -f docker-compose.product.yml up -d --build
```

**Step 3: Start Order Service**
```powershell
docker-compose -f docker-compose.order.yml up -d --build
```

### Check Running Containers
```powershell
docker ps
```

### View Logs
```powershell
# Infrastructure
docker-compose -f docker-compose.infrastructure.yml logs -f

# Product Service
docker-compose -f docker-compose.product.yml logs -f

# Order Service
docker-compose -f docker-compose.order.yml logs -f

# Specific container
docker logs -f product-service
docker logs -f order-service
docker logs -f trello-sqlserver
```

## Apply Database Migrations

After containers are running, apply EF Core migrations:

```powershell
# Product Service
docker exec -it product-service dotnet ef database update

# Order Service
docker exec -it order-service dotnet ef database update
```

Or connect to SQL Server directly:
```
Server: localhost,1433
User: sa
Password: YourStrong@Passw0rd
```

## Access Services

- **Product Service API**: http://localhost:5217/swagger
- **Order Service API**: http://localhost:5037/swagger
- **Product Service Health**: http://localhost:5217/health
- **Order Service Health**: http://localhost:5037/health

## Azure Service Bus Integration

To enable messaging between services:

1. Set the Service Bus connection string:
```powershell
# Windows PowerShell
$env:SERVICEBUS_CONNECTION_STRING="Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=..."

# Linux/Mac
export SERVICEBUS_CONNECTION_STRING="Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=..."
```

2. Restart containers:
```powershell
docker-compose down
docker-compose up -d
```

## Stop Services

### Stop All Services
```powershell
docker-compose down
```

### Stop Individual Services
```powershell
# Stop Product Service
docker-compose -f docker-compose.product.yml down

# Stop Order Service
docker-compose -f docker-compose.order.yml down

# Stop Infrastructure (SQL Server)
docker-compose -f docker-compose.infrastructure.yml down

# Remove everything including volumes (DELETES ALL DATA)
docker-compose -f docker-compose.infrastructure.yml down -v
```

## Rebuild After Code Changes

### Rebuild All
```powershell
docker-compose up -d --build
```

### Rebuild Individual Services
```powershell
# Rebuild Product Service
docker-compose -f docker-compose.product.yml up -d --build

# Rebuild Order Service
docker-compose -f docker-compose.order.yml up -d --build
```

## Troubleshooting

### SQL Server not ready
```powershell
# Check SQL Server logs
docker-compose logs sqlserver

# Wait for healthcheck
docker-compose ps
```

### Port conflicts
If ports 5217, 5037, or 1433 are in use:
```powershell
# Check what's using the port
netstat -ano | findstr :5217

# Edit docker-compose.yml to use different ports
```

### Database connection errors
```powershell
# Verify SQL Server is healthy
docker exec -it trello-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P YourStrong@Passw0rd -Q "SELECT @@VERSION"
```

## Database Connection Strings

**Product Service:**
```
Server=sqlserver;Database=TrelloProducts;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true
```

**Order Service:**
```
Server=sqlserver;Database=TrelloOrders;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true
```

## Volume Management

Data is persisted in Docker volumes:
- `sqlserver-data` - SQL Server databases

To backup:
```powershell
docker run --rm -v trello_sqlserver-data:/data -v ${PWD}:/backup alpine tar czf /backup/sqlserver-backup.tar.gz -C /data .
```

To restore:
```powershell
docker run --rm -v trello_sqlserver-data:/data -v ${PWD}:/backup alpine tar xzf /backup/sqlserver-backup.tar.gz -C /data
```
