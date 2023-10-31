---
canonical_url: 'https://bitoftech.net/2022/08/25/deploy-microservice-application-azure-container-apps/'
---

# Module 1 - Deploy Backend API to ACA

!!! info "Module Duration"
    60 minutes

In this module, we will start by creating the first microservice named `ACA Web API – Backend` as illustrated in the [architecture diagram](../../assets/images/00-workshop-intro/ACA-Architecture-workshop.jpg){target=_blank}. Followed by that we will provision the  Azure resources needed to deploy the service to Azure Container Apps using the Azure CLI.

### 1. Set up Git Repository

This workshop spans several days. As such, you may close your tools, CLI sessions, reboot, or simply want to persist working implementations in a repository as each module builds upon the one before it. A local Git repository can help.

- Open a command-line terminal and create a folder for your project, then switch to that folder.

    === "Windows"
        ```shell
        md TasksTracker.ContainerApps
        cd TasksTracker.ContainerApps
        ```
    === "Linux"
        ```shell
        mkdir ~\TasksTracker.ContainerApps
        cd ~\TasksTracker.ContainerApps
        ```

- Initialize the git repository.

    ```shell
    git init
    ```

- Create a `.gitignore` file in the `TasksTracker.ContainerApps` directory. This ensures we keep our git repo clean of build assets and other artifacts.

    === ".gitignore"
        ```shell
        # Exclude build artifacts
        **/obj/
        **/bin/
        **/dist/
        ```

- Commit the `.gitignore` file.

    ```shell
    git commit -a -m "Add .gitignore"
    ```

### 2. Create the backend API project (Web API)

- Use the `code` command to launch Visual Studio Code from that directory as shown:

    ```shell
    
    code .
    ```

- From VS Code Terminal tab, open developer command prompt or PowerShell terminal in the project folder `TasksTracker.ContainerApps` and execute `dotnet --info`. Take note of the intalled .NET SDK versions.

- Inside the `TasksTracker.ContainerApps` project folder create a new file and set the .NET SDK version from the above command:

    === "global.json"
    ```json
    {
        "sdk": {
            "version": "7.0.401",
            "rollForward": "latestFeature"
        }
    }
    ```

- Initialize the project in the terminal. This will create and ASP.NET Web API project scaffolded with a single controller.

    ```shell
    dotnet new webapi -o TasksTracker.TasksManager.Backend.Api
    ```

- Delete the boilerplate `WeatherForecast.cs` and `Controllers\WeatherForecastController.cs` files from the new `TasksTracker.TasksManager.Backend.Api` project folder.

- Next, we need to containerize this application, so we can push it to Azure Container Registry as a docker image, then deploy it to Azure Container Apps. Start by opening the VS Code Command Palette (++ctrl+shift+p++) and select `Docker: Add Docker Files to Workspace...`

    - Use `.NET: ASP.NET Core` when prompted for application platform.
    - Choose `Linux` when prompted to choose the operating system.
    - Take note of the provided **application port** as we will pass it later on as the `--target-port` for the `az containerapp create` command.
    - You will be asked if you want to add Docker Compose files. Select `No`.
    - `Dockerfile` and `.dockerignore` files are added to the workspace.

- Open `Dockerfile` and replace `FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:7.0 AS build` with `FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build`.

- In the new project folder, add a new folder named **Models** and create a new file with name below. These are the DTOs that will be used across the projects.

    === "TaskModel.cs"
    ```csharp
    --8<-- "docs/aca/01-deploy-api-to-aca/TaskModel.cs"
    ```

- In the new project folder, create a new folder named **Services** (make sure it is created at the same level as the models folder and not inside the models folder itself) and add **new files** as shown below. Add the Fake Tasks Manager service. This will be the interface of Tasks Manager service. We will work initially with data in memory to keep things simple with very limited dependency on any other components or data store and focus on the deployment of the backend API to ACA. In the upcoming modules we will switch this implementation with a concrete data store where we are going to store data in Redis and Azure Cosmos DB using Dapr State Store building block.

    === "ITasksManager.cs"
        ```csharp
        --8<-- "docs/aca/01-deploy-api-to-aca/ITasksManager.cs"
        ```
    === "FakeTasksManager.cs"
        ```csharp
        --8<-- "docs/aca/01-deploy-api-to-aca/FakeTasksManager.cs"
        ```

