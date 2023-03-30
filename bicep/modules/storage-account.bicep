targetScope = 'resourceGroup'

// ------------------
//    PARAMETERS
// ------------------

@description('The location where the resources will be created.')
param location string = resourceGroup().location

@description('Optional. The tags to be assigned to the created resources.')
param tags object = {}

@description('The name of the external Azure Storage Account.')
param storageAccountName string

@description('The name of the external Queue in Azure Storage.')
param externalTasksQueueName string

// ------------------
// RESOURCES
// ------------------

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-09-01' = {
  name: storageAccountName
  tags: tags
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
}

resource storageQueuesService 'Microsoft.Storage/storageAccounts/queueServices@2021-09-01' = {
  name: 'default'
  parent: storageAccount
}

resource externalQueue 'Microsoft.Storage/storageAccounts/queueServices/queues@2021-09-01' = {
  name: externalTasksQueueName
  parent: storageQueuesService
}

// ------------------
// OUTPUTS
// ------------------

@description('The external storage account name.')
output storageAccountName string = storageAccount.name
