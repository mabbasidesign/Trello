# Trello Microservices - Docker Commands Reference

## START ALL SERVICES

# Navigate to docker folder
cd C:\Users\mabba\Desktop\Trello\docker

# Start SQL Server
docker-compose -f docker-compose.infrastructure.yml up -d

# Start Product Service
docker-compose -f docker-compose.product.yml up -d --build

# Start Order Service
docker-compose -f docker-compose.order.yml up -d --build

# Or start everything together
docker-compose up -d --build


## CHECK STATUS

# View all running containers
docker ps

# View all containers (including stopped)
docker ps -a


## VIEW LOGS

# Product Service
docker logs product-service --tail 20
docker logs product-service -f

# Order Service
docker logs order-service --tail 20
docker logs order-service -f

# SQL Server
docker logs trello-sqlserver --tail 20
docker logs trello-sqlserver -f


## STOP SERVICES

# Stop Product Service
docker-compose -f docker-compose.product.yml down

# Stop Order Service
docker-compose -f docker-compose.order.yml down

# Stop SQL Server
docker-compose -f docker-compose.infrastructure.yml down

# Stop everything
docker-compose down


## REBUILD AFTER CODE CHANGES

# Rebuild Product Service
docker-compose -f docker-compose.product.yml up -d --build

# Rebuild Order Service
docker-compose -f docker-compose.order.yml up -d --build

# Rebuild everything
docker-compose up -d --build


## DATABASE MIGRATION (After containers are running)

# Product Service
docker exec -it product-service dotnet ef database update

# Order Service
docker exec -it order-service dotnet ef database update


## CLEANUP

# Remove all containers and networks (keeps volumes)
docker-compose down

# Remove everything including volumes (DELETES DATA!)
docker-compose down -v

# Remove unused images
docker image prune -a


## ACCESS SERVICES

# Product Service Swagger: http://localhost:5217/swagger
# Order Service Swagger: http://localhost:5037/swagger
# Product Service Health: http://localhost:5217/health
# Order Service Health: http://localhost:5037/health


## TROUBLESHOOTING

# Restart a specific container
docker restart product-service
docker restart order-service
docker restart trello-sqlserver

# View container details
docker inspect product-service
docker inspect order-service
docker inspect trello-sqlserver

# Execute commands inside container
docker exec -it product-service bash
docker exec -it order-service bash
docker exec -it trello-sqlserver bash

# Check SQL Server connection
docker exec -it trello-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P YourStrong@Passw0rd -Q "SELECT @@VERSION"