The code above generates ten tasks and stores them in a list in memory. It also has some operations to add/remove/update those tasks.

- Now we need to register `FakeTasksManager` on project startup. Open file `#!csharp Program.cs` and register the newly created service by adding the highlighted lines from below snippet. Don't forget to include the required using statements for the task interface and class.

=== "Program.cs"

```csharp hl_lines="1 5"
using TasksTracker.TasksManager.Backend.Api.Services;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
builder.Services.AddSingleton<ITasksManager, FakeTasksManager>();

// Code removed for brevity
app.Run();
```

- Inside the **Controllers** folder create a new controller with the below filename. We need to create API endpoints to manage tasks.

=== "TasksController.cs"

```csharp
--8<-- "docs/aca/01-deploy-api-to-aca/TasksController.cs"
```

- From VS Code Terminal tab, open developer command prompt or PowerShell terminal and navigate to the parent directory which hosts the `.csproj` project folder and build the project.

    ```shell
    cd ~\TasksTracker.ContainerApps\TasksTracker.TasksManager.Backend.Api
    dotnet build
    ```

!!! note
    Throughout the documentation, we will use the the tilde character [~] to represent the base / parent folder where you chose to install the workshop assets.

Make sure that the build is successful and that there are no build errors. Usually you should see a "Build succeeded" message in the terminal upon a successful build.

### 3. Deploy Web API Backend Project to ACA

We will be using Azure CLI to deploy the Web API Backend to ACA as shown in the following steps:

- We will start with Installing/Upgrading the Azure Container Apps Extension.

    ```shell
    # Upgrade Azure CLI
    az upgrade

    # Install/Upgrade Azure Container Apps Extension
    az extension add --name containerapp --upgrade

    # Login to Azure
    az login 
    ```

- You may be able to use the queried Azure subscription ID or you may need to set it manually depending on your setup.

    ```shell
    # Get/Set the Azure Subscription ID. 
    # $AZURE_SUBSCRIPTION_ID = "<Your Azure Subscription ID>" # Your Azure Subscription id which you can find on the azure portal
    $AZURE_SUBSCRIPTION_ID = az account show --query id --output tsv
    # Only required if you have multiple subscriptions
    #az account set --subscription $AZURE_SUBSCRIPTION_ID
    ```

- Define the variables below in the PowerShell console to use them across the different modules in the workshop. You should change the values of those variables to be able to create the resources successfully. Some of those variables should be unique across all Azure subscriptions such as Azure Container Registry name. Remember to replace the place holders with your own values:

    ```shell
    # Create a random, 6-digit, Azure safe string
    $RANDOM_STRING=-join ((97..122) + (48..57) | Get-Random -Count 6 | ForEach-Object { [char]$_})
    $RESOURCE_GROUP="rg-tasks-tracker-$RANDOM_STRING"
    $LOCATION="eastus"
    $ENVIRONMENT="cae-tasks-tracker"
    $WORKSPACE_NAME="log-tasks-tracker-$RANDOM_STRING"
    $APPINSIGHTS_NAME="appi-tasks-tracker-$RANDOM_STRING"
    $BACKEND_API_NAME="tasksmanager-backend-api"
    $AZURE_CONTAINER_REGISTRY_NAME="crtaskstracker$RANDOM_STRING"
    ```

- Also assign the target port from when you created the Dockerfile:

    ```shell
    $TARGET_PORT=[exposed Docker target port from Dockerfile]
    ```

??? tip "List of Variables"
    As you progress through the different modules it may be hard to keep track of the variables that you have set so far. You can retrieve a list of all the variables that you have set throughout this workshop by executing the [variables script](../../aca/13-appendix/03-variables.md) in the same terminal where you are executing the scripts.

??? tip "Cloud Adoption Framework Abbreviations"
    Unless you have your own naming convention, we suggest to use [Cloud Adoption Framework (CAF) abbreviations](https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-abbreviations){target=_blank} for resource prefixes.

- Create a `resource group` to organize the services related to the application, run the below command:

    ```shell
    az group create `
    --name $RESOURCE_GROUP `
    --location "$LOCATION"
    ```

