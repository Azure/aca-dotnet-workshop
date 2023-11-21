---
canonical_url: https://bitoftech.net/2022/09/05/azure-container-apps-with-dapr-bindings-building-block/
---

# Module 6 - ACA with Dapr Bindings Building Block

!!! info "Module Duration"
    90 minutes

## Objective

In this module, we will accomplish four objectives:

1. Learn how to interface with external systems.
1. Extend the backend background processor service (`ACA-Processor Backend`) to interface with an external system.
1. Use Azure Key Vault via a Dapr Secret Store Component to externalize secrets.
1. Deploy updated revisions for Backend Background Processor App.

## Module Sections

--8<-- "snippets/restore-variables.md"

### 1. Interfacing with an External System

To achieve interfacing with an external system in a simple way, we will utilize [Dapr Input and Output Bindings](https://docs.dapr.io/developing-applications/building-blocks/bindings/bindings-overview/){target=_blank}.

The external system owns an Azure Storage Queue which the Tasks Tracker microservice application **reacts** to through an event handler (aka **Input Binding**). This event handler receives and processes the message coming to the storage queue. Once the processing of the message completes and stores the task into Cosmos DB, the system will **trigger** an event (aka **Output binding**) that invokes the external service. This service, in turn, stores the content of the message into an Azure Blob Storage container. It is important to emphasize that both the Azure Storage Queue and the Azure Storage Blob belong to the *external* system.

The rest of this module will implement the three scenarios mentioned below:

- Trigger a process on the `ACA-Processor Backend` based on a **message sent to a specific Azure Storage Queue**. This scenario will assume that the Azure Storage Queue is an external system to which external clients can submit tasks.
- From the service `ACA-Processor Backend` we will **invoke an external resource** that stores the content of the incoming task from the external queue as a JSON blob file on Azure Storage Blobs.
- Remove the SendGrid SDK as well as the custom code created in the previous module to send emails and replace it with [Dapr SendGrid output binding.](https://docs.dapr.io/reference/components-reference/supported-bindings/sendgrid/){target=_blank}

Take a look at the high-level architecture diagram below to understand the flow of input and output bindings in Dapr:

![simple-binding-arch](../../assets/images/06-aca-dapr-bindingsapi/simple-binding.jpg)

!!! note
    Those 3rd party external services could be services hosted on another cloud provider, different Azure subscription, or even on premise. Dapr bindings are usually used to trigger an application with events coming in from external systems as well as interface with external systems.

    For simplicity of the workshop we are going to host those two supposedly external services in the same subscription of our Tasks Tracker microservice application.

If you look at Dapr Bindings Building Block, you will notice a lot of similarities with the Pub/Sub Building Block that we covered in the [previous module](../../aca/05-aca-dapr-pubsubapi/index.md). But remember that Pub/Sub Building Block is meant to be used for async communication between services **within your solution**. The Binding Building Block has a wider scope and it mainly focuses on connectivity and interoperability across different systems, disparate applications, and services outside the boundaries of your own application. For a full list of [supported bindings](https://docs.dapr.io/reference/components-reference/supported-bindings/){target=_blank} visit this link.

#### 1.1 Overview of Dapr Bindings Building Block

Let's take a look at the detailed Dapr Bindings Building Block architecture diagram that we are going to implement in this module to fulfill the use case we discussed earlier:
![detailed-binding-arch](../../assets/images/06-aca-dapr-bindingsapi/detailed-binding.jpg)

Looking at the diagram we notice the following:

- In order to receive events and data from the external resource (Azure Storage Queue) our `ACA-Processor Backend` service needs to register a public endpoint that will become an event handler.
- This binding configuration between the external resource and our service will be configured by using the `Input Binding Configuration Yaml` file. The Dapr sidecar of the background service will read the configuration and subscribe to the endpoint defined for the external resource. In our case, it will be a specific Azure Storage Queue.
- When a message is published to the storage queue, the input binding component running in the Dapr sidecar picks it up and triggers the event.
- The Dapr sidecar invokes the endpoint (event handler defined in the `ACA-Processor Backend` Service) configured for the binding. In our case, it will be an endpoint that can be reached by invoking a `POST` operation `http://localhost:3502/ExternalTasksProcessor/Process` and the request body content will be the JSON payload of the published message to the Azure Storage Queue.
- When the event is handled in our `ACA-Processor Backend` and the business logic is completed, this endpoint needs to return an HTTP response with a `200 OK` status to acknowledge that processing is complete. If the event handling is not completed or there is an error, this endpoint should return an HTTP 4xx or 5xx status code.
- In order to enable the service `ACA-Processor Backend` to trigger an event that invokes an external resource, we need to use the `Output Binding Configuration Yaml` file to configure the binding between our service and the external resource (Azure Blob Storage) and how to connect to it.
- Once the Dapr sidecar reads the binding configuration file, our service can trigger an event that invokes the output binding API on the Dapr sidecar. In our case, the event will be creating a new blob file containing the content of the message we read earlier from the Azure Storage Queue.
- With this in place, our service `ACA-Processor Backend` will be ready to invoke the external resource by sending a **POST** operation to the endpoint `http://localhost:3502/v1.0/bindings/ExternalTasksBlobstore` and the JSON payload will contain the content below. Alternatively, we can use the Dapr client SDK to invoke this output biding to invoke the external service and store the file in Azure Blob Storage.

    ```json
    {
        "data": "{
            "taskName": "Task Coming from External System",
            "taskAssignedTo": "user1@hotmail.com",
            "taskCreatedBy": "tjoudeh@bitoftech.net",
            "taskDueDate": "2022-08-19T12:45:22.0983978Z"
        }",
        "operation": "create"
    }
    ```

Let's start by updating our Backend Background Processor project and define the input and output bindings configuration files and event handlers.

To proceed with this workshop, we need to provision the Azure Storage Account to start responding to messages published to a queue and then later use the same storage account to store blob files as an external event.
Run the PowerShell script below to create Azure Storage Account and get the master key.

!!! tip
    We will be retrieving the storage account key for local dev testing purposes. Note that the command below will return two keys. You will only need one of them for this exercise.
    When deploying the changes to ACA, we are going to store the storage key securely into Azure Key Vault using [Dapr Secrets Store Building Block with AKV](https://docs.dapr.io/reference/components-reference/supported-secret-stores/azure-keyvault/){target=_blank}.

    We didn't use Azure Manged Identity here because the assumption is that those services are not part of our solution and thus they could theoretically be a non AD compliant services or hosted on another cloud. 
    If these services where part of your application's ecosystem it is always recommended that you use Azure Managed Identity.

```shell
$STORAGE_ACCOUNT_NAME = "sttaskstracker$RANDOM_STRING"
    
az storage account create `
--name $STORAGE_ACCOUNT_NAME `
--resource-group $RESOURCE_GROUP `
--location $LOCATION `
--sku Standard_LRS `
--kind StorageV2
    
# List Azure storage keys
az storage account keys list `
--resource-group $RESOURCE_GROUP `
--account-name $STORAGE_ACCOUNT_NAME

# Get the primary storage account key
$STORAGE_ACCOUNT_KEY=($(az storage account keys list `
--resource-group $RESOURCE_GROUP `
--account-name $STORAGE_ACCOUNT_NAME ) | ConvertFrom-Json)[0].value

echo "Storage Account Name : $STORAGE_ACCOUNT_NAME"
echo "Storage Account Key  : $STORAGE_ACCOUNT_KEY"
```

### 2. Updating the Backend Background Processor Project

#### 2.1 Create an event handler (API endpoint) to respond to messages published to Azure Storage Queue

Let's add an endpoint that will be responsible to handle the event when a message is published to Azure Storage Queue. This endpoint will start receiving the message published from the external service.

Start by adding a new controller **Controllers** folder under the **TasksTracker.Processor.Backend.Svc** project:

=== "ExternalTasksProcessorController.cs"

    ```csharp
    --8<-- "docs/aca/06-aca-dapr-bindingsapi/ExternalTasksProcessorController.cs"
    ```

??? tip "Curious to know more about the code?"

    - We defined an action method named `ProcessTaskAndStore` which can be accessed by sending HTTP POST operation on the 
    endpoint `ExternalTasksProcessor/Process`. 
    
    - This action method accepts the TaskModel in the request body as JSON payload.This is what will be received from the external service (Azure Storage Queue). 
    
    - Within this action method, we are going to store the received task by sending a POST request to `/api/tasks` which is part of the backend api named `tasksmanager-backend-api`.

    - Then we return `200 OK` to acknowledge that message received is processed successfully and should be removed from the external service queue.

#### 2.2 Create Dapr Input Binding Component File

Now we need to create the component configuration file which will describe the configuration as well as how our backend background processor will start handling events coming from the external service (Azure Storage Queues). To do so, add a new file under **components** folder.

=== "dapr-bindings-in-storagequeue.yaml"

    ```yaml
    --8<-- "docs/aca/06-aca-dapr-bindingsapi/dapr-bindings-in-storagequeue.yaml"
    ```

??? tip "Curious to learn more about the specification of yaml file?"

    The full specifications of yaml file with Azure Storage Queues can be found on [this link](https://docs.dapr.io/reference/components-reference/supported-bindings/storagequeues/){target=_blank}, but let's go over the configuration we have added here:

    - The type of binding is `bindings.azure.storagequeues`.
    - The name of this input binding is `externaltasksmanager`.
    - We are setting the `storageAccount` name, `storageAccessKey` value, and the `queue` name. Those properties will describe how the event handler we added can connect to the external service.
    You can create any queue you prefer on the Azure Storage Account we created to simulate an external system.
    - We are setting the `route` property to the value `/externaltasksprocessor/process` which is the address of the endpoint we have just added so POST requests are sent to this endpoint.
    - We are setting the property `decodeBase64` to `true` as the message queued in the Azure Storage Queue is Base64 encoded.

!!! note
    The value of the Metadata `storageAccessKey` is used as plain text here for local dev scenario. We will see how we are going to store this key securely in Azure Key Vault and use Dapr Secrets Store API to read the access key.

#### 2.3 Create Dapr Output Binding Component File

Now we need to create the component configuration file which will describe the configuration and how our service `ACA-Processor Backend` will be able to invoke the external service (Azure Blob Storage) and be able to create and store a JSON blob file that contains the content of the message received from Azure Storage Queues.

To do so, add a new file folder **components**.

=== "dapr-bindings-out-blobstorage.yaml"

    ```yaml
    --8<-- "docs/aca/06-aca-dapr-bindingsapi/dapr-bindings-out-blobstorage.yaml"
    ```

??? tip "Curious to learn more about the specification of yaml file?"

    The full specifications of yaml file with Azure blob storage can be found on [this link](https://docs.dapr.io/reference/components-reference/supported-bindings/blobstorage/){target=_blank}, 
    but let's go over the configuration we have added here:

    - The type of binding is `bindings.azure.blobstorage`.
    - The name of this output binding is `externaltasksblobstore`. We will use this name when we use the Dapr SDK to trigger the output binding.
    - We are setting the `storageAccount` name, `storageAccessKey` value, and the `container` name. Those properties will describe how our backend background service will be able to connect to the external
    service and create a blob file. We will assume that there is a container already created on the external service and named `externaltaskscontainer` as shown in the image below
    
    ![Storage-Account-Container](../../assets/images/06-aca-dapr-bindingsapi/StorageAccountContainer.png)

    - We are setting the property `decodeBase64`  to `false` as we don't want to encode file content to base64 images, we need to store the file content as is.

#### 2.4 Use Dapr client SDK to Invoke the Output Binding

Now we need to invoke the output binding by using the .NET SDK.

Update and replace the code in the file with the code below. Pay close attention to the updated **ProcessTaskAndStore** action method:

=== "ExternalTasksProcessorController.cs"

    ```csharp hl_lines="13-14 37-45"
    --8<-- "docs/aca/06-aca-dapr-bindingsapi/Update.ExternalTasksProcessorController.cs"
    ```

??? tip "Curious to know more about the code?"

    Looking at the `ProcessTaskAndStore` action method above, you will see that we are calling the method `InvokeBindingAsync` and we are passing the binding name `externaltasksblobstore` 
    defined in the configuration file, as well the second parameter `create` which is the action we need to carry against the external blob storage. 

    You can for example delete or get a content of a certain file. For a full list of supported actions on Azure Blob Storage, [visit this link](https://docs.dapr.io/reference/components-reference/supported-bindings/blobstorage/#binding-support){target=_blank}.

    Notice how are setting the file name we are storing at the external service. We need the file names to be created using the same Task Identifier, so we will pass the key `blobName` with the file name values 
    into the `metaData` dictionary.

#### 2.5 Test Dapr Bindings Locally

--8<-- "snippets/update-variables.md::5"

Now we are ready to give it an end-to-end test on our dev machines. To do so, run the below commands in three separate PowerShell console, ensure you are on the right root folder of each respective project.

--8<-- "snippets/restore-variables.md:7:11"

=== ".NET 6 or below"

    --8<-- "snippets/dapr-run-frontend-webapp.md:basic-dotnet6"
    --8<-- "snippets/dapr-run-backend-api.md:dapr-components-dotnet6"
    --8<-- "snippets/dapr-run-backend-service.md:dapr-components-dotnet6"

=== ".NET 7 or above"

    --8<-- "snippets/dapr-run-frontend-webapp.md:basic"
    --8<-- "snippets/dapr-run-backend-api.md:dapr-components"
    --8<-- "snippets/dapr-run-backend-service.md:dapr-components"

<!-- Run the 3 applications together using Debug and Run button from VS Code. You can read how we configured the 3 apps to run together
in this [section](../30-appendix/01-run-debug-dapr-app-vscode.md). -->

Open Azure Storage Explorer on your local machine. If you don't have it installed you can install it from [here](https://azure.microsoft.com/en-us/products/storage/storage-explorer/#overview){target=_blank}.
Login to your Azure Subscription and navigate to the storage account already created, create a queue, and use the same name you already used in the Dapr Input configuration file.
In our case the name of the queue in the configuration file is `external-tasks-queue`.

![Azure-storage-explorer](../../assets/images/06-aca-dapr-bindingsapi/Azure_Storage_Explorer.png)

The content of the message that Azure Storage Queue excepts should be as below, so try to queue a new message using the tool as the image below:

```json
{
    "taskName": "Task from External System",
    "taskAssignedTo": "user1@hotmail.com",
    "taskCreatedBy": "tjoudeh@bitoftech.net",
    "taskDueDate": "2022-08-19T12:45:22.0983978Z"
}
```

![storage-queue](../../assets/images/06-aca-dapr-bindingsapi/storage-queue.jpg)

If all is configured successfully you should be able to see a JSON file created as a blob in the Azure Storage Container named `externaltaskscontainer` based on your configuration.

![storage-queue](../../assets/images/06-aca-dapr-bindingsapi/blob-storage.jpg)

### 3. Configure Dapr Secret Store Component with Azure Key Vault

Currently, we have three Dapr components which are not Microsoft Entra ID enabled services. As you may have noticed so far, the different component files are storing sensitive keys to access the different external services.
The recommended approach for retrieving these secrets is to reference an existing Dapr secret store component that securely accesses the secrets.

We need Create a [Dapr secret store component](https://docs.dapr.io/developing-applications/building-blocks/secrets/secrets-overview/){target=_blank} using the Container Apps schema. The Dapr secret store will be configured
with [Azure Key Vault secret store](https://docs.dapr.io/reference/components-reference/supported-secret-stores/azure-keyvault/){target=_blank}.

#### 3.1 Create an Azure Key Vault resource

Create an Azure Key Vault which will be used to store securely any secret or key used in our application.

```shell
$KEYVAULT_NAME = "kv-tasks-tracker-$RANDOM_STRING"

az keyvault create `
--name $KEYVAULT_NAME `
--resource-group $RESOURCE_GROUP `
--location $LOCATION `
--enable-rbac-authorization true
```

!!! note
    It is important to create the Azure Key Vault with Azure RBAC for authorization by setting `--enable-rbac-authorization true` because the role we are going to assign to the Microsoft Entra ID application will work only when RBAC authorization is enabled.

#### 3.2 Grant Backend Processor App a Role To Read Secrets from Azure Key Vault

In the previous module we have configured the `system-assigned` identity for the service `ACA-Processor Backend`. Now we need to assign a role named `Key Vault Secrets User` to it, so it access and read secrets from Azure Key Vault.

You can read more about [Azure built-in roles for Key Vault data plane operations](https://learn.microsoft.com/en-us/azure/key-vault/general/rbac-guide?tabs=azure-cli#azure-built-in-roles-for-key-vault-data-plane-operations){target=_blank}.

```shell
$KEYVAULT_SECRETS_USER_ROLE_ID = "4633458b-17de-408a-b874-0445c86b69e6" # ID for 'Key Vault Secrets User' Role

# Get PRINCIPAL ID of BACKEND Processor Service
$BACKEND_SERVICE_PRINCIPAL_ID = az containerapp show `
--name $BACKEND_SERVICE_NAME `
--resource-group $RESOURCE_GROUP `
--query identity.principalId

az role assignment create `
--role $KEYVAULT_SECRETS_USER_ROLE_ID `
--assignee $BACKEND_SERVICE_PRINCIPAL_ID `
--scope "/subscriptions/$AZURE_SUBSCRIPTION_ID/resourcegroups/$RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/$KEYVAULT_NAME"
```

#### 3.3 Create Secrets in the Azure Key Vault

To create a secret in Azure Key Vault you need to have a role which allows you to create secrets. From the Azure CLI we will assign the role `Key Vault Secrets Officer` to the user signed in to AZ CLI to
be able to create secrets. To do so use the script below:

```shell
$SIGNEDIN_USERID = az ad signed-in-user show --query id
$KEYVAULT_SECRETS_OFFICER_ROLE_ID = "b86a8fe4-44ce-4948-aee5-eccb2c155cd7" # ID for 'Key Vault Secrets Office' Role 

az role assignment create `
--role $KEYVAULT_SECRETS_OFFICER_ROLE_ID `
--assignee $SIGNEDIN_USERID `
--scope "/subscriptions/$AZURE_SUBSCRIPTION_ID/resourcegroups/$RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/$KEYVAULT_NAME"
```

Now we will create the secrets in the Azure Key Vault using the commands below:

```shell
# Set External Azure Storage Access Key as a secret named 'external-azure-storage-key'
az keyvault secret set `
--vault-name $KEYVAULT_NAME `
--name "external-azure-storage-key" `
--value $STORAGE_ACCOUNT_KEY
```

#### 3.4 Create a ACA Dapr Secrets Store Component file

Obtain the name of the Key Vault.

```shell
$KEYVAULT_NAME
```

Create a new yaml file under the **aca-components** folder.

=== "containerapps-secretstore-kv.yaml"

    ```yaml
    --8<-- "docs/aca/06-aca-dapr-bindingsapi/containerapps-secretstore-kv.yaml"
    ```

??? tip "Curious to learn more about the yaml file?"

    - We didn't specify the component name `secretstoreakv` in the metadata of the this component yaml file. We are going to specify it once we add this dapr component to Azure Container Apps Environment 
    via CLI similar to what we did in earlier modules.
    - We are not referencing any service bus connection strings as the authentication between Dapr and Azure Service Bus will be configured using Managed Identities. 
    - The metadata `vaultName` value is set to the name of the Azure Key Vault we've just created. 
    - We are allowing this component only to be accessed by the dapr with application id `tasksmanager-backend-processor`. This means that our Backend API or Frontend Web App services will not be able
    to access the Dapr secret store. If we want to allow them to access the secrets we need to update this component file and grant the system-identity of those services a `Key Vault Secrets User` role.

#### 3.5 Create Input and Output Binding Component Files Matching Azure Container Apps Specs

Add new files under the **aca-components** use the yaml below:

=== "containerapps-bindings-in-storagequeue.yaml"

    ```yaml
    --8<-- "docs/aca/06-aca-dapr-bindingsapi/containerapps-bindings-in-storagequeue.yaml"
    ```
    ??? tip "Curious to learn more about the yaml file?"
    
        The properties of this file are matching the ones used in Dapr component-specific file. It is a component of type `bindings.azure.storagequeues`. 
        The only differences are the following: 
    
        - We are setting the property `secretStoreComponent` value to `secretstoreakv` which is the name of Dapr secret store component.
        - We are using `secretRef` when setting the metadata `storageAccessKey`. The value `external-azure-storage-key` represents the AKV secret created earlier.

=== "containerapps-bindings-out-blobstorage.yaml"

    ```yaml
    --8<-- "docs/aca/06-aca-dapr-bindingsapi/containerapps-bindings-out-blobstorage.yaml"
    ```
    ??? tip "Curious to learn more about the yaml file?"

        The properties of this file are matching the ones used in Dapr component-specific file. It is a component of type `bindings.azure.blobstorage`. 
        The only differences are the following:
    
        - We are setting the property `secretStoreComponent` value to `secretstoreakv` which is the name of Dapr secret store component.
        - We are using `secretRef` when setting the metadata `storageAccessKey`. The value `external-azure-storage-key` represents the AKV secret created earlier

With those changes in place, we are ready to rebuild the backend background processor container image, update Azure Container Apps Env, and redeploy a new revision.

### 4. Deploy a New Revision of the Backend Background Processor App to ACA

#### 4.1 Build the Backend Background Processor Image and Push it To ACR

As we have done previously we need to build and deploy the Backend Background Processor image to ACR, so it is ready to be deployed to ACA.
Continue using the same PowerShell console and paste the code below (make sure you are under the  **TasksTracker.ContainerApps** directory):

```shell
dotnet publish --project TasksTracker.Processor.Backend.Svc `
-t:PublishContainer `
-p ContainerRegistry=$AZURE_CONTAINER_REGISTRY_NAME `
-p ContainerRepository=tasksmanager/$BACKEND_SERVICE_NAME
```

#### 4.2 Add Dapr Secret Store Component to ACA Environment

We need to run the command below from the root to create the Dapr secret store component:

```shell
az containerapp env dapr-component set `
--name $ENVIRONMENT `
--resource-group $RESOURCE_GROUP `
--dapr-component-name secretstoreakv `
--yaml '.\aca-components\containerapps-secretstore-kv.yaml'
```

#### 4.3 Add the Bindings Dapr Components to ACA Environment

Next, we will add the Dapr bindings components using the component files created.

```shell
# Input binding component for Azure Storage Queue
az containerapp env dapr-component set `
--name $ENVIRONMENT `
--resource-group $RESOURCE_GROUP `
--dapr-component-name externaltasksmanager `
--yaml '.\aca-components\containerapps-bindings-in-storagequeue.yaml'
    
# Output binding component for Azure Blob Storage
az containerapp env dapr-component set `
--name $ENVIRONMENT `
--resource-group $RESOURCE_GROUP `
--dapr-component-name externaltasksblobstore `
--yaml '.\aca-components\containerapps-bindings-out-blobstorage.yaml'
```

#### 4.4 Deploy new revisions of the Backend Background Processor to ACA

Update the Azure Container App hosting the Backend Background Processor with a new revision so our code changes are available for end users.

```shell
# Update Backend Background Processor container app and create a new revision 
az containerapp update `
--name $BACKEND_SERVICE_NAME `
--resource-group $RESOURCE_GROUP `
--revision-suffix v$TODAY-3 `
```

!!! success
    With those changes in place and deployed, from the [Azure portal](https://portal.azure.com){target=_blank} you can open the log streams section of the container app hosting the `ACA-Processor-Backend` and check the logs generated after queuing a message into Azure Storage Queue (using Azure Storage Explorer tool used earlier) as an external system.

    ```json
    {
      "taskName": "Task from External System",
      "taskAssignedTo": "user42@hotmail.com",
      "taskCreatedBy": "tjoudeh@bitoftech.net",
      "taskDueDate": "2022-08-19T12:45:22.0983978Z"
    }
    ```

    You should receive logs similar to the below:

    ![app-logs](../../assets/images/06-aca-dapr-bindingsapi/app-logs.png)

--8<-- "snippets/update-variables.md"
--8<-- "snippets/persist-state.md:module6"

## Review

In this module, we have accomplished four objectives:

1. Learned how to interface with external systems.
1. Extended the backend background processor service (`ACA-Processor Backend`) to interface with an external system.
1. Used Azure Key Vault via a Dapr Secret Store Component to externalize secrets.
1. Deployed updated revisions for Backend Background Processor App.

In the next module, we will cover a special type of Dapr input binding named Cron Binding.
