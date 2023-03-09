targetScope = 'resourceGroup'

// ------------------
//    PARAMETERS
// ------------------

@description('The location where the resources will be created.')
param location string = resourceGroup().location

@description('Optional. The tags to be assigned to the created resources.')
param tags object = {}

@description('The name of Cosmos DB resource.')
param cosmosDbName string

@description('The name of Cosmos DB\'s database.')
param cosmosDbDatabaseName string

@description('The name of Cosmos DB\'s collection.')
param cosmosDbCollectionName string


// ------------------
// VARIABLES
// ------------------


// ------------------
// RESOURCES
// ------------------
resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2022-08-15' = {
  name: cosmosDbName
  location: location
  tags: tags
  kind: 'GlobalDocumentDB'
  properties: {
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    databaseAccountOfferType: 'Standard'
    publicNetworkAccess:'Enabled'
  }
}

resource cosmosDbDatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2021-04-15' = {
  name: cosmosDbDatabaseName
  parent: cosmosDbAccount
  tags: tags
  properties: {
    resource: {
      id: cosmosDbDatabaseName
    }
  }
}

resource cosmosDbDatabaseCollection 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2021-05-15' = {
  name: cosmosDbCollectionName
  parent: cosmosDbDatabase
  tags: tags
  properties: {
    resource: {
      id: cosmosDbCollectionName
      partitionKey: {
        paths: [
          '/partitionKey'
        ]
        kind: 'Hash'
      }
    }
    options: {
      autoscaleSettings: {
        maxThroughput: 4000
      }
    }
  }
}

// ------------------
// OUTPUTS
// ------------------

@description('The name of Cosmos DB resource.')
output cosmosDbName string = cosmosDbAccount.name
@description('The name of Cosmos DB\'s database.')
output cosmosDbDatabaseName string = cosmosDbDatabase.name
@description('The name of Cosmos DB\'s collection.')
output cosmosDbCollectionName string = cosmosDbDatabaseCollection.name
@description('The document endpoint of Cosmos DB\'s account.')
output cosmosDbDocumentEndpoint string = cosmosDbAccount.properties.documentEndpoint
