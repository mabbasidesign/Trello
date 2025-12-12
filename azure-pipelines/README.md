# Azure DevOps CI/CD Pipeline

This directory contains Azure Pipelines configuration for automated CI/CD of the Trello Microservices project.

## Pipeline Overview

The main pipeline (`azure-pipelines.yml` in root) consists of four stages:

1. **Build** - Compile and test .NET services
2. **BuildDockerImages** - Build and push Docker images to Azure Container Registry
3. **DeployInfrastructure** - Deploy Azure resources using Bicep templates
4. **DeployServices** - Deploy containerized services to Azure App Service

## Setup Instructions

### Prerequisites

- Azure DevOps account
- Azure subscription
- Azure Container Registry (ACR)
- Azure App Service or Azure Kubernetes Service (AKS)

### 1. Create Azure Container Registry

```bash
# Create resource group
az group create --name rg-trello-microservices --location eastus

# Create Azure Container Registry
az acr create --resource-group rg-trello-microservices \
  --name trelloacr \
  --sku Standard \
  --admin-enabled true
```

### 2. Create Service Connections in Azure DevOps

#### Docker Registry Service Connection

1. Go to **Project Settings** → **Service connections**
2. Click **New service connection**
3. Select **Docker Registry**
4. Choose **Azure Container Registry**
5. Select your subscription and ACR
6. Name it: `TrelloDockerRegistry`

#### Azure Resource Manager Service Connection

1. Go to **Project Settings** → **Service connections**
2. Click **New service connection**
3. Select **Azure Resource Manager**
4. Choose **Service Principal (automatic)**
5. Select your subscription
6. Name it: `TrelloAzureSubscription`

### 3. Update Pipeline Variables

Edit `azure-pipelines.yml` and update these variables:

```yaml
variables:
  dockerRegistryServiceConnection: 'TrelloDockerRegistry' # Service connection name
  containerRegistry: 'trelloacr.azurecr.io' # ACR name
  azureSubscription: 'TrelloAzureSubscription' # Service connection name
  resourceGroupName: 'rg-trello-microservices' # Resource group
```

### 4. Create App Services (Option 1: Web Apps for Containers)

```bash
# Create App Service Plan
az appservice plan create \
  --name plan-trello-prod \
  --resource-group rg-trello-microservices \
  --is-linux \
  --sku B1

# Create Product Service Web App
az webapp create \
  --resource-group rg-trello-microservices \
  --plan plan-trello-prod \
  --name app-trello-product-prod \
  --deployment-container-image-name trelloacr.azurecr.io/trello-microservices-product:latest

# Create Order Service Web App
az webapp create \
  --resource-group rg-trello-microservices \
  --plan plan-trello-prod \
  --name app-trello-order-prod \
  --deployment-container-image-name trelloacr.azurecr.io/trello-microservices-order:latest

# Configure Product Service
az webapp config appsettings set \
  --resource-group rg-trello-microservices \
  --name app-trello-product-prod \
  --settings \
    ConnectionStrings__DefaultSQLConnection="Server=tcp:your-sql-server.database.windows.net,1433;Database=TrelloProducts;User ID=sqladmin;Password=YourPassword;Encrypt=True;" \
    ServiceBus__ConnectionString="Endpoint=sb://your-servicebus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=yourkey" \
    WEBSITES_PORT=8080

# Configure Order Service
az webapp config appsettings set \
  --resource-group rg-trello-microservices \
  --name app-trello-order-prod \
  --settings \
    ConnectionStrings__DefaultSQLConnection="Server=tcp:your-sql-server.database.windows.net,1433;Database=TrelloOrders;User ID=sqladmin;Password=YourPassword;Encrypt=True;" \
    ServiceBus__ConnectionString="Endpoint=sb://your-servicebus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=yourkey" \
    WEBSITES_PORT=8080
```

### 5. Create Azure SQL Server and Databases

```bash
# Create SQL Server
az sql server create \
  --name sql-trello-prod \
  --resource-group rg-trello-microservices \
  --location eastus \
  --admin-user sqladmin \
  --admin-password 'YourStrongPassword123!'

# Create Product Database
az sql db create \
  --resource-group rg-trello-microservices \
  --server sql-trello-prod \
  --name TrelloProducts \
  --service-objective S0

# Create Order Database
az sql db create \
  --resource-group rg-trello-microservices \
  --server sql-trello-prod \
  --name TrelloOrders \
  --service-objective S0

# Allow Azure services to access
az sql server firewall-rule create \
  --resource-group rg-trello-microservices \
  --server sql-trello-prod \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0
```

### 6. Create Environment in Azure DevOps

1. Go to **Pipelines** → **Environments**
2. Click **New environment**
3. Name it: `production`
4. Choose **None** for resource
5. Add approval checks if needed