- Create an Azure Container Registry (ACR) instance in the resource group to store images of all Microservices we are going to build during this workshop. Make sure that you set the `admin-enabled` flag to true in order to seamlessly authenticate the Azure container app when trying to create the container app using the image stored in ACR.

    ```shell
    az acr create `
    --resource-group $RESOURCE_GROUP `
    --name $AZURE_CONTAINER_REGISTRY_NAME `
    --sku Basic `
    --admin-enabled true
    ```

!!! note
    Notice that we create the registry with admin rights `--admin-enabled` flag set to `true` which is not suited for real production, but good for our workshop.

- Create an Azure Log Analytics Workspace which will provide a common place to store the system and application log data from all container apps running in the environment. Each environment should have its own Log Analytics Workspace. To create it, run the command below:

    ```shell
    # Create the log analytics workspace
    az monitor log-analytics workspace create `
    --resource-group $RESOURCE_GROUP `
    --workspace-name $WORKSPACE_NAME

    # Retrieve workspace ID
    $WORKSPACE_ID=az monitor log-analytics workspace show `
    --resource-group $RESOURCE_GROUP `
    --workspace-name $WORKSPACE_NAME `
    --query customerId `
    --output tsv

    # Retrieve workspace secret
    $WORKSPACE_SECRET=az monitor log-analytics workspace get-shared-keys `
    --resource-group $RESOURCE_GROUP `
    --workspace-name $WORKSPACE_NAME `
    --query primarySharedKey `
    --output tsv
    ```

