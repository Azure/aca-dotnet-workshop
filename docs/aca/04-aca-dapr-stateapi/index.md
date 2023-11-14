---
canonical_url: https://bitoftech.net/2022/08/29/azure-container-apps-state-store-with-dapr-state-management-api/
---

# Module 4 - ACA State Store With Dapr State Management API

!!! info "Module Duration"
    60 minutes

## Objective

In this module, we will accomplish three objectives:

1. Learn about Dapr State Management with Redis Cache.
2. Use the Dapr Client SDK.
3. Provision Azure Cosmos DB resources & Update app and API in Azure.

## Module Sections

--8<-- "snippets/restore-variables.md"

### 1. State Management

#### 1.1 Overview of Dapr State Management API

By using the [Dapr State Management Building Block](https://docs.dapr.io/developing-applications/building-blocks/state-management/state-management-overview/){target=_blank}, we will see how we can store the data in Azure Cosmos DB without installing any Cosmos DB SDK or write specific code to integrate our Backend API with Azure Cosmos DB.
Moreover, we will use Redis to store tasks when we are running the application locally. You will see that we can switch between different stores without any code changes, thanks to the [Dapr pluggable state stores feature](https://docs.dapr.io/developing-applications/building-blocks/state-management/state-management-overview/#pluggable-state-stores){target=_blank}. It is a matter of adding new Dapr Component files and the underlying store will be changed. This page shows the [supported state stores](https://docs.dapr.io/reference/components-reference/supported-state-stores/){target=_blank} in Dapr.

![dapr-stateapi-cosmosdb](../../assets/images/04-aca-dapr-stateapi/dapr-stateapi-cosmosdb.jpg)

Dapr's state management API allows you to save, read, and query key/value pairs in the supported state stores. To try this out, and without doing any code changes or installing any NuGet packages, we can directly invoke the State Management API and store the data on Redis locally. When you initialized Dapr in your local development environment, it installed Redis container instance locally. So we can use Redis locally to store and retrieve state. If you navigate to the path `%USERPROFILE%\.dapr\components` (assuming you are using Windows) you will find a file named `statestore.yaml`. Inside this file, you will see the properties needed to access the local Redis instance. The [state store template component file structure](https://docs.dapr.io/operations/components/setup-state-store/){target=_blank} can be found on this link.

To try out the State Management APIs, run the Backend API from VS Code by running the following command.

=== ".NET 6 or below"

    --8<-- "snippets/dapr-run-backend-api.md:basic-dotnet6"

=== ".NET 7 or above"

    --8<-- "snippets/dapr-run-backend-api.md:basic"

Now from any rest client, invoke the below **POST** request to the endpoint: [http://localhost:3500/v1.0/state/statestore](http://localhost:3500/v1.0/state/statestore){target=_blank}

```http
POST /v1.0/state/statestore HTTP/1.1
Host: localhost:3500
Content-Type: application/json
[
    {
        "key": "Book1",
        "value": {
            "title": "Parallel and High Performance Computing",
            "author": "Robert Robey",
            "genre": "Technical"
        }
    },
    {
        "key": "Book2",
        "value": {
            "title": "Software Engineering Best Practices",
            "author": "Capers Jones",
            "genre": "Technical"
        }
    },
    {
        "key": "Book3",
        "value": {
            "title": "The Unstoppable Mindset",
            "author": "Jessica Marks",
            "genre": "Self Improvement",
            "formats":["kindle", "audiobook", "papercover"]
        }
    }
]
```

You should see an *HTTP 204 No Content* response.

What we've done here is the following:

- The value `statestore` in the endpoint should match the `name` value in the global component file `statestore.yaml`
- We have sent a request to store 3 entries of books, you can put any JSON representation in the value property

#### 1.2 Local Redis Cache 

To see the results visually, you can install a VS Code extension to connect to Redis DB and see the results. There are several Redis extensions available for VS Code. For this workshop we will use an extension named ["Redis Xplorer"](https://marketplace.visualstudio.com/items?itemName=davidsekar.redis-xplorer){target=_blank}.

Once you install the extension it will add a tab under the explorer section of VS Code called "REDIS XPLORER". Next you will need to connect to the Redis server locally by adding a new "REDIS XPLORER" profile. Click on the + sign in the "REDIS XPLORER" section in VS Code.
This will ask you to enter the nickname (e.g. *dapr_redis*) as well as the hostname and port. For the hostname and port you can get this information by executing the following command in your powershell terminal:

```shell
docker ps
```

Look under the Ports column and use the server and port specified there. In the image below the server is `0.0.0.0` and the port is `6379`. Use the values that you see on your own terminal. Leave the password empty.

![dapr-stateapi-redis](../../assets/images/04-aca-dapr-stateapi/docker_redis.png)

After you connect to Redis locally, you should see the 3 entries similar to the ones shown in the image below. Notice how each entry key is prefixed by the Dapr App Id. In our case it is `tasksmanager-backend-api`. More about [key prefix strategy](#key-prefix-strategies) in later sections in this module.

![dapr-stateapi-redis](../../assets/images/04-aca-dapr-stateapi/dapr-api-redis.jpg)

To get the value of a key, you need to issue a GET request to the endpoint `http://localhost:3500/v1.0/state/statestore/{YourKey}`. This will return the value from the key store.
For example if you execute the following GET [http://localhost:3500/v1.0/state/statestore/Book3](http://localhost:3500/v1.0/state/statestore/Book3){target=_blank} the results will be the below object:

```JSON
{
    "formats": [
        "kindle",
        "audiobook",
        "papercover"
    ],
    "title": "The Unstoppable Mindset",
    "author": "Jessica Marks",
    "genre": "Self Improvement"
}
```

### 2. Use Dapr Client SDK For State Store Management

Whereas in the previous section we demonstrated using Dapr State Store without code changes, we will now introduce a change on the Backend API and create a new service named `TasksStoreManager.cs` which will implement the interface `ITasksManager.cs` to start storing tasks data on the persist store. Locally, we will start testing with Redis, then we are going to change the state store to use Azure Cosmos DB.

#### 2.1 Add Dapr Client SDK With The Backend API

Similar to what we have done in the Frontend Web App, we need to use Dapr Client SDK to manage the state store. Update the below file with the added Dapr package reference:

=== ".NET 6"
    === "TasksTracker.TasksManager.Backend.Api.csproj"

        ```xml
        <ItemGroup>
            <PackageReference Include="Dapr.AspNetCore" Version="{{ dapr.version }}" />
        </ItemGroup>
        ```

=== ".NET 7"
    === "TasksTracker.TasksManager.Backend.Api.csproj"

        ```xml hl_lines="10"
        --8<-- "docs/aca/04-aca-dapr-stateapi/Backend.Api-dotnet7.csproj"
        ```

=== ".NET 8"
    === "TasksTracker.TasksManager.Backend.Api.csproj"

        ```xml hl_lines="10"
        --8<-- "docs/aca/04-aca-dapr-stateapi/Backend.Api-dotnet8.csproj"
        ```

#### 2.2 Create a New Concrete Implementation to Manage Tasks Persistence

As you recall from the previous module, we were storing the tasks in memory. Now we need to store them in Redis and, later on, Azure Cosmos DB. The key thing to keep in mind here is that switching from Redis to Azure Cosmos DB won't require changing the code below which is a huge advantage of using Dapr.

Add below file to the **Services** folder. This file will implement the interface `ITasksManager`.

=== "TasksStoreManager.cs"

    ```csharp
    --8<-- "docs/aca/04-aca-dapr-stateapi/TasksStoreManager.cs"
    ```

??? info "Curious about the code?"
    Looking at the code above, we have injected the `DaprClient` into the new service and DaprClient has a set of [methods to support CRUD operations](https://docs.dapr.io/developing-applications/building-blocks/state-management/howto-get-save-state/){target=_blank}.
    Notice how we are using the state store named `statestore`  which should match the name in the component file.

!!! note
    The query API will not work against the local Redis store as you need to install [RediSearch](https://redis.io/docs/stack/search/){target=_blank} locally on your machine which is out of the scope for this workshop.
    It will work locally once we switch to Azure Cosmos DB.

#### 2.3 Register the TasksStoreManager New Service and DaprClient

Now we need to register the new service named `TasksStoreManager` and `DaprClient` when the Backend API app starts up. Update the below file with the highlighted text as shown below.

!!! note
    Do not forget to comment out the registration of the `FakeTasksManager` service as we don't want to store tasks in memory anymore.

=== "Program.cs"

    ```csharp hl_lines="7-9"
    --8<-- "docs/aca/04-aca-dapr-stateapi/Program.cs"
    ```

- Let's verify that the Dapr dependency is restored properly and that the project compiles. From VS Code Terminal tab, open developer command prompt or PowerShell terminal and navigate to the parent directory which hosts the `.csproj` project folder and build the project.

    ```shell
    cd ~\TasksTracker.ContainerApps\TasksTracker.TasksManager.Backend.Api
    dotnet build
    ```

Now you are ready to run both applications and debug them. You can store new tasks, update them, delete existing tasks and mark them as completed. The data should be stored on your local Redis instance.

!!! info
    For now don't try running the application as you will get an error running the query against the local Redis. As mentioned earlier setting up the local Redis store is out of scope for this workshop.
    Instead, we will focus on wiring the Azure Cosmos DB as the store for our tasks.

### 3. Use Azure Cosmos DB with Dapr State Store Management API

#### 3.1 Provision Cosmos DB Resources

Now we will create an Azure Cosmos DB account, Database, and a new container that will store our tasks.
You can use the PowerShell script below to create the Cosmos DB resources on the same resource group we used in the previous module.
You need to set the variable name of the `$COSMOS_DB_ACCOUNT` to a unique name as it needs to be unique globally. Remember to replace the placeholders with your own values:

```shell
$COSMOS_DB_ACCOUNT="cosmos-tasks-tracker-state-store-$RANDOM_STRING"
$COSMOS_DB_DBNAME="tasksmanagerdb"
$COSMOS_DB_CONTAINER="taskscollection" 

# Check if Cosmos account name already exists globally
$result = az cosmosdb check-name-exists `
--name $COSMOS_DB_ACCOUNT

# Continue if the Cosmos DB account does not yet exist
if ($result -eq "false") {
    echo "Creating Cosmos DB account..."

    # Create a Cosmos account for SQL API
    az cosmosdb create `
    --name $COSMOS_DB_ACCOUNT `
    --resource-group $RESOURCE_GROUP
    
    # Create a SQL API database
    az cosmosdb sql database create `
    --name $COSMOS_DB_DBNAME `
    --resource-group $RESOURCE_GROUP `
    --account-name $COSMOS_DB_ACCOUNT
    
    # Create a SQL API container
    az cosmosdb sql container create `
    --name $COSMOS_DB_CONTAINER `
    --resource-group $RESOURCE_GROUP `
    --account-name $COSMOS_DB_ACCOUNT `
    --database-name $COSMOS_DB_DBNAME `
    --partition-key-path "/id" `
    --throughput 400
    
    $COSMOS_DB_ENDPOINT=(az cosmosdb show `
    --name $COSMOS_DB_ACCOUNT `
    --resource-group $RESOURCE_GROUP `
    --query documentEndpoint `
    --output tsv)
    
    echo "Cosmos DB Endpoint: "
    echo $COSMOS_DB_ENDPOINT
}
```

!!! note
    The `primaryMasterKey` connection string is only needed for our local testing on the development machine, we'll be using a different approach (**Managed Identities**) when deploying Dapr component to Azure Container Apps Environment.

Once the scripts execution is completed, we need to get the `primaryMasterKey` of the Cosmos DB account next. You can do this using the PowerShell script below.
Copy the value of `primaryMasterKey` as we will use it in the next step.

```shell
# List Azure Cosmos DB keys
$COSMOS_DB_PRIMARY_MASTER_KEY=(az cosmosdb keys list `
--name $COSMOS_DB_ACCOUNT `
--resource-group $RESOURCE_GROUP `
--query primaryMasterKey `
--output tsv)

echo "Cosmos DB Primary Master Key:"
echo $COSMOS_DB_PRIMARY_MASTER_KEY
```

#### 3.2. Create a Component File for State Store Management 

Dapr uses a modular design where functionality is delivered as a component. Each component has an interface definition.
All the components are pluggable so that you can swap out one component with the same interface for another.

Components are configured at design-time with a YAML file which is stored in either a components/local folder within your solution, or globally in the `.dapr` folder created when invoking `dapr init`.
These YAML files adhere to the generic [Dapr component schema](https://docs.dapr.io/operations/components/component-schema/){target=_blank}, but each is specific to the component specification.

It is important to understand that the component spec values, particularly the spec `metadata`, can change between components of the same component type.
As a result, it is strongly recommended to review a component's specs, paying particular attention to the sample payloads for requests to set the metadata used to interact with the component.

The diagram below is from Dapr official documentation which shows some examples of the components for each component type. We are now looking at the State Stores components. Specifically the one for [Azure Cosmos DB](https://docs.dapr.io/reference/components-reference/supported-state-stores/setup-azure-cosmosdb/){target=_blank}.

![dapr-components](../../assets/images/04-aca-dapr-stateapi/dapr-components.jpg)

To add the component file state store, add a new folder named **components** under the directory **TasksTracker.ContainerApps** and add a new yaml file as show below. The values for `url` and `masterKey` can be found in the console output from the most recent commands.

!!! info
    You need to replace the **masterKey** value with your Cosmos Account key. Remember this is only needed for local development debugging, we will not be using the masterKey when we deploy to ACA.

    Replace the **url** value with the URI value of your cosmos database account. You can get that from the [Azure portal](https://portal.azure.com){target=_blank} by navigating to the cosmos database account overview page and get the uri value from there. 
    Basically the uri should have the following structure. [https://COSMOS_DB_ACCOUNT.documents.azure.com:443/](https://COSMOS_DB_ACCOUNT.documents.azure.com:443/).

=== "dapr-statestore-cosmos.yaml"

    ```yaml
    --8<-- "docs/aca/04-aca-dapr-stateapi/dapr-statestore-cosmos.yaml"
    ```

??? info "Curious to learn more about the contents of the yaml file?"
    - We've used the name `statestore` which should match the name of statestore we've used in the `TaskStoreManager.cs` file. As well, we have set the metadata key/value to allow us to connect to Azure Cosmos DB.
    - We've updated the other metadata keys such as `database`, `collection`, etc... to match the values of your Cosmos DB instance. For full metadata specs, you can check this [page](https://docs.dapr.io/reference/components-reference/supported-state-stores/setup-azure-cosmosdb/){target=_blank}.
    - By default, all dapr-enabled container apps within the same environment will load the full set of deployed components. By adding `scopes` to a component, you tell the Dapr sidecars for each respective container app which components to load at runtime.
    Using scopes is recommended for production workloads. In our case, we have set the scopes to `tasksmanager-backend-api` which represents the dapr-app-id which is associated to the container app that needs access to Azure Cosmos DB State Store as this will be the application that needs access to Azure Cosmos DB State Store. More about scopes can be found on this [link](https://learn.microsoft.com/en-us/azure/container-apps/dapr-overview?tabs=bicep1%2Cyaml#component-scopes){target=_blank}.

!!! note
    Dapr component scopes correspond to the Dapr application ID of a container app, not the container app name.

Now you should be ready to launch both applications and start doing CRUD operations from the Frontend Web App including querying the store. All your data will be stored in Cosmos DB Database you just provisioned.

If you have been running the different microservices using the [debug and launch Dapr applications in VSCode](../13-appendix/01-run-debug-dapr-app-vscode.md) then remember to uncomment the following line inside tasks.json file. 
This will instruct dapr to load the local projects components located at **./components** instead of the global components' folder.

```json hl_lines="2"
{
    "componentsPath": "./components"
}
```

If you have been using the dapr cli commands instead of the aforementioned debugging then you will need to execute the backend api with the resources-path property as follows.

=== ".NET 6 or below"

    --8<-- "snippets/dapr-run-backend-api.md:dapr-components-dotnet6"

=== ".NET 7 or above"

    --8<-- "snippets/dapr-run-backend-api.md:dapr-components"

!!! note "Deprecation Warning"
    components-path is being deprecated in favor of --resources-path. At the time of producing this workshop the --resources-path was not supported yet by the VS code extension. Hence, you will notice the use of the property "componentsPath": "./components" in the tasks.json file. Check the extension documentation in case that has changed.

After creating a new record you can navigate to the Data explorer on the [Azure portal](https://portal.azure.com){target=_blank} for the azure cosmos database account. It should look like the image below:

![cosmos-db-dapr-state-store](../../assets/images/04-aca-dapr-stateapi/cosmos-db-dapr-state-store.jpg)

##### Key Prefix Strategies

When you look at the key stored per entry and for example `tasksmanager-backend-api||aa3eb856-8309-4e68-93af-119be0d400e8`, you will notice that the key is prefixed with the Dapr application App Id responsible
to store this entry which in our case is `tasksmanager-backend-api`. There might be some scenarios which you need to have another service to access the same data store (not recommended as each service should be responsible about its own data store), in which case you can change the default behavior.

This can be done by adding the meta tag below to the component file. For example, if we need to set the value of the prefix to a constant value such as `TaskId` we can do the following:

```yaml
spec:
    metadata:
    - name: keyPrefix
    - value: TaskId
```

If we need to totally omit the key prefix, so it is accessed across multiple Dapr applications, we can set the value to `none`.

#### 3.3 Configure Managed Identities in Container App

As we highlighted earlier, we'll not use a connection strings to establish the relation between our Container App and Azure Cosmos DB when we deploy to ACA. Cosmos DB Master Key/Connection string was only used when debugging locally. Now we will rely on Managed Identities to allow our container app to access Cosmos DB. With [Manged Identities](https://learn.microsoft.com/en-us/azure/container-apps/dapr-overview?tabs=bicep1%2Cyaml){target=_blank} you do't worry about storing the keys securely and rotate them inside your application. This approach is safer and easier to manage.

We will be using a `system-assigned` identity with a role assignment to grant our Backend API container app permissions to access data stored in Cosmos DB. We need to assign it a custom role for the Cosmos DB data plane. In this example ae are going to use a [built-in role](https://learn.microsoft.com/en-us/azure/cosmos-db/how-to-setup-rbac#built-in-role-definitions){target=_blank}, named `Cosmos DB Built-in Data Contributor`, which grants our application full read-write access to the data. You can optionally create custom, fine-tuned roles following the instructions in the [official docs](https://learn.microsoft.com/en-us/azure/cosmos-db/how-to-setup-rbac){target=_blank}.

#### 3.3.1 Create system-assigned identity for our Backend API Container App

Run the command below to create `system-assigned` identity for our Backend API container app:

```shell
az containerapp identity assign `
--name $BACKEND_API_NAME `
--resource-group $RESOURCE_GROUP `
--system-assigned

$COSMOS_DB_PRIMARY_MASTER_KEY=(az cosmosdb keys list `
--name $COSMOS_DB_ACCOUNT `
--resource-group $RESOURCE_GROUP `
--query primaryMasterKey `
--output tsv)

$BACKEND_API_PRINCIPAL_ID=(az containerapp identity show `
--name $BACKEND_API_NAME `
--resource-group $RESOURCE_GROUP `
--query principalId `
--output tsv)
```

This command will create an Enterprise Application (basically a Service Principal) within Azure AD, which is linked to our container app. The output of this command will be similar to the one shown below.
Keep a note of the property `principalId` as we are going to use it in the next step.

```json
{
    "principalId": "[your principal id will be displayed here]",
    "tenantId": "[your tenant id will be displayed here]",
    "type": "SystemAssigned"
}
```

#### 3.3.2 Assign the Container App System-Identity To the Built-in Cosmos DB Role

Next, we need to associate the container app system-identity with the target Cosmos DB resource.
You can read more about Azure built-in roles for Cosmos DB or how to create custom fine-tuned roles [here](https://learn.microsoft.com/en-us/azure/cosmos-db/how-to-setup-rbac#built-in-role-definitions){target=_blank}.
Run the command below to associate the container app `system-assigned` identity with `Cosmos DB Built-in Data Contributor` role.

!!! note
    Make sure you save this principal id somewhere as you will need it in later modules. You can't rely on having it saved in powershell under `$BACKEND_API_PRINCIPAL_ID` as this variable could replace later on.
    Remember to replace the placeholders with your own values:

```shell
$ROLE_ID = "00000000-0000-0000-0000-000000000002" #"Cosmos DB Built-in Data Contributor" 

az cosmosdb sql role assignment create `
--resource-group $RESOURCE_GROUP `
--account-name  $COSMOS_DB_ACCOUNT `
--scope "/" `
--principal-id $BACKEND_API_PRINCIPAL_ID `
--role-definition-id $ROLE_ID
```

### 3.4 Deploy the Backend API and Frontend Web App Projects to ACA

We are almost ready to deploy all local changes from this module and the previous module to ACA. But before we do that, we need one last addition.

We have to create a [dapr component schema file](https://learn.microsoft.com/en-us/azure/container-apps/dapr-overview?tabs=bicep1%2Cyaml#component-schema){target=_blank} for Azure Cosmos DB which meets the specs defined by
Azure Container Apps. The reason for this variance is that ACA Dapr schema is slightly simplified to support Dapr components and removes unnecessary fields, including `apiVersion`, `kind`, and redundant metadata and spec properties.

#### 3.4.1 Create an ACA-Dapr Component File For State Store Management

Here it is recommended to separate the component files that will be used when deploying to Azure Container Apps from the ones which we will use when running our application locally (Dapr self-hosted).

Create a new folder named **aca-components** under the directory **TasksTracker.ContainerApps**, then add a new file as shown below:

!!! info
    Remember to replace the url value with the URI value of your cosmos database account. You can get that from the [Azure portal](https://portal.azure.com){target=_blank} by navigating to the cosmos database account overview page and get the uri value from there.
    Basically the uri should have the following structure `https://COSMOS_DB_ACCOUNT.documents.azure.com:443/`

=== "containerapps-statestore-cosmos.yaml"

    ```yaml
    --8<-- "docs/aca/04-aca-dapr-stateapi/containerapps-statestore-cosmos.yaml"
    ```
???+ tip "Curious to learn more about the contents of the yaml file?"
    - We didn't specify the Cosmos DB component name `statestore` when we created this component file. We are going to specify it once we add this dapr component to Azure Container Apps Environment via CLI.
    - We are not referencing any Cosmos DB Keys/Connection strings as the authentication between Dapr and Cosmos DB will be configured using Managed Identities.
    - We are setting the `scopes` array value to `tasksmanager-backend-api` to ensure Cosmos DB component is loaded at runtime by only the appropriate container apps. In our case it will be needed only for the container apps with Dapr application IDs `tasksmanager-backend-api`. In future modules we are going to include another container app which needs to access Cosmos DB.

#### 3.4.2 Build Frontend Web App and Backend API App Images in Azure Container Registry

As we have done previously, we need to build and deploy both app images to ACR, so they are ready to be deployed to Azure Container Apps.
To do so, continue using the same PowerShell console and paste the code below. Ensure you are in the root directory:

```shell
az acr build `
--registry $AZURE_CONTAINER_REGISTRY_NAME `
--image "tasksmanager/$BACKEND_API_NAME" `
--file 'TasksTracker.TasksManager.Backend.Api/Dockerfile' .

az acr build `
--registry $AZURE_CONTAINER_REGISTRY_NAME `
--image "tasksmanager/$FRONTEND_WEBAPP_NAME" `
--file 'TasksTracker.WebPortal.Frontend.Ui/Dockerfile' .
```

#### 3.4.3 Add Cosmos DB Dapr State Store to Azure Container Apps Environment

We need to run the command below to add the yaml file `.\aca-components\containerapps-statestore-cosmos.yaml` to Azure Container Apps Environment.

```shell
az containerapp env dapr-component set `
--name $ENVIRONMENT `
--resource-group $RESOURCE_GROUP `
--dapr-component-name statestore `
--yaml '.\aca-components\containerapps-statestore-cosmos.yaml'
```

#### 3.4.4 Enable Dapr for the Frontend Web App and Backend API Container Apps

Until this moment Dapr was not enabled on the Container Apps we have provisioned. Enable Dapr for both Container Apps by running the two commands below in the PowerShell console.

```shell
az containerapp dapr enable `
--name $BACKEND_API_NAME `
--resource-group $RESOURCE_GROUP `
--dapr-app-id  $BACKEND_API_NAME `
--dapr-app-port $TARGET_PORT

az containerapp dapr enable `
--name $FRONTEND_WEBAPP_NAME `
--resource-group $RESOURCE_GROUP `
--dapr-app-id  $FRONTEND_WEBAPP_NAME `
--dapr-app-port $TARGET_PORT
```

??? tip "Curious to learn more about the command above?"
    - We've enabled Dapr on both container apps and specified a unique Dapr identifier for the Back End API and Front End Web App container apps.
    This `dapr-app-id` will be used for service discovery, state encapsulation and the pub/sub consumer ID.
    - We've set the `dapr-app-port` which is the port our applications are listening on which will be used by Dapr for communicating to our applications.

    For a complete list of the supported Dapr sidecar configurations in Container Apps, you can refer to [this link](https://learn.microsoft.com/en-us/azure/container-apps/dapr-overview?tabs=bicep1%2Cyaml#dapr-enablement){target=_blank}.

#### 3.4.5 Deploy New Revisions of the Frontend Web App and Backend API to Container Apps

The last thing we need to do here is to update both container apps and deploy the new images from ACR. To do so we need to run the commands found below.

```shell
# Update Frontend web app container app and create a new revision 
az containerapp update `
--name $FRONTEND_WEBAPP_NAME  `
--resource-group $RESOURCE_GROUP `
--revision-suffix v$TODAY

# Update Backend API App container app and create a new revision 
az containerapp update `
--name $BACKEND_API_NAME  `
--resource-group $RESOURCE_GROUP `
--revision-suffix v$TODAY-1

echo "Azure Frontend UI URL:" 
echo $FRONTEND_UI_BASE_URL
```

!!! tip
    Notice here that we used a `revision-suffix` property, so it will append to the revision name which offers you better visibility on which revision you are looking at.

!!! success
    With this final step, we should be able to access the Frontend Web App, call the backend API app using Dapr sidecar, and store tasks to Azure Cosmos DB.

--8<-- "snippets/update-variables.md"
--8<-- "snippets/persist-state.md:module4"

## Review

In this module, we accomplished three objectives:

1. Learned about Dapr State Management with Redis Cache.
2. Used the Dapr Client SDK.
3. Provisioned Azure Cosmos DB resources & Update app and API in Azure.

In the next module, we will introduce the Dapr Pub/Sub Building block which we will publish messages to Azure Service Bus when a task is saved. We will also introduce a new background service will process those incoming messages and send an email to the task assignee.
