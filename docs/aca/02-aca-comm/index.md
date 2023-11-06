---
canonical_url: https://bitoftech.net/2022/08/25/communication-microservices-azure-container-apps/
---

# Module 2 - Communication Between Microservices in ACA

!!! info "Module Duration"
    60 minutes

## Objective
In this module, we will accomplish three objectives:

1. Create a web app named `{{ apps.frontend }}`, which is the UI to interact with `{{ apps.backend }}`.
1. Deploy the `{{ apps.frontend }}` container app to Azure.
1. Shield `{{ apps.backend }}` from external access.

## Module Sections

--8<-- "snippets/restore-variables.md"

### 1. Create the Frontend Web App project

- Initialize the web project. This will create and ASP.NET Razor Pages web app project.

    ```shell
    dotnet new webapp -o TasksTracker.WebPortal.Frontend.Ui
    ```

- We need to containerize this application, so we can push it to Azure Container Registry as a docker image then deploy it to ACA. Open the VS Code Command Palette (++ctrl+shift+p++) and select `Docker: Add Docker Files to Workspace...`

    - Use `.NET: ASP.NET Core` when prompted for application platform.
    - Choose `TasksTracker.WebPortal.Frontend.Ui\TasksTracker.WebPortal.Fortend.Ui.csproj` when prompted to choose a project file.
    - Choose `Linux` when prompted to choose the operating system.
    - Use the **same application port** as you used for the backend API. This allows us to reuse `$TARGET_PORT` later on.
    - You will be asked if you want to add Docker Compose files. Select `No`.
    - `Dockerfile` and `.dockerignore` files are added to the workspace.

- Open `Dockerfile` and replace `FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:7.0 AS build` with `FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build`.

- From inside the **Pages** folder, add a new folder named **Tasks**. Within that folder, add a new folder named **Models**, then create file as shown below.

    === "TasksModel.cs"
    ```csharp
    --8<-- "docs/aca/02-aca-comm/TasksModel.cs"
    ```

- Now, in the **Tasks** folder, we will add 3 Razor pages for CRUD operations which will be responsible for listing tasks, creating a new task, and updating existing tasks.
By looking at the cshtml content notice that the page is expecting a query string named `createdBy` which will be used to group tasks for application users.

    !!! note
        We are following this approach here to keep the workshop simple, but for production applications, authentication should be applied and the user email should be retrieved from the claims identity of the authenticated users.

    === "Index.cshtml"
        ```html
        --8<-- "docs/aca/02-aca-comm/Tasks.Index.cshtml"
        ```

    === "Index.cshtml.cs"
        ```csharp
        --8<-- "docs/aca/02-aca-comm/Tasks.Index.cshtml.cs"
        ```

        !!! tip "What does this code do?"
            In the code above we've injected named HttpClientFactory which is responsible to call the Backend API service as HTTP request. The index page supports deleting and marking tasks as completed along with listing tasks for certain users based on the `createdBy` property stored in a cookie named `TasksCreatedByCookie`.
            More about populating this property later in the workshop.

    === "Create.cshtml"
        ```html
        --8<-- "docs/aca/02-aca-comm/Create.cshtml"
        ```

    === "Create.cshtml.cs"
        ```csharp
        --8<-- "docs/aca/02-aca-comm/Create.cshtml.cs"
        ```
        
        !!! tip "What does this code do?"
            The code is self-explanatory here. We just injected the type HttpClientFactory in order to issue a POST request and create a new task.

    === "Edit.cshtml"
        ```html
        --8<-- "docs/aca/02-aca-comm/Edit.cshtml"
        ```

    === "Edit.cshtml.cs"
        ```csharp
        --8<-- "docs/aca/02-aca-comm/Edit.cshtml.cs"
        ```
        
        !!! tip "What does this code do?"
            The code added is similar to the create operation. The Edit page accepts the TaskId as a Guid, loads the task, and then updates the task by sending an HTTP PUT operation.

- Now we will inject an HTTP client factory and define environment variables. To do so we will register the HttpClientFactory named `BackEndApiExternal` to make it available for injection in controllers. Open the `Program.cs` file and update it with highlighted code below. Your file may be flattened rather than indented and not contain some of the below elements. Don't worry. Just place the highlighted lines in the right spot:

    === "Program.cs"
    ```csharp hl_lines="6-15"
    --8<-- "docs/aca/02-aca-comm/program.cs"
    ```

- Next, we will add a new environment variable named `BackendApiConfig:BaseUrlExternalHttp` into `appsettings.json` file. This variable will contain the Base URL for the backend API deployed in the previous module to ACA. Later on in the workshop, we will see how we can set the environment variable once we deploy it to ACA. Use the output from this script as the `BaseUrlExternalHttp` value.

    ```PowerShell
    $BACKEND_API_EXTERNAL_BASE_URL
    ```

    === "appsettings.json"
    ```json
        --8<-- "docs/aca/02-aca-comm/appsettings.json"
    ```

- Lastly, we will update the web app landing page `Index.html` and `Index.cshtml.cs` inside **Pages** folder to capture the email of the tasks owner user and assign this email to a cookie named `TasksCreatedByCookie`.

    === "Index.cshtml"
        ```html
        --8<-- "docs/aca/02-aca-comm/Index.cshtml"
        ```

    === "Index.cshtml.cs"
        ```csharp
        --8<-- "docs/aca/02-aca-comm/Index.cshtml.cs"
        ```

- From VS Code Terminal tab, open developer command prompt or PowerShell terminal and navigate to the frontend directory which hosts the `.csproj` project folder and build the project.

    ```shell
    cd ~\TasksTracker.ContainerApps\TasksTracker.WebPortal.Frontend.Ui
    dotnet build
    ```