- Create an [Application Insights](https://learn.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview?tabs=net){target=_blank} Instance which will be used mainly for [distributed tracing](https://learn.microsoft.com/en-us/azure/azure-monitor/app/distributed-tracing){target=_blank} between different container apps within the ACA environment to provide searching for and visualizing an end-to-end flow of a given execution or transaction. To create it, run the command below:

    ```shell
    # Install the application-insights extension for the CLI
    az extension add -n application-insights
    
    # Create application-insights instance
    az monitor app-insights component create `
    --resource-group $RESOURCE_GROUP `
    --location $LOCATION `
    --app $APPINSIGHTS_NAME `
    --workspace $WORKSPACE_NAME
    
    # Get Application Insights Instrumentation Key
    $APPINSIGHTS_INSTRUMENTATIONKEY=($(az monitor app-insights component show `
    --resource-group $RESOURCE_GROUP `
    --app $APPINSIGHTS_NAME ) | ConvertFrom-Json).instrumentationKey
    ```

- Now we will create an Azure Container Apps Environment. As a reminder of the different ACA component [check this link in the workshop introduction](../../aca/00-workshop-intro/1-aca-core-components.md). The ACA environment acts as a secure boundary around a group of container apps that we are going to provision during this workshop. To create it, run the below command:

    ```shell
    # Create the ACA environment
    az containerapp env create `
    --name $ENVIRONMENT `
    --resource-group $RESOURCE_GROUP `
    --location $LOCATION `
    --logs-workspace-id $WORKSPACE_ID `
    --logs-workspace-key $WORKSPACE_SECRET `
    --dapr-instrumentation-key $APPINSIGHTS_INSTRUMENTATIONKEY
    ```

??? tip "Want to learn what above command does?"
    - It creates an ACA environment and associates it with the Log Analytics Workspace created in the previous step.
    - We are setting the `--dapr-instrumentation-key` value to the instrumentation key of the Application Insights instance. This will come handy when we introduce Dapr in later modules and show how the distributed tracing between microservices/container apps are captured and visualized in Application Insights.  
    > **_NOTE:_**
    You can set the `--dapr-instrumentation-key` after you create the ACA environment but this is not possible via the AZ CLI right now. There is an [open issue](https://github.com/microsoft/azure-container-apps/issues/293){target=_blank} which is being tracked by the product group.

- Build the Web API project on ACR and push the docker image to ACR. Use the below command to initiate the image build and push process using ACR. The `.` at the end of the command represents the docker build context, in our case, we need to be on the parent directory which hosts the `.csproj`.

    ```shell
    cd ~\TasksTracker.ContainerApps

    az acr build `
    --registry $AZURE_CONTAINER_REGISTRY_NAME `
    --image "tasksmanager/$BACKEND_API_NAME" `
    --file 'TasksTracker.TasksManager.Backend.Api/Dockerfile' .
    ```

    Once this step is completed, you can verify the results by going to the Azure portal and checking that a new repository named `tasksmanager/tasksmanager-backend-api` has been created, and that there is a new Docker image with a `latest` tag.

- The last step here is to create and deploy the Web API to ACA following the below command:

    ```shell
    $fqdn=(az containerapp create `
    --name $BACKEND_API_NAME `
    --resource-group $RESOURCE_GROUP `
    --environment $ENVIRONMENT `
    --image "$AZURE_CONTAINER_REGISTRY_NAME.azurecr.io/tasksmanager/$BACKEND_API_NAME" `
    --registry-server "$AZURE_CONTAINER_REGISTRY_NAME.azurecr.io" `
    --target-port $TARGET_PORT `
    --ingress 'external' `
    --min-replicas 1 `
    --max-replicas 1 `
    --cpu 0.25 --memory 0.5Gi `
    --query properties.configuration.ingress.fqdn `
    --output tsv)

    echo "See a listing of tasks created by the author at this URL:"
    echo "https://$fqdn/api/tasks/?createdby=tjoudeh@bitoftech.net"
    ```

??? tip "Want to learn what above command does?"
    - Ingress param is set to `external` which means that this container app (Web API) project will be accessible from the public internet. When Ingress is set to `Internal` or `External` it will be assigned a fully qualified domain name (FQDN). Important notes about IP addresses and domain names can be found [here](https://learn.microsoft.com/en-us/azure/container-apps/ingress?tabs=bash#ip-addresses-and-domain-names){target=_blank}.
    - The target port param is set to 80, this is the port our Web API container listens to for incoming requests.
    - We didn't specify the ACR registry username and password, `az containerapp create` command was able to look up ACR username and password and add them as a secret under the created Azure container app for future container updates.
    - The minimum and the maximum number of replicas are set. More about this when we cover Autoscaling in later modules. For the time being, only a single instance of this container app will be provisioned as Auto scale is not configured.
    - We set the size of the Container App. The total amount of CPUs and memory requested for the container app must add up to certain combinations, for full details check the link [here](https://docs.microsoft.com/en-us/azure/container-apps/containers#configuration){target=_blank}.
    - The `query` property will filter the response coming from the command and just return the FQDN. Take note of this FQDN as you will need it for the next step.

    For full details on all available parameters for this command, please visit this [page](https://docs.microsoft.com/en-us/cli/azure/containerapp?view=azure-cli-latest#az-containerapp-create){target=_blank}.

- You can now verify the deployment of the first ACA by navigating to the link at the end of the above script or to the Azure Portal and selecting the resource group named `tasks-tracker-rg` that you created earlier. You should see the 5 resourses created below.
![Azure Resources](../../assets/images/01-deploy-api-to-aca/Resources.jpg)

!!! success
    To test the backend api service, either click on the URL output by the last command or copy the FQDN (Application URL) of the Azure container app named `tasksmanager-backend-api`, then issue a `GET` request similar to this one: `https://tasksmanager-backend-api.<your-aca-env-unique-id>.eastus.azurecontainerapps.io/api/tasks/?createdby=tjoudeh@bitoftech.net` and you should receive an array of the 10 tasks similar to the below image.

    Note that the specific query string matters as you may otherwise get an empty result back. 

    !!! tip
        You can find your azure container app application url on the azure portal overview tab.
    
        ![Web API Response](../../assets/images/01-deploy-api-to-aca/Response.jpg)

### 4. Persist State

- Persist Module 1 to Git.

    ```shell
    git add .
    git commit -m "Add Module 1"
    ```

- Execute the [Set-Variables.ps1 script](../../aca/13-appendix/03-variables.md){target=_blank} in the console to create a `variables.ps1` file with all current variables. The output of the script will inform you how many variables are written out.

- Persist a list of all current variables.

    ```shell
    git add .\Variables.ps1
    git commit -m "Add Variables.ps1"
    ```

In the next module, we will see how we will add a new Frontend Web App as a microservice and how it will communicate with the backend API.