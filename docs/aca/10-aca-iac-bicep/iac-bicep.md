---
canonical_url: https://bitoftech.net/2022/09/16/use-bicep-to-deploy-dapr-microservices-apps-to-azure-container-apps-part-10/
---

# Build the Infrastructure as Code Using Bicep

!!! info "Module Duration"
    30 minutes

!!! note
    If you're not interested in manually deploying the Bicep files or creating the container registry yourself, and prefer not to delve into the details of how they work, then you can skip this section and head directly to either [Build the Infrastructure as Code Using Bicep and Github](../../aca/10-aca-iac-bicep/ci-cd-git-action.md) or [Build the Infrastructure as Code Using Bicep and Azure DevOps](../../aca/10-aca-iac-bicep/ci-cd-azdo.md) depending on your DevOps tool of choice.

To begin, we need to define the Bicep modules that will be required to generate the Infrastructure code. Our goal for this module is to have a freshly created resource group that encompasses all the necessary resources and configurations - such as connection strings, secrets, environment variables, and Dapr components - which we utilized to construct our solution. By the end, we will have a new resource group that includes the following resources.

![aca-resources](../../assets/images/10-aca-iac-bicep/aca-rescources.jpg)

!!! note
    To simplify the execution of the module, we will assume that you have already created latest images of three services and pushed them to a container registry. [This section](#deploy-the-infrastructure-and-create-the-components) below guides you through
    different options of getting images pushed to either Azure Container Registry (ACR) or GitHub Container Registry (GHCR).

#### 1. Add the Needed Extension to VS Code

To proceed, you must install an extension called [Bicep](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-bicep). This extension will simplify building Bicep files as it offers IntelliSense, Validation, listing all available resource types, etc..

#### 2. Define an Azure Container Apps Environment

Add a new folder named `bicep` on the root project directory, then add another folder named `modules`. Add file as shown below:

=== "container-apps-environment.bicep"

    ```bicep
    --8<-- "https://raw.githubusercontent.com/Azure/aca-dotnet-workshop/main/bicep/modules/container-apps-environment.bicep"
    ```

??? tip "What we've added in the Bicep file above"
    - The module takes multiple parameters, all of which are set to default values. This indicates that if no value is specified, the default value will be utilized.
    - The `location` parameter defaults to the location of the container resource group. Bicep has a function called `resourceGroup()`, which can be used to retrieve the location.
    - The parameters `prefix` and `suffix` could be used if you want to add a prefix or suffix to the resource names.
    - The parameter `tag` is used to tag the created resources. Tags are key-value pairs that help you identify resources based on settings that are relevant to your organization and deployment.
    - The parameters `containerAppsEnvironmentName`, `logAnalyticsWorkspaceName`, and `applicationInsightName` have default values of resource names using the helper function named `uniqueString`. This function performs a 64-bit hash of the provided strings to create a unique string. This function is helpful when you need to create a unique name for a resource. We are passing the `resourceGroup().id` to this function to ensure that if we executed this module on two different resource groups, the generated string will be a global unique name.
    - This module will create two resources. It will start by creating a `logAnalyticsWorkspace`, then an `applicationInsights` resource. Notice how we are setting the `logAnalyticsWorkspace.id` as an application insights `WorkspaceResourceId`.
    - Lastly we are creating the `containerAppsEnvironment`. Notice how we are setting the `daprAIInstrumentationKey` by using the Application Insights `InstrumentationKey` and then setting `logAnalyticsConfiguration.customerId` and `logAnalyticsConfiguration.sharedKey`.
    - The output of this module are a is parameter named `applicationInsightsName`. This output is needed as an input for a subsequent module.

#### 3. Define an Azure Key Vault Resource

Add file as shown below under the folder `bicep\modules`:

=== "key-vault.bicep"

    ```bicep
    --8<-- "https://raw.githubusercontent.com/Azure/aca-dotnet-workshop/main/bicep/modules/key-vault.bicep"
    ```

??? tip "What we've added in the Bicep file above"
    - This module will create the Azure Key Vault resource which will be used to store secrets.
    - The output of this module is a single parameter named `keyVaultId`. This output is needed as an input for a subsequent module.

#### 4. Define a Azure Service Bus Resource

Add file as shown below under the folder `bicep\modules`:

=== "service-bus.bicep"

    ```bicep
    --8<-- "https://raw.githubusercontent.com/Azure/aca-dotnet-workshop/main/bicep/modules/service-bus.bicep"
    ```

??? tip "What we've added in the Bicep file above"
    - This module will create the Azure Service resource, a topic, a subscription for the consumer, and an authorization rule with `Manage` permissions.
    - The output of this module will return three output parameters which will be used as an input for a subsequent module.

#### 5. Define an Azure CosmosDb Resource

Add file as shown below under the folder `bicep\modules`:

=== "cosmos-db.bicep"

    ```bicep
    --8<-- "https://raw.githubusercontent.com/Azure/aca-dotnet-workshop/main/bicep/modules/cosmos-db.bicep"
    ```

??? tip "What we've added in the Bicep file above"
    - This module will create the Azure CosmosDB account, a CosmosDB database, and a CosmosDB collection.
    - The output of this module will return three output parameters which will be used as an input for a subsequent module.

#### 6. Define an Azure Storage Resource

Add file as shown below under the folder `bicep\modules`:

=== "storage-account.bicep"

    ```bicep
    --8<-- "https://raw.githubusercontent.com/Azure/aca-dotnet-workshop/main/bicep/modules/storage-account.bicep"
    ```

??? tip "What we've added in the Bicep file above"
    - This module will create the Azure Storage account, a storage queue service, and a queue.
    - The output of this module will be a single output parameter which will be used as an input for a subsequent module.

#### 7. Define Dapr Components

Next we will define all dapr components used in the solution in a single bicep module. To accomplish this, add a new file under the folder `bicep\modules` as shown below:

=== "dapr-components.bicep"

    ```bicep
    --8<-- "https://raw.githubusercontent.com/Azure/aca-dotnet-workshop/main/bicep/modules/dapr-components.bicep"
    ```

??? tip "What we've added in the Bicep file above"
    - This module will be responsible for creating all dapr components used in the solution. It accepts various input parameters needed by the dapr components.
    - Notice how we are using the keyword `existing` to obtain a strongly typed reference to the pre-created resource

        ```shell hl_lines="1 5"
        resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2022-03-01' existing = {
        name: containerAppsEnvironmentName
        }

        resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2022-08-15' existing = {
        name: cosmosDbName
        }
        ```

#### 8. Create Secrets Into Azure Key Vault

This module will have the responsibility of generating the secrets and saving them in Azure Key Vault. Additionally, it will establish a role assignment for the backend processor service, specifically of type `Azure Role Key Vault Secrets User`, which will allow the service to access the Key Vault and retrieve the secrets.

To achieve this, create a new directory called `container-apps\secrets` within the `modules` folder. Add new file as shown below under the folder `bicep\modules\container-apps\secrets`:

=== "processor-backend-service-secrets.bicep"

    ```bicep
    --8<-- "https://raw.githubusercontent.com/Azure/aca-dotnet-workshop/main/bicep/modules/container-apps/secrets/processor-backend-service-secrets.bicep"
    ```

#### 9. Define the Frontend Service Azure Container App

We will now begin defining the modules that are necessary for producing the container apps, starting with the Frontend App. To initiate this process, add a new file under the folder `bicep\modules\container-apps` as shown below:

=== "webapp-frontend-service.bicep"

    ```bicep
    --8<-- "https://raw.githubusercontent.com/Azure/aca-dotnet-workshop/main/bicep/modules/container-apps/webapp-frontend-service.bicep"
    ```

??? tip "What we've added in the Bicep file above"
    - Observe the usage of the `@secure` attribute on input parameters that contain confidential information or keys. This attribute may be applied to both string and object parameters that encompass secretive values. By implementing this attribute, Azure will abstain from presenting the parameter values within the deployment logs or on the terminal if you happen to be utilizing Azure CLI.
    - The output parameters of this module will provide the fully qualified domain name (FQDN) for the frontend container application.

#### 10. Define the Backend Api Service Azure Container App

Add a new file under the folder `bicep\modules\container-apps` as shown below:

=== "webapi-backend-service.bicep"

    ```bicep
    --8<-- "https://raw.githubusercontent.com/Azure/aca-dotnet-workshop/main/bicep/modules/container-apps/webapi-backend-service.bicep"
    ```

??? tip "What we've added in the Bicep file above"

    - Notice how we are assigning the Cosmosdb account a read/write access using the `Cosmos DB Built-in Data Contributor` role to the Backend API system assigned identity, by using the code below:

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
    - A similar technique was applied when assigning the Azure Service Bus Data Sender role to the Backend API, enabling it to publish messages to Azure Service Bus utilizing the Backend API system-assigned identity. This was accomplished utilizing the following code:
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

#### 11. Define the Backend Processor Service Azure Container App

Add a new file under the folder `bicep\modules\container-apps` as shown below:

=== "processor-backend-service.bicep"

    ```bicep
    --8<-- "https://raw.githubusercontent.com/Azure/aca-dotnet-workshop/main/bicep/modules/container-apps/processor-backend-service.bicep"
    ```

??? tip "What we've added in the Bicep file above"

    - Notice how we are assigning the role `Azure Service Bus Data Receiver` to the Backend Processor to be able to consume/read messages from Azure Service Bus Topic using Backend Processor system assigned identity, by using the code below:
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
    - Within this module, we've invoked the module defined in [step 8](#8-create-secrets-into-azure-key-vault) which is responsible to create the secrets in Azure Key Vault and assign the role `Azure Role Key Vault Secrets User` to the Backend Processor Service, by using the code below:
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

#### 12. Define a Container Module For the Three Container Apps

This module will act as a container for the three Container Apps modules defined in the previous three steps. It is optional to create it, but it makes it easier when we invoke all the created modules as you will see in the next step.

Add a new file under the folder `bicep\modules` as shown below:

=== "container-apps.bicep"

    ```bicep
    --8<-- "https://raw.githubusercontent.com/Azure/aca-dotnet-workshop/main/bicep/modules/container-apps.bicep"
    ```

#### 13. Define the Main Module For the Solution

Finally, we must specify the Main Bicep module that will connect all other modules together. This file will be referenced by the AZ CLI command when producing all resources.

To achieve this, add a new file under the `bicep` directory as shown below:

=== "main.bicep"

    ```bicep
    --8<-- "https://raw.githubusercontent.com/Azure/aca-dotnet-workshop/main/bicep/main.bicep"
    ```

??? tip "What we've added in the Bicep file above"

    - When calling the module `dapr-components.bicep` we are setting the value of the array `dependsOn` to the Container Apps Environment. This is called explicit dependency which aids the Bicep interpreter in comprehending the relationships between components. In this instance, the Container Apps Environment must be provisioned before the Dapr Components to guarantee a successful deployment.
    
    - When calling the module `container-apps.bicep`, some of the input params are expecting are referencing another resource, for example consider the input param named `cosmosDbName` and the value used is `cosmosDb.outputs.cosmosDbName`. This means that the module `cosmos-db.bicep` should be created successfully before creating the container apps module, this called Implicit dependency.

### Deploy the Infrastructure and Create the Components

Start by creating a new resource group which will contain all the resources to be created by the Bicep scripts.

    ```Powershell
    $RESOURCE_GROUP="<your RG name>"
    $LOCATION="<your location>"
    
    az group create `
    --name $RESOURCE_GROUP `
    --location $LOCATION
    ```

Create a parameters file which will simplify the invocation of the main bicep file. To achieve this, right click on file `main.bicep` and select **Generate Parameter File**.
This will result in creating a file named `main.parameters.json` similar to the file below:

??? example

    === "main.parameters.json"
    
        ```json
        --8<-- "https://raw.githubusercontent.com/Azure/aca-dotnet-workshop/main/bicep/main.parameters.json"
        ```
!!! note

    To use this file, you need to edit this generated file and provide values for the parameters. You can use the same values shown above in sample file. 

    You only need to replace parameter values between the angle brackets `<>` with values related to your container registry and SendGrid. Values for container registry and container images can be derived by following
    one of the three options in next step.
    
    In case you followed along with the whole workshop and would like to use your own sourcecode, make sure to replace the port numbers (80) by the port numbers that were generated when you created your docker files in vs code for your three applications (e.g., 5225). Make sure that the port numbers match the numbers in the respective docker files. If port numbers aren't matching, your deployment will work without errors, but the portal will report issues with the apps and calling one of the apps will result in a session timeout.

Next, we will prepare container images for the three container apps and update the values in `main.parameters.json` file. You can do so by any of the three options below:

=== "Option 1: Build and Push the Images to Azure Container Registry (ACR)"

    1. Create an Azure Container Registry (ACR) inside the newly created Resource Group:

        ```Powershell
        $CONTAINER_REGISTRY_NAME="<your ACR name>"

        az acr create `
            --resource-group $RESOURCE_GROUP `
            --name $CONTAINER_REGISTRY_NAME `
            --sku Basic
        ```

    2. Build and push the images to ACR. Make sure you are at the root project directory when executing the following commands:

        ```Powershell

        ## Build Backend API on ACR and Push to ACR

        az acr build --registry $CONTAINER_REGISTRY_NAME `
            --image "tasksmanager/tasksmanager-backend-api" `
            --file 'TasksTracker.TasksManager.Backend.Api/Dockerfile' .
        
        ## Build Backend Service on ACR and Push to ACR

        az acr build --registry $CONTAINER_REGISTRY_NAME `
            --image "tasksmanager/tasksmanager-backend-processor" `
            --file 'TasksTracker.Processor.Backend.Svc/Dockerfile' .

        ## Build Frontend Web App on ACR and Push to ACR

        az acr build --registry $CONTAINER_REGISTRY_NAME `
            --image "tasksmanager/tasksmanager-frontend-webapp" `
            --file 'TasksTracker.WebPortal.Frontend.Ui/Dockerfile' .
        ```

    3. Update the `main.parameters.json` file with the container registry name and the container images names as shown below:

        ```json hl_lines="3 6 9 12"
        {
            "containerRegistryName": {
                "value": "<CONTAINER_REGISTRY_NAME>"
            },
            "backendProcessorServiceImage": {
                "value": "<CONTAINER_REGISTRY_NAME>.azurecr.io/tasksmanager/tasksmanager-backend-processor:latest"
            },
            "backendApiServiceImage": {
                "value": "<CONTAINER_REGISTRY_NAME>.azurecr.io/tasksmanager/tasksmanager-backend-api:latest"
            },
            "frontendWebAppServiceImage": {
                "value": "<CONTAINER_REGISTRY_NAME>.azurecr.io/tasksmanager/tasksmanager-frontend-webapp:latest"
            }
        }
        ```

=== "Option 2: Import pre-built public images to your private Azure Container Registry"

    All the container image are available in a public image repository. If you do not wish to build the container images from code directly, you can import it directly into 
    your private container instance as shown below.

    1. Create an Azure Container Registry (ACR) inside the newly created Resource Group:

        ```Powershell
        $CONTAINER_REGISTRY_NAME="<your ACR name>"

        az acr create `
            --resource-group $RESOURCE_GROUP `
            --name $CONTAINER_REGISTRY_NAME `
            --sku Basic
        ```
    2. Import the images to your private ACR as shown below:

        ```Powershell 

            az acr import `
            --name $CONTAINER_REGISTRY_NAME `
            --image tasksmanager/tasksmanager-backend-api `
            --source ghcr.io/azure/tasksmanager-backend-api:latest
            
            az acr import  `
            --name $CONTAINER_REGISTRY_NAME `
            --image tasksmanager/tasksmanager-frontend-webapp `
            --source ghcr.io/azure/tasksmanager-frontend-webapp:latest
            
            az acr import  `
            --name $CONTAINER_REGISTRY_NAME `
            --image tasksmanager/tasksmanager-backend-processor `
            --source ghcr.io/azure/tasksmanager-backend-processor:latest

        ```

    3. Update the `main.parameters.json` file with the container registry name and the container images names as shown below:

        ```json hl_lines="3 6 9 12"
        {
            "containerRegistryName": {
                "value": "<CONTAINER_REGISTRY_NAME>"
            },
            "backendProcessorServiceImage": {
                "value": "<CONTAINER_REGISTRY_NAME>.azurecr.io/tasksmanager/tasksmanager-backend-processor:latest"
            },
            "backendApiServiceImage": {
                "value": "<CONTAINER_REGISTRY_NAME>.azurecr.io/tasksmanager/tasksmanager-backend-api:latest"
            },
            "frontendWebAppServiceImage": {
                "value": "<CONTAINER_REGISTRY_NAME>.azurecr.io/tasksmanager/tasksmanager-frontend-webapp:latest"
            }
        }
        ```

=== "Option 3: Use the pre-built images from the public repository"

    All the container image are available in a public image repository. If you do not wish to build the container images from code directly, you can use the pre-built images from the public repository as shown below.

    The public images can be set directly in the `main.parameters.json` file:

    ```json hl_lines="3 6 9 12"
    {
        "containerRegistryName": {
            "value": ""
        },
        "backendProcessorServiceImage": {
          "value": "ghcr.io/azure/tasksmanager-backend-processor:latest"
        },
        "backendApiServiceImage": {
          "value": "ghcr.io/azure/tasksmanager-backend-api:latest"
        },
        "frontendWebAppServiceImage": {
          "value": "ghcr.io/azure/tasksmanager-frontend-webapp:latest"
        },
    }   
    ```

Start the deployment by calling `az deployment group create`. To accomplish this, open the PowerShell console and use the content below.


    ```Powershell
    az deployment group create `
    --resource-group $RESOURCE_GROUP `
    --template-file "./bicep/main.bicep" `
    --parameters "./bicep/main.parameters.json"
    ```

The Azure CLI will take the Bicep module and start creating the deployment in the resource group.

### Verify the Final Results

!!! success
    Upon successful deployment, you should observe all resources generated within the designated resource group. Additionally, you may navigate to the `Deployments` section to confirm that the ARM templates have been deployed, which should resemble the image provided below:

    ![aca-deployment](../../assets/images/10-aca-iac-bicep/aca-deployment.jpg)
