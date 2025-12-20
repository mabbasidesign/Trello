param location string = resourceGroup().location
param accountName string = uniqueString(resourceGroup().id, 'inventory-cosmos')
param databaseName string = 'InventoryDb'
param containerName string = 'InventoryItems'

resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2023-03-15' = {
  name: accountName
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    locations: [
      {
        locationName: location
        failoverPriority: 0
      }
    ]
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    capabilities: [
      {
        name: 'EnableServerless'
      }
    ]
  }
}

resource cosmosDbDatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2023-03-15' = {
  name: '${cosmosDbAccount.name}/${databaseName}'
  properties: {
    resource: {
      id: databaseName
    }
  }
  dependsOn: [cosmosDbAccount]
}

resource cosmosDbContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-03-15' = {
  name: '${cosmosDbAccount.name}/${databaseName}/${containerName}'
  properties: {
    resource: {
      id: containerName
      partitionKey: {
        paths: ['/id']
        kind: 'Hash'
      }
      defaultTtl: -1
    }
  }
  dependsOn: [cosmosDbDatabase]
}

output cosmosDbAccountName string = cosmosDbAccount.name
output cosmosDbDatabaseName string = databaseName
output cosmosDbContainerName string = containerName
output cosmosDbConnectionString string = listKeys(cosmosDbAccount.id, cosmosDbAccount.apiVersion).primaryMasterKey
