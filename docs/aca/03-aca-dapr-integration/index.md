---
canonical_url: https://bitoftech.net/2022/08/29/dapr-integration-with-azure-container-apps/
---

# Module 3 - Dapr Integration with ACA

!!! info "Module Duration"
    60 minutes

## Objective

In this module, we will accomplish one objective:

1. Decouple `{{ apps.frontend }}` from `{{ apps.backend }}` by introducing Dapr.

## Module Sections

### 1. Introduce Dapr

#### 1.1 Restore Variables

--8<-- "snippets/restore-variables.md"

#### 1.2 Benefits of Integrating Dapr in Azure Container Apps

The Tasks Tracker microservice application is composed of multiple microservices. Function calls are spread across the network. To support the distributed nature of microservices,
we need to account for failures, retries, and timeouts. While Azure Container Apps features the building blocks for running microservices, using the Distributed Application Runtime (Dapr) provides an even richer microservices programming model.

Dapr includes features like service discovery, pub/sub, service-to-service invocation with mutual TLS, retries, state store management, and more.
Here is a good [link](https://learn.microsoft.com/en-us/dotnet/architecture/dapr-for-net-developers/service-invocation){target=_blank} which touches on some of the benefits of the Dapr service invocation building block which we will be building upon in this module.
Because the calls will flow through container sidecars, Dapr can inject some useful cross-cutting behaviors that are meaningfully abstracted from our application containers.

Although we won't tap into all these benefits in this workshop, it's worth noting that you will probably need to rely on these features in production:

- Automatically retry calls upon failure.
- Make calls between services secured  with mutual authentication (mTLS), including automatic certificate rollover.
- Control what operations clients can perform using access control policies.
- Capture traces and metrics for all calls between services to provide insights and diagnostics.

#### 1.3 Configure Dapr on a Local Development Machine

In order to run applications using Dapr, we need to install and initialize Dapr CLI locally. The official documentation is quite clear, and we can follow the steps needed to [install](https://docs.dapr.io/getting-started/install-dapr-cli/){target=_blank} Dapr and then [Initialize](https://docs.dapr.io/getting-started/install-dapr-selfhost/){target=_blank} it.

#### 1.4 Configure Backend API and Frontend Web App Locally Using Dapr

You are now ready to run the applications locally using the Dapr sidecar in a self-hosted mode. The [Dapr](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-dapr){target=_blank} VS Code extension will allow you to run, debug, and interact with Dapr-enabled applications.

- Let's start by capturing the UI and API localhost ports:

    ```shell
    $API_APP_PORT=<web api application https port number found under Properties->launchSettings.json. e.g. 7112>
    $UI_APP_PORT=<web frontend ui application https port found under Properties->launchSettings.json. e.g. 7000>
    ```

- Now that we know the `UI_APP_PORT`, we can also declare the local frontend UI URL:

    ```shell
    $FRONTEND_UI_BASE_URL_LOCAL="https://localhost:$UI_APP_PORT"
    ```

--8<-- "snippets/update-variables.md::1"

!!! note
    Remember to replace the placeholders with your own values based on image below. Remember to use https port number for the Web API application.

=== ".NET 6 or below"

    ```shell
    cd ~\TasksTracker.ContainerApps\TasksTracker.TasksManager.Backend.Api
    ```

    ```shell
    dapr run `
    --app-id tasksmanager-backend-api `
    --app-port $API_APP_PORT `
    --dapr-http-port 3500 `
    --app-ssl `
    -- dotnet run
    ```

=== ".NET 7 or above"

    ```shell
    cd ~\TasksTracker.ContainerApps\TasksTracker.TasksManager.Backend.Api
    ```

    ```shell
    dapr run `
    --app-id tasksmanager-backend-api `
    --app-port $API_APP_PORT `
    --dapr-http-port 3500 `
    --app-ssl `
    -- dotnet run --launch-profile https
    ```

 ![app-port](../../assets/images/03-aca-dapr-integration/self_hosted_dapr_app-port.png)

??? tip "Want to learn more about Dapr run command above?"
    When using Dapr run command you are running a dapr process as a sidecar next to the Web API application. The properties you have configured are as follows:

      - app-id: The unique identifier of the application. Used for service discovery, state encapsulation, and the pub/sub consumer identifier.
      - app-port: This parameter tells Dapr which port your application is listening on. You can get the app port from `Properties->launchSettings.json` file in the Web API Project as shown in the image above. Make sure you use the https port listed within the `Properties->launchSettings.json` as we are using the --app-ssl when running the dapr cli locally. Don't use the port inside the DockerFile. The DockerFile port will come in handy when you deploy to ACA at which point the application would be running inside a container.
      - dapr-http-port: The HTTP port for Dapr to listen on.
      - app-ssl: Sets the URI scheme of the app to https and attempts an SSL connection.

    For a full list of properties, you can check this [link.](https://docs.dapr.io/reference/cli/dapr-run/)

 If all is working as expected, you should receive an output similar to the one below where your app logs and dapr logs will appear on the same PowerShell terminal:

 ![dapr-logs](../../assets/images/03-aca-dapr-integration/dapr-logs.jpg)

 Now to test invoking the Web API using Dapr sidecar, you can issue an **GET** request to the following URL: [http://localhost:3500/v1.0/invoke/tasksmanager-backend-api/method/api/tasks?createdBy=tjoudeh@bitoftech.net](http://localhost:3500/v1.0/invoke/tasksmanager-backend-api/method/api/tasks?createdBy=tjoudeh@bitoftech.net){target=_blank}

??? info "Want to learn more about what is happening here?"
    What happened here is that Dapr exposes its HTTP and gRPC APIs as a sidecar process which can access our Backend Web API. We didn't do any changes to the application code to include any Dapr runtime code. We also ensured separation of the application logic for improved supportability.

    Looking back at the HTTP GET request, we can break it as follows:

      - `/v1.0/invoke` Endpoint: is the Dapr feature identifier for the "Service to Service invocation" building block. This building block enables applications to communicate with each other through well-known endpoints in the form of http or gRPC messages. Dapr provides an endpoint that acts as a combination of a reverse proxy with built-in service discovery while leveraging built-in distributed tracing and error handling.
      - `3500`: the HTTP port that Dapr is listening on.
      - `tasksmanager-backend-api`: is the dapr application unique identifier.
      - `method`: reserved word when using invoke endpoint.
      - `api/tasks?createdBy=tjoudeh@bitoftech.net`: the path of the action method that needs to be invoked in the webapi service.

    Another example is that we want to create a new task by invoking the POST operation, we need to issue the below POST request:

      ```http
      POST /v1.0/invoke/tasksmanager-backend-api/method/api/tasks/ HTTP/1.1
      Host: localhost:3500
      Content-Type: application/json
      {
              "taskName": "Task number: 51",
              "taskCreatedBy": "tjoudeh@bitoftech.net",
              "taskDueDate": "2022-08-31T09:33:35.9256429Z",
              "taskAssignedTo": "assignee51@mail.com"
      }
      ```

- Next, we will be using Dapr SDK in the frontend Web App to invoke Backend API services, The [Dapr .NET SDK](https://github.com/dapr/dotnet-sdk){target=_blank} provides .NET developers with an intuitive and language-specific way to interact with Dapr.
The SDK offers developers three ways of making remote service invocation calls:

    1. Invoke HTTP services using HttpClient
    2. Invoke HTTP services using DaprClient
    3. Invoke gRPC services using DaprClient

    We will be using the second approach in this workshop (HTTP services using DaprClient), but it is worth spending some time explaining the first approach (Invoke HTTP services using HttpClient). We will go over the first approach briefly and then discuss the second in details.

    Install DAPR SDK for .NET Core in the Frontend Web APP, so we can use the service discovery and service invocation offered by Dapr Sidecar. To do so, add below nuget package to the project.

    === ".NET 6"
        === "TasksTracker.WebPortal.Frontend.Ui.csproj"
    
            ```xml
            <ItemGroup>
                <PackageReference Include="Dapr.AspNetCore" Version="{{ dapr.version }}" />
            </ItemGroup>
            ```

    === ".NET 7"
        === "TasksTracker.WebPortal.Frontend.Ui.csproj"
    
            ```xml
            --8<-- "docs/aca/03-aca-dapr-integration/Frontend.Ui.csproj"
            ```

- Next, open the file `Programs.cs` of the Frontend Web App and register the DaprClient as the highlighted below.

    === ".NET 6"
        === "Program.cs"

            ```csharp hl_lines="11"
            namespace TasksTracker.WebPortal.Frontend.Ui
            {
                public class Program
                {
                    public static void Main(string[] args)
                    {
                        var builder = WebApplication.CreateBuilder(args);
                        // Add services to the container.
                        builder.Services.AddRazorPages();
                        // Code removed for brevity
                        builder.Services.AddDaprClient();
                        var app = builder.Build();
                        // Code removed for brevity 
                    }
                }
            }
            ```

    === ".NET 7"
        === "Program.cs"

            ```csharp hl_lines="6"
            --8<-- "docs/aca/03-aca-dapr-integration/Program.cs"
            ```

- Now, we will inject the DaprClient into the `.cshtml` pages to use the method `InvokeMethodAsync` (second approach). Update files under folder **Pages\Tasks** and use the code below for different files.

    === "Index.cshtml.cs"

        ```csharp    
        --8<-- "docs/aca/03-aca-dapr-integration/Tasks.Index.cshtml.cs"
        ```

    === "Create.cshtml.cs"

        ```csharp
        --8<-- "docs/aca/03-aca-dapr-integration/Create.cshtml.cs"
        ```

    === "Edit.cshtml.cs"

        ```csharp
        --8<-- "docs/aca/03-aca-dapr-integration/Edit.cshtml.cs"
        ``` 

???+ tip
    Notice how we are not using the `HttpClientFactory` anymore and how we were able from the Frontend Dapr Sidecar to invoke backend API Sidecar using the method `InvokeMethodAsync` which accepts the Dapr **remote App ID** for the Backend API `tasksmanager-backend-api` and it will be able to discover the URL and invoke the method based on the specified input params.

    In addition to this, notice how in POST and PUT operations, the third argument is a `TaskAdd` or `TaskUpdate` Model, those objects will be serialized internally (using System.Text.JsonSerializer) and sent as the request payload. The .NET SDK takes care of the call to the Sidecar. It also deserializes the response in case of the GET operations to a `List<TaskModel>` object.

    Looking at the first option of invoking the remote service "Invoke HTTP services using HttpClient", you can see that we can create an HttpClient by invoking `DaprClient.CreateInvokeHttpClient` and specify the remote service app id, custom port if needed and then use the HTTP methods such as `GetFromJsonAsync`, this is a good approach as well at it gives you full support of advanced scenarios, such as custom headers and full control over request and response messages.

    In both options, the final request will be rewritten by the Dapr .NET SDK before it gets executed. In our case and for the GET operation it will be written to this request: `http://127.0.0.1:3500/v1/invoke/tasksmanager-backend-api/method/api/tasks?createdBy=tjoudeh@bitoftech.net`

#### 1.5 Run Backend API and Frontend Web App Locally Using Dapr

We are ready now to verify the changes on the Frontend Web App and test locally. Therefore, we need to run the Frontend Web App along with the Backend Web API to ensure the Dapr Sidecar containers are working as expected. 

- Open another terminal inside VS Code, so that we can run the two commands shown below (ensure that you are on the right project directory when running each command).

- In the second terminal, run `.\Variables.ps1` to apply all session variables.

- Obtain the local frontend UI URL to test shortly once the frontend UI and backend API are running in the next step.

    ```shell
    $FRONTEND_UI_BASE_URL_LOCAL
    ```

- In each of the two terminals previously opened, run the frontend UI and backend API respectively.

    === ".NET 6 or below"

        ```shell
        cd ~\TasksTracker.ContainerApps\TasksTracker.WebPortal.Frontend.Ui 
    
        dapr run `
        --app-id tasksmanager-frontend-webapp `
        --app-port $UI_APP_PORT `
        --dapr-http-port 3501 `
        --app-ssl `
        -- dotnet run 
        ```
    
        ```shell
        cd ~\TasksTracker.ContainerApps\TasksTracker.TasksManager.Backend.Api
        dapr run `
        --app-id tasksmanager-backend-api `
        --app-port $API_APP_PORT `
        --dapr-http-port 3500 `
        --app-ssl `
        -- dotnet run
        ```

    === ".NET 7 or above"

        ```shell
        cd ~\TasksTracker.ContainerApps\TasksTracker.WebPortal.Frontend.Ui
    
        dapr run `
        --app-id tasksmanager-frontend-webapp `
        --app-port $UI_APP_PORT `
        --dapr-http-port 3501 `
        --app-ssl `
        -- dotnet run --launch-profile https
        ```
    
        ```shell
        cd ~\TasksTracker.ContainerApps\TasksTracker.TasksManager.Backend.Api
    
        dapr run `
        --app-id tasksmanager-backend-api `
        --app-port $API_APP_PORT `
        --dapr-http-port 3500 `
        --app-ssl `
        -- dotnet run --launch-profile https
        ```

 Notice how we assigned the Dapr App Id “tasksmanager-frontend-webapp” to the Frontend WebApp.

!!! note
    If you need to run both microservices together, you need to keep calling `dapr run` manually each time in the terminal. And when you have multiple microservices talking to each other you need to run at the same time to debug the solution. This can be a convoluted process. You can refer to the [debug and launch Dapr applications in VSCode](../13-appendix/01-run-debug-dapr-app-vscode.md) to see how to configure VScode for running and debugging Dapr applications.

!!! success
    Now both Applications are running using Dapr sidecar. Open the local frontend UI URL, ignore the certificate warning locally, then provide an email to load the tasks for the user (e.g. `tjoudeh@bitoftech.net`). If the application is working as expected you should see tasks list associated with the email you provided.

- Close the sessions and navigate to the root.

--8<-- "snippets/update-variables.md:7:12"
--8<-- "snippets/persist-state.md:module3"

## Review

In this module, we have accomplished one objective:

1. Decoupled `{{ apps.frontend }}` from `{{ apps.backend }}` by introducing Dapr.

In the next module, we will integrate the Dapr state store building block by saving tasks to Azure Cosmos DB. We will also deploy the updated applications to Azure Container Apps.
