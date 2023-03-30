---
canonical_url: https://bitoftech.net/2022/09/16/use-bicep-to-deploy-dapr-microservices-apps-to-azure-container-apps-part-10/
---

# Module 10 - Deployment via Bicep
In this module, we will be working on defining the proper process to automate the infrastructure provisioning by creating the scripts/templates to provision the resources, this process is known as IaC (Infrastructure as Code).

Once we have this in place, IaC deployments will benefit us in key ways such as:

1. Increase confidence in the deployments, ensure consistency, reduce human error in resource provisioning, and ensure consistent deployments.
2. Avoid configuration drifts, IaC is an idempotent operation, which means it provides the same result each time it’s run.
3. Provision of new environments, during the lifecycle of the application you might need to run penetration testing or load testing for a short period of time in a totally isolated environment, with IaC in place it will be a matter of executing the scripts to recreate an identical environment to the production one.
4. When you provision resources from the Azure Portal many processes are abstracted, in our case think of when creating an Azure Container Apps Environment from the portal, behind the sense it will create a log analytics workspace and associate with the environment. With IaC it can help provide a better understanding of how Azure works and how to troubleshoot issues that might arise.

### ARM Templates in Azure

ARM templates are files that define the infrastructure and configuration for your deployment, the template uses declarative syntax, which lets you state what you intend to deploy without having to write the sequence of programming commands to create it.