!!! note
    Make sure that the build is successful and that there are no build errors. Usually you should see a **Build succeeded** message in the terminal upon a successful build.

### 2. Deploy Razor Pages Web App Frontend Project to ACA

- We need to add the below PS variables:

    ```powershell
    $FRONTEND_WEBAPP_NAME="tasksmanager-frontend-webapp"
    ```

- Now we will build and push the Web App project docker image to ACR. Use the below command to initiate the image build and push process using ACR. The `.` at the end of the command represents the docker build context. In our case, we need to be on the parent directory which hosts the .csproject.

    ```powershell
    cd ~\TasksTracker.ContainerApps
    ```

    ```powershell
    az acr build `
    --registry $AZURE_CONTAINER_REGISTRY_NAME `
    --image "tasksmanager/$FRONTEND_WEBAPP_NAME" `
    --file 'TasksTracker.WebPortal.Frontend.Ui/Dockerfile' .
    ```

- Once this step is completed you can verify the results by going to the [Azure portal](https://portal.azure.com){target=_blank} and checking that a new repository named `tasksmanager/tasksmanager-frontend-webapp` has been created and that a new docker image with a `latest` tag has been created.

- Next, we will create and deploy the Web App to ACA using the following command:

    ```powershell
    $fqdn=(az containerapp create `
    --name "$FRONTEND_WEBAPP_NAME"  `
    --resource-group $RESOURCE_GROUP `
    --environment $ENVIRONMENT `
    --image "$AZURE_CONTAINER_REGISTRY_NAME.azurecr.io/tasksmanager/$FRONTEND_WEBAPP_NAME" `
    --registry-server "$AZURE_CONTAINER_REGISTRY_NAME.azurecr.io" `
    --env-vars "BackendApiConfig__BaseUrlExternalHttp=$BACKEND_API_EXTERNAL_BASE_URL/" `
    --target-port $TARGET_PORT `
    --ingress 'external' `
    --min-replicas 1 `
    --max-replicas 1 `
    --cpu 0.25 --memory 0.5Gi `
    --query properties.configuration.ingress.fqdn `
    --output tsv)

    $FRONTEND_UI_BASE_URL="https://$fqdn"
    
    echo "See the frontend web app at this URL:"
    echo $FRONTEND_UI_BASE_URL
    ```

!!! tip
    Notice how we used the property `env-vars` to set the value of the environment variable named `BackendApiConfig_BaseUrlExternalHttp` which we added in the AppSettings.json file. You can set multiple environment variables at the same time by using a space between each variable.
    The `ingress` property is set to `external` as the Web frontend App will be exposed to the public internet for users.

After your run the command, copy the FQDN (Application URL) of the Azure container app named `tasksmanager-frontend-webapp` and open it in your browser, and you should be able to browse the frontend web app and manage your tasks.

### 3. Update Backend Web API Container App Ingress property

So far the Frontend App is sending HTTP requests to publicly exposed Web API which means that any REST client can invoke this API. We need to change the Web API ingress settings and make it only accessible for applications deployed within our Azure Container Environment only. Any application outside the Azure Container Environment will not be able to access the Web API.

- To change the settings of the Backend API, execute the following command:

    ```powershell
    $fqdn=(az containerapp ingress enable `
    --name $BACKEND_API_NAME  `
    --resource-group $RESOURCE_GROUP `
    --target-port $TARGET_PORT `
    --type "internal" `
    --query fqdn `
    --output tsv)

    $BACKEND_API_INTERNAL_BASE_URL="https://$fqdn"

    echo "The internal backend API URL:"
    echo $BACKEND_API_INTERNAL_BASE_URL
    ```

??? tip "Want to know more about the command?"
    When you do this change, the FQDN (Application URL) will change, and it will be similar to the one shown below. Notice how there is an `Internal` part of the URL. `https://tasksmanager-backend-api.internal.[Environment unique identifier].eastus.azurecontainerapps.io/api/tasks/`

    If you try to invoke the URL from the browser directly it will return 404 as this Internal Url can only be accessed from container apps within the container environment.
    
    The FQDN consists of multiple parts. For example, all our Container Apps will be under a specific Environment unique identifier (e.g. `agreeablestone-8c14c04c`) and the Container App will vary based on the name provided, check the image below for a better explanation.
    ![Container Apps FQDN](../../assets/images/02-aca-comm/container-apps-fqdn.jpg)

- Now we will need to update the Frontend Web App environment variable to point to the **internal** backend Web API FQDN. The last thing we need to do here is to update the Frontend WebApp environment variable named `BackendApiConfig_BaseUrlExternalHttp` with the new value of the _internal_ Backend Web API base URL, to do so we need to update the Web App container app and it will create a new revision implicitly (more about revisions in the upcoming modules). The following command will update the container app with the changes:

    ```powershell
    az containerapp update `
    --name "$FRONTEND_WEBAPP_NAME" `
    --resource-group $RESOURCE_GROUP `
    --set-env-vars "BackendApiConfig__BaseUrlExternalHttp=$BACKEND_API_INTERNAL_BASE_URL/"
    ```

!!! success
    Browse the web app again, and you should be able to see the same results and access the backend API endpoints from the Web App. You can obtain the frontend URL from executing this variable.

    ```powershell
    $FRONTEND_UI_BASE_URL
    ```

--8<-- "snippets/persist-state.md:module2"
--8<-- "snippets/update-variables.md"

## Review
In this module, we have accomplished three objectives:

1. Created a web app named `{{ apps.frontend }}`, which is the UI to interact with `{{ apps.backend }}`.
1. Deployed the `{{ apps.frontend }}` container app to Azure.
1. Shielded `{{ apps.backend }}` from external access.

In the next module, we will start integrating Dapr and use the service to service Building block for services discovery and invocation.
