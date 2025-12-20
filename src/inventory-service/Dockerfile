# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .

# Environment variables for Cosmos DB
ENV ASPNETCORE_ENVIRONMENT=Development
ENV CosmosDb__ConnectionString=""
ENV CosmosDb__DatabaseId="InventoryDb"
ENV CosmosDb__ContainerId="InventoryItems"

EXPOSE 8080
ENTRYPOINT ["dotnet", "inventory-service.dll"]