### 7. Import Pipeline to Azure DevOps

1. Go to **Pipelines** → **Pipelines**
2. Click **New pipeline**
3. Select **Azure Repos Git** or **GitHub**
4. Select your repository
5. Choose **Existing Azure Pipelines YAML file**
6. Select `/azure-pipelines.yml`
7. Click **Run**

## Pipeline Stages Explained

### Stage 1: Build

- Installs .NET 10.0 SDK
- Restores NuGet packages
- Builds both services
- Runs unit tests (when available)
- Publishes code coverage

**Trigger:** All branches on push/PR

### Stage 2: BuildDockerImages

- Builds Docker images for both services
- Tags with build ID and 'latest'
- Pushes to Azure Container Registry

**Trigger:** Only on `main` branch

### Stage 3: DeployInfrastructure

- Deploys Bicep templates
- Creates/updates Service Bus, queues, topics
- Outputs connection strings

**Trigger:** Only on `main` branch, after Docker build succeeds

### Stage 4: DeployServices

- Deploys container images to App Services
- Uses latest tagged images
- Requires manual approval via environment

**Trigger:** Only on `main` branch, after infrastructure deployment

## Customization Options

### Option 1: Deploy to Azure Kubernetes Service (AKS)

Replace the `DeployServices` stage with:

```yaml
- stage: DeployToAKS
  displayName: 'Deploy to AKS'
  dependsOn: DeployInfrastructure
  jobs:
    - deployment: DeployToAKS
      displayName: 'Deploy to Kubernetes'
      pool:
        vmImage: 'ubuntu-latest'
      environment: 'production'
      strategy:
        runOnce:
          deploy:
            steps:
              - task: KubernetesManifest@0
                displayName: 'Deploy Product Service'
                inputs:
                  action: 'deploy'
                  kubernetesServiceConnection: 'TrelloAKSConnection'
                  namespace: 'trello'
                  manifests: |
                    k8s/product-service.yaml
              
              - task: KubernetesManifest@0
                displayName: 'Deploy Order Service'
                inputs:
                  action: 'deploy'
                  kubernetesServiceConnection: 'TrelloAKSConnection'
                  namespace: 'trello'
                  manifests: |
                    k8s/order-service.yaml
```

### Option 2: Multi-Environment Pipeline

Create separate variable groups for dev, staging, and production:

```yaml
variables:
  - group: trello-dev # For development
  - group: trello-staging # For staging
  - group: trello-prod # For production
```

### Option 3: Add Database Migrations

Add this step before deploying services:

```yaml
- task: AzureCLI@2
  displayName: 'Run Database Migrations'
  inputs:
    azureSubscription: $(azureSubscription)
    scriptType: 'bash'
    scriptLocation: 'inlineScript'
    inlineScript: |
      # Pull the image
      docker pull $(containerRegistry)/$(imageRepository)-product:$(tag)
      
      # Run migrations
      docker run --rm \
        -e ConnectionStrings__DefaultSQLConnection="$(ProductDbConnectionString)" \
        $(containerRegistry)/$(imageRepository)-product:$(tag) \
        dotnet ef database update
```

## Monitoring and Rollback

### View Pipeline Runs

1. Go to **Pipelines** → **Pipelines**
2. Click on your pipeline
3. View run history and logs

### Rollback Deployment

```bash
# Rollback to previous image tag
az webapp config container set \
  --name app-trello-product-prod \
  --resource-group rg-trello-microservices \
  --docker-custom-image-name trelloacr.azurecr.io/trello-microservices-product:12345
```

## Security Best Practices

1. **Store secrets in Azure Key Vault**
   - Connection strings
   - Service Bus keys
   - SQL passwords

2. **Use managed identities**
   - App Services to access Key Vault
   - App Services to access ACR

3. **Enable vulnerability scanning**
   - Enable Defender for ACR
   - Scan images before deployment

4. **Restrict network access**
   - Use Private Endpoints for SQL
   - Use VNET integration for App Services

## Troubleshooting

### Pipeline fails on Docker build

- Check Dockerfile paths are correct
- Ensure build context is set properly
- Verify ACR service connection is working

### Deployment fails

- Check App Service exists
- Verify service connection has proper permissions
- Check application settings are configured

### Container won't start

- Check application logs in App Service
- Verify environment variables are set
- Ensure WEBSITES_PORT is set to 8080

## Additional Resources

- [Azure Pipelines Documentation](https://docs.microsoft.com/azure/devops/pipelines/)
- [Docker task in Azure Pipelines](https://docs.microsoft.com/azure/devops/pipelines/tasks/build/docker)
- [Deploy to App Service](https://docs.microsoft.com/azure/devops/pipelines/targets/webapp)