Within Azure there are 2 ways to create IaC, we can use the [JSON ARM templates](https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/overview) or [Bicep](https://docs.microsoft.com/en-us/azure/azure-resource-manager/bicep/overview?tabs=bicep) (domain-specific language). From past experience, using JSON ARM templates in real-world scenarios tends to be complex to manage and maintain especially when the project grows and the number of components and dependencies increases.

Using Bicep is simpler and easier to work with, you can be more productive compared to ARM templates, it is worth mentioning that Bicep code will be compiled into ARM templates eventually, this process called "Transpilation".

To learn more about Bicep, I highly recommend checking Microsoft learn website [Fundamentals of Bicep.](https://docs.microsoft.com/en-us/training/paths/fundamentals-bicep/)

![aca-arm-bicep](../../assets/images/10-aca-iac-bicep/aca-bicep-l.jpg)

### Build the Infrastructure as Code using Bicep

Let's get started by defining the Bicep modules needed to create the Infrastructure code, what we want to achieve by the end of this module is to have a new resource group containing all the resources and configuration (connection strings, secrets, env variables, dapr components, etc..) we used to build our solution, we should have a new resource group which contains the below resources.
![aca-resources](../../assets/images/10-aca-iac-bicep/aca-rescources.jpg)

!!! note
    To simplify the execution of the module, we will assume that the azure resource "Azure Container Registry" is already provisioned and it contains the latest images of the 3 services. We will not provision Azure Container Registry part of this Bicep modules.

#### 1. Add the needed extension to VS Code
You need to install an extension named [Bicep](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-bicep), This extension will simplify building Bicep files as they will offer IntelliSense, Validation, listing all available resource types, etc..

#### 2. Define an Azure Container Apps Environment
Add a new folder named `Bicep` on the root project directory, then add another folder named `Modules`. Add a new file named `container-apps-environment.bicep`. The content of the file can be found on this [link.](https://github.com/Azure/aca-dotnet-workshop/blob/main/bicep/modules/container-apps-environment.bicep).

What we've added to this file is the following:
- The module accepts various parameters, all of them are parameters with default values, meaning that if no value is provided it will take the default value. 
- The parameter `location` defaults to the container resource group location. Bicep has a function named `resourceGroup()` which you can get the location from.
- The parameters `prefix` and `suffix` could be used if you want to add a prefix or suffix to the resource names
- The parameter `tag` is used to tag the created resources, tags are key-value pairs that help you identify resources based on settings that are relevant to your organization and deployment.
- The parameters `containerAppsEnvironmentName`, `logAnalyticsWorkspaceName`, and `applicationInsightName` have default values of resource names using the helper function named `uniqueString`. This function performs a 64-bit hash of the provided strings to create a unique string. This function is helpful when you need to create a unique name for a resource. We are passing the `resourceGroup().id` to this function to ensure that if we executed this module on 2 different resource groups, the generated string will be a global unique name.
- This module will create 3 resources, it will start by creating a `logAnalyticsWorkspace`, then an `applicationInsights` resource. Notice how we are setting the `logAnalyticsWorkspace.id` as an application insights `WorkspaceResourceId`
- Lastly we are creating the `containerAppsEnvironment`. Notice how we are setting the `daprAIInstrumentationKey` by using the Application Insights `InstrumentationKey` and then setting `logAnalyticsConfiguration.customerId` and `logAnalyticsConfiguration.sharedKey`.
- The output of this module are a single parameter named `applicationInsightsName`, This output is needed as an input for other module.

#### 3. Define an Azure Key Vault Resource
Add a new file named `key-vault.bicep` under the folder `bicep\modules`. The content of the file can be found on this [link.](https://github.com/Azure/aca-dotnet-workshop/blob/main/bicep/modules/key-vault.bicep)

- This module will create the Azure Key Vault resource which will be used to store secrets.
- The output of this module are a single parameter named `keyVaultId`, This output is needed as an input for other module.

#### 4. Define a Azure Service Bus Resource
Add a new file named `service-bus.bicep` under the folder `bicep\modules`. The content of the file can be found on this [link.](https://github.com/Azure/aca-dotnet-workshop/blob/main/bicep/modules/service-bus.bicep)

- This module will create the Azure Service resource, a topic, a subscription for the consumer, and an authorization rule with `Manage` permissions.

- The output of this module will return 3 output parameters which will be used as inputs params for other modules.

#### 4. Define an Azure CosmosDb Resource

Add a new file named `cosmos-db.bicep` under the folder `bicep\modules`. The content of the file can be found on this [link.](https://github.com/Azure/aca-dotnet-workshop/blob/main/bicep/modules/cosmos-db.bicep)

- This module will create the Azure CosmosDB account, a CosmosDB database, and a CosmosDB collection.

- The output of this module will return 3 output parameters which will be used as inputs params for other modules.

#### 5. Define an Azure Storage Resource

Add a new file named `storage-account.bicep` under the folder `bicep\modules`. The content of the file can be found on this [link.](https://github.com/Azure/aca-dotnet-workshop/blob/main/bicep/modules/storage-account.bicep)

- This module will create the Azure Storage account, a storage queue service, and a queue.

- The output of this module will single output parameter which is the Azure storage account name.

#### 6. Define Dapr Components

Next we will define all dapr components used in the solution in ne single bicep module, to accomplish this, add a new file named `dapr-components.bicep` under the folder `bicep\modules`. The content of the file can be found on this [link.](https://github.com/Azure/aca-dotnet-workshop/blob/main/bicep/modules/dapr-components.bicep)

- This module will be responsible to create all dapr components used in the solution, it accepts various input parameters needed by the dapr components.

- Notice how we are using the keyword `existing` to obtain a strongly typed reference to the pre-created resource

    ```shell
    resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2022-03-01' existing = {
       name: containerAppsEnvironmentName
    }

    resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2022-08-15' existing = {
       name: cosmosDbName
    }
    ```

#### 7. Create Secrets into Azure Key Vault

* This module will be responsible to create the secrets and store them in Azure Key Vault, as well it create a role assignment for the backend processor service of type `Azure Role Key Vault Secrets User` so the service can access the Key Vault and read secrets.

* To accomplish this, add a new folder named `container-apps\secrets` under the `modules` folder, and then add a new file named `processor-backend-service-secrets.bicep` under the folder `container-apps\secrets`. The content of the file can be found on this [link.](https://github.com/Azure/aca-dotnet-workshop/blob/main/bicep/modules/container-apps/secrets/processor-backend-service-secrets.bicep)

#### 8. Define the Frontend Service Azure Container App
Now we will start defining the modules needed to create the container apps, we will start with the Frontend App, to accomplish this add a new file named `webapp-frontend-service.bicep` under the folder `modules\container-apps`. The content of the file can be found on this [link.](https://github.com/Azure/aca-dotnet-workshop/blob/main/bicep/modules/container-apps/webapp-frontend-service.bicep)

- Notice how we used the attribute `@secure` on input parameters which contain secrets or keys, this attribute can be applied on string and object parameters that contain secret values, when it is used Azure won't make the parameter values available in the deployment logs nor on the terminal if you are using Azure CLI.

- The out parameters this module will return the fully qualified domain name (FQDN) of the frontend container app.

#### 9. Define the Backend Api Service Azure Container App

Add a new file named `webapi-backend-service.bicep` under the folder `modules\container-apps`. The content of the file can be found on this [link.](https://github.com/Azure/aca-dotnet-workshop/blob/main/bicep/modules/container-apps/webapi-backend-service.bicep)

- Notice how we are assigning the Cosmosdb account a read/write access using the `Cosmos DB Built-in Data Contributor` role to the Backend API system assigned identity by using the code below:

    ```Shell
    resource backendApiService_cosmosdb_role_assignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2022-08-15' = {
    name: guid(subscription().id, backendApiService.name, '00000000-0000-0000-0000-000000000002')
    parent: cosmosDbAccount
    properties: {
        principalId: backendApiService.identity.principalId
        roleDefinitionId:  resourceId('Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions', cosmosDbAccount.name, '00000000-0000-0000-0000-000000000002')//DocumentDB Data Contributor
        scope: '${cosmosDbAccount.id}/dbs/${cosmosDbDatabase.name}/colls/${cosmosDbDatabaseCollection.name}'
    }
    ```

- The same approach used when assigning the role `Azure Service Bus Data Sender` to the Backend API to be able to publish messages to Azure Service Bus using Backend API system assigned identity, this done using the code below:
    ```Shell
    resource backendApiService_sb_role_assignment 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = {
    name: guid(resourceGroup().id, backendApiService.name, '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39')
    properties: {
        principalId: backendApiService.identity.principalId
        roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39')//Azure Service Bus Data Sender
        principalType: 'ServicePrincipal'
    }
    scope: serviceBusTopic
    }
    ```

#### 10. Define the Backend Processor Service Azure Container App
Add a new file named `processor-backend-service.bicep` under the folder `modules\container-apps`. The content of the file can be found on this [link.](https://github.com/Azure/aca-dotnet-workshop/blob/main/bicep/modules/container-apps/processor-backend-service.bicep)

- Notice how we are assigning the role `Azure Service Bus Data Receiver` to the Backend Processor to be able to consume/read messages from Azure Service Bus Topic using Backend Processor system assigned identity, this done using the code below:
    ```Shell
    resource backendProcessorService_sb_role_assignment 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = {
    name: guid(resourceGroup().id, backendProcessorServiceName, '4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0')
    properties: {
        principalId: backendProcessorService.identity.principalId
        roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', '4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0') // Azure Service Bus Data Receiver.
        principalType: 'ServicePrincipal'
    } 
    scope: serviceBusNamespace
    }
    ```

- Within this module, we've invoked the module defined in [step 7](#7-create-secrets-into-azure-key-vault) which is responsible to create the secrets in Azure Key Vault and assign the role `Azure Role Key Vault Secrets User` to the Backend Processor Service,  this achieved using the code below:
    ```shell
    module backendProcessorKeySecret 'secrets/processor-backend-service-secrets.bicep' = {
    name: 'backendProcessorKeySecret-${uniqueString(resourceGroup().id)}'
    params: {
        keyVaultName: keyVaultName
        sendGridKeySecretName: sendGridKeySecretName
        sendGridKeySecretValue: sendGridKeySecretValue
        externalAzureStorageKeySecretName: externalStorageKeySecretName
        externalAzureStorageKeySecretValue: storageAccount.listKeys().keys[0].value
        backendProcessorServicePrincipalId: backendProcessorService.identity.principalId
    }
    scope: resourceGroup(keyVaultSubscriptionId, keyVaultResourceGroupName)
    }
    ```

#### 11. Define a container module for the 3 Container Apps 
This module will act as a container for the 3 Container Apps modules defined in the previous 3 steps, it is optional to create it, but it makes it easier when we invoke all the created modules as you will see in the next step. To create this module add a new file named `container-apps.bicep` under the folder `modules` The content of the file can be found on this [link.](https://github.com/Azure/aca-dotnet-workshop/blob/main/bicep/modules/container-apps.bicep)

#### 12. Define the Main module for the solution 
Lastly, we need to define the Main Bicep module which will link all other modules together, this will be the file that is referenced from AZ CLI command when creating the entire resources, to do so add a new file named `main.bicep` under the folder `bicep`, the content of the file can be found on this [link.](https://github.com/Azure/aca-dotnet-workshop/blob/main/bicep/main.bicep)

Few things to notice on this module:
- When calling the module `dapr-components.bicep` we are setting the value of the array `dependsOn` to the Container Apps Environment, this is called explicit dependency which will help the Bicep interpreter to understand the dependencies between components, in this case, the Container Apps Environment should be provisioned before the Dapr Components in order for the deployment to success.

- When calling the module `container-apps.bicep`, some of the input params are expecting are referencing another resource, for example consider the input param named `cosmosDbName` and the value used is `cosmosDb.outputs.cosmosDbName`. This means that the module `cosmos-db.bicep` should be created successfully before creating the container apps module, this called Implicit dependency.

### Deploy the infrastructure and create the components
With the steps above completed we are ready to an end to end test, we just need to create a parameters file which will simplify the invocation of the main bicep file, to achieve this, right click on file `main.bicep` and select `Generate Parameter File`, this will result in creating a file named `main.parameters.json` similar to the file on this [link.](https://github.com/Azure/aca-dotnet-workshop/blob/main/bicep/main.parameters.json).

To use this file, you need to edit this generated file and provide values for the parameters, you can use the same values provided on the github link, you only need to replace parameter values between the angle brackets `<>` with values related to your ACR resource and SendGrid.

To start the actual deployment by calling `az deployment group create` to do so, open the PowerShell console and use the content below. You need to create an empty resource group before. I'm using a resource group named `aca-workshop-bicep-rg`

```Powershell
az group create `
--name "aca-workshop-bicep-rg" `
--location "eastus"  


az deployment group create `
--resource-group "aca-workshop-bicep-rg" `
--template-file "./bicep/main.bicep" `
--parameters "./bicep/main.parameters.json"
```

Azure CLI will take the Bicep module, and start creating the deployment in the resource group `aca-workshop-bicep-rg`

### Verify the final results

If the deployment succeeded; you should see all the resources created under the resource group, as well you can navigate to the `Deployments` tab to verify the ARM templates deployed, it should look like the below image:

![aca-deployment](../../assets/images/10-aca-iac-bicep/aca-deployment.jpg)