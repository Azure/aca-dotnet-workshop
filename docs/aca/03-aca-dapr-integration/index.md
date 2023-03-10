---
title: Module 3 - Dapr Integration with ACA
has_children: false
nav_order: 3
canonical_url: 'https://bitoftech.net/2022/08/29/dapr-integration-with-azure-container-apps/'
---
# Module 3 - Dapr Integration with ACA

In this module, we will start integrating Dapr into both services and see how Dapr with ACA will simplify complex microservices scenarios such as service discovery, service-to-service invocation, calling services asynchronously via pub/sub patterns, auto-scaling for overloaded services, etc..

### Benefits of integrating Dapr in Azure Container Apps
The Tasks Tracker microservice application is composed of multiple microservices (2 microservices so far), and function calls are spread across the network. To support the distributed nature of microservices, we need to account for failures, retries, and timeouts. While Container Apps features the building blocks for running microservices, the use of Dapr provides an even richer microservices programming model. Dapr includes features like service discovery, pub/sub, service-to-service invocation with mutual TLS, retries, state store management, and more. Here is a good [link](https://learn.microsoft.com/en-us/dotnet/architecture/dapr-for-net-developers/service-invocation) which touches on some of the benefits of the Dapr service invocation building block which we will be building upon in this module. Because the calls will flow through sidecars, Dapr can inject some useful cross-cutting behaviors. Although we won't tap into all these benefits in this workshop its worth keeping in mind that you will most probably need to rely on these features in production.
* Automatically retry calls upon failure.
* Make calls between services secure with mutual (mTLS) authentication, including automatic certificate rollover.
* Control what operations clients can do using access control policies.
* Capture traces and metrics for all calls between services to provide insights and diagnostics. 

### Configure Dapr on a local development machine
In order to run applications using Dapr, we need to install and initialize Dapr CLI locally. The official documentation is quite clear and we can follow the steps needed to [install](https://docs.dapr.io/getting-started/install-dapr-cli/) Dapr and then [Initialize](https://docs.dapr.io/getting-started/install-dapr-selfhost/) it.

### Run Backend API and Frontend Web App locally using Dapr
You are now ready to run the applications locally using Dapr sidecar in a self-hosted mode. There is a VS code extension called [Dapr](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-dapr) which will allow you to run, debug, and interact with Dapr-enabled applications in VS Code.

1. Lets start by running the Backend Web API service using Dapr. From VS Code open a new PowerShell terminal, change the directory in the terminal to folder `TasksTracker.TasksManager.Backend.Api` and run the below command in PS terminal. Remember to replace the place holders with your own values.

    ```powershell
    If you are on .Net 6 or below use the following command:
    dapr run --app-id tasksmanager-backend-api --app-port <web api application https port number found under properties->launchSettings.json. e.g. 7112> --dapr-http-port 3500 --app-ssl -- dotnet run
    ```
    ```powershell
    If you are on .Net 7 or above use the following command:
    dapr run --app-id tasksmanager-backend-api --app-port <web api application https port number found under properties->launchSettings.json. e.g. 7112> --dapr-http-port 3500 --app-ssl -- dotnet run --launch-profile https
    ```

    ![app-port](../../assets/images/03-aca-dapr-integration/self_hosted_dapr_app-port.png)

    When using Dapr run command you are running a dapr process as a sidecar next to the Web API application. The properties you have configured are as follows:
    * app-id: The unique identifier of the application. Used for service discovery, state encapsulation, and the pub/sub consumer identifier.
    * app-port: This parameter tells Dapr which port your application is listening on. You can get the app port from `properties->launchSettings.json` file in the Web API Project as shown in the image above. Make sure you use the https port listed within the `properties->launchSettings.json` as we are using the --app-ssl when running the dapr cli locally. Don't use the port inside the DockerFile. The DockerFile port will come in handy when you deploy to ACA at which point the application would be running inside a container.
    * dapr-http-port: The HTTP port for Dapr to listen on.
    * app-ssl: Sets the URI scheme of the app to https and attempts an SSL connection.
    
    For a full list of properties, you can check this [link.](https://docs.dapr.io/reference/cli/dapr-run/)

    If all is working as expected, you should receive an output similar to the one below where your app logs and dapr logs will appear on the same PowerShell terminal:
    ![dapr-logs](../../assets/images/03-aca-dapr-integration/dapr-logs.jpg)

    Now to test invoking the Web API using Dapr sidecar, you can issue HTTP GET request to the following URL: `http://localhost:3500/v1.0/invoke/tasksmanager-backend-api/method/api/tasks?createdBy=tjoudeh@bitoftech.net`

    What happened here is that Dapr exposes its HTTP and gRPC APIs as a sidecar process which can access our Backend Web API. We didn't do any changes to the application code to include any Dapr runtime code. We also ensured separation of the application logic for improved supportability.

    Looking back at the HTTP GET request, we can break it as follows:

    * `/v1.0/invoke` Endpoint: is the Dapr feature identifier for the "Service to Service invocation" building block. This building block enables applications to communicate with each other through well-known endpoints in the form of http or gRPC messages. Dapr provides an endpoint that acts as a combination of a reverse proxy with built-in service discovery while leveraging built-in distributed tracing and error handling.
    * `3500`: the HTTP port that Dapr is listening on.
    * `tasksmanager-backend-api`: is the dapr application unique identifier.
    * `method`: reserved word when using invoke endpoint.
    * `api/tasks?createdBy=tjoudeh@bitoftech.net`: the path of the action method that needs to be invoked in the webapi service.

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
2. Next, we will be using Dapr SDK in the frontend Web App to invoke Backend API services, The [Dapr .NET SDK](https://github.com/dapr/dotnet-sdk) provides .NET developers with an intuitive and language-specific way to interact with Dapr. The SDK offers developers three ways of making remote service invocation calls:
    1. Invoke HTTP services using HttpClient
    2. Invoke HTTP services using DaprClient
    3. Invoke gRPC services using DaprClient

    We will be using the second approach in this workshop (HTTP services using DaprClient), but it is worth spending some time explaining the first approach (Invoke HTTP services using HttpClient). We will go over the first approach briefly and then discuss the second in details.

    Install DAPR SDK for .NET Core in the Frontend Web APP so we can use the service discovery and service invocation offered by Dapr Sidecar. To do so, open the .csproj file of the project `TasksTracker.WebPortal.Frontend.Ui.csproj` and add the following NuGet package:

    ```json
    <ItemGroup>
        <PackageReference Include="Dapr.AspNetCore" Version="1.9.0" />
    </ItemGroup>
    ```
    Next, open the file `Programs.cs` of the Frontend Web App and register the DaprClient as the code below. 
    The `AddDaprClient` call registers the `DaprClient` class with the ASP.NET Core dependency injection system. With the client registered, you can now inject an instance of DaprClient into your service code to communicate with the Dapr sidecar, building blocks, and components.

    ```csharp
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
    Now, we will inject the DaprClient into the Index.cshtml page to use the method `InvokeMethodAsync` (second approach). To do so, open the page named `Index.cshtml.cs` under folder `Pages\Tasks` and use the code below
    ```csharp
    using Dapr.Client;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    namespace TasksTracker.WebPortal.Frontend.Ui.Pages.Tasks
        {
            [IgnoreAntiforgeryToken(Order = 1001)]
            public class IndexModel : PageModel
            {

                private readonly IHttpClientFactory _httpClientFactory;
                private readonly DaprClient _daprClient;
                public List<TaskModel>? TasksList { get; set; }

                [BindProperty]
                public string? TasksCreatedBy { get; set; }

                public IndexModel(IHttpClientFactory httpClientFactory, DaprClient daprClient)
                {
                    _httpClientFactory = httpClientFactory;
                    _daprClient = daprClient;
                }

                public async Task OnGetAsync()
                {
                
                    TasksCreatedBy = Request.Cookies["TasksCreatedByCookie"];

                    //Invoke via internal URL (Not Dapr)
                    //var httpClient = _httpClientFactory.CreateClient("BackEndApiExternal");
                    //TasksList = await httpClient.GetFromJsonAsync<List<TaskModel>>($"api/tasks?createdBy={TasksCreatedBy}");


                    // Invoke via Dapr SideCar URL
                    //var port = 3500;//Environment.GetEnvironmentVariable("DAPR_HTTP_PORT");
                    //HttpClient client = new HttpClient();
                    //var result = await client.GetFromJsonAsync<List<TaskModel>>($"http://localhost:{port}/v1.0/invoke/tasksmanager-backend-api/method/api/tasks?createdBy={TasksCreatedBy}");
                    //TasksList = result;

                    // Invoke via DaprSDK (Invoke HTTP services using HttpClient) --> Use Dapr Desitination App ID (Option 1)
                    //var daprHttpClient = DaprClient.CreateInvokeHttpClient(appId: "tasksmanager-backend-api"); 
                    //TasksList = await daprHttpClient.GetFromJsonAsync<List<TaskModel>>($"api/tasks?createdBy={TasksCreatedBy}");
                    
                    // Invoke via DaprSDK (Invoke HTTP services using HttpClient) --> Specify Port (Option 2)
                    //var daprHttpClient = DaprClient.CreateInvokeHttpClient(daprEndpoint: "http://localhost:3500"); 
                    //TasksList = await daprHttpClient.GetFromJsonAsync<List<TaskModel>>($"http://tasksmanager-backend-api/api/tasks?createdBy={TasksCreatedBy}");
                    
                    // Invoke via DaprSDK (Invoke HTTP services using DaprClient)
                    TasksList = await _daprClient.InvokeMethodAsync<List<TaskModel>>(HttpMethod.Get, "tasksmanager-backend-api", $"api/tasks?createdBy={TasksCreatedBy}");

                }

                public async Task<IActionResult> OnPostDeleteAsync(Guid id)
                {
                    // direct svc to svc http request
                    // var httpClient = _httpClientFactory.CreateClient("BackEndApiExternal");
                    // var result = await httpClient.DeleteAsync($"api/tasks/{id}");

                    //Dapr SideCar Invocation
                    await _daprClient.InvokeMethodAsync(HttpMethod.Delete, "tasksmanager-backend-api", $"api/tasks/{id}");

                    return RedirectToPage();          
                }

                public async Task<IActionResult> OnPostCompleteAsync(Guid id)
                {
                    // direct svc to svc http request
                    // var httpClient = _httpClientFactory.CreateClient("BackEndApiExternal");
                    // var result = await httpClient.PutAsync($"api/tasks/{id}/markcomplete", null);

                    //Dapr SideCar Invocation
                    await _daprClient.InvokeMethodAsync(HttpMethod.Put, "tasksmanager-backend-api", $"api/tasks/{id}/markcomplete");

                    return RedirectToPage();
                }
            }
        }
    ```
    Notice how we are not using the `HttpClientFactory` anymore and how we were able from the Frontend Dapr Sidecar to invoke backend API Sidecar using the method `InvokeMethodAsync` which accepts the Dapr **remote App ID** for the Backend API `tasksmanager-backend-api` and it will be able to discover the URL and invoke the method based on the specified input params.

    In addition to this, notice how in POST and PUT operations, the third argument is a `TaskAdd` or `TaskUpdate` Model, those objects will be serialized internally (using System.Text.JsonSerializer) and sent as the request payload. The .NET SDK takes care of the call to the Sidecar. It also deserializes the response in case of the GET operations to a `List<TaskModel>` object.

    Looking at the first option of invoking the remote service "Invoke HTTP services using HttpClient", you can see that we can create an HttpClient by invoking `DaprClient.CreateInvokeHttpClient` and specify the remote service app id, custom port if needed and then use the HTTP methods such as `GetFromJsonAsync`, this is a good approach as well at it gives you full support of advanced scenarios, such as custom headers and full control over request and response messages.

    In both options, the final request will be rewritten by the Dapr .NET SDK before it gets executed. In our case and for the GET operation it will be written to this request: `http://127.0.0.1:3500/v1/invoke/tasksmanager-backend-api/method/api/tasks?createdBy=tjoudeh@bitoftech.net`

    We need now to update the [Create.cshtml.cs](https://github.com/Azure/aca-dotnet-workshop/blob/5dc6b68dcf118440df4c96c14dd538d4d69f80f4/TasksTracker.WebPortal.Frontend.Ui/Pages/Tasks/Create.cshtml.cs) and [Edit.cshtml.cs](https://github.com/Azure/aca-dotnet-workshop/blob/5dc6b68dcf118440df4c96c14dd538d4d69f80f4/TasksTracker.WebPortal.Frontend.Ui/Pages/Tasks/Edit.cshtml.cs) by injecting the DaprClient.

3. We are ready now to verify changes on Frontend Web App and test locally, we need to run the Frontend Web App along with the Backend Web API and test locally that changes using the .NET SDK and invoking services via Dapr Sidecar are working as expected. To do so run the two commands commands shown below (ensure that you are on the right project directory when running each command). Remember to replace the place holders with your own values:

    ```powershell
    If you are on .Net 6 or below use the following commands:

    ~\TasksTracker.ContainerApps\TasksTracker.WebPortal.Frontend.Ui> dapr run --app-id tasksmanager-frontend-webapp --app-port <web frontend application https port found under properties->launchSettings.json. e.g. 7000> --dapr-http-port 3501 --app-ssl -- dotnet run 

    ~\TasksTracker.ContainerApps\TasksTracker.TasksManager.Backend.Api> dapr run --app-id tasksmanager-backend-api --app-port <web api application https port found under properties->launchSettings.json. e.g. 7112> --dapr-http-port 3500 --app-ssl -- dotnet run

    If you are on .Net 7 or above use the following commands:

    ~\TasksTracker.ContainerApps\TasksTracker.WebPortal.Frontend.Ui> dapr run --app-id tasksmanager-frontend-webapp --app-port <web frontend application https port found under properties->launchSettings.json. e.g. 7000> --dapr-http-port 3501 --app-ssl -- dotnet run --launch-profile https

    ~\TasksTracker.ContainerApps\TasksTracker.TasksManager.Backend.Api> dapr run --app-id tasksmanager-backend-api --app-port <web api application https port found under properties->launchSettings.json. e.g. 7112> --dapr-http-port 3500 --app-ssl -- dotnet run --launch-profile https
    ```

    Notice how we assigned the Dapr App Id ???tasksmanager-frontend-webapp??? to the Frontend WebApp.

    Now both Applications are running using Dapr sidecar. Open your browser and browse for "https://localhost:{localwebappport}". E.g. "https://localhost:7000" and provide an email to load the tasks for the user (e.g. tjoudeh@bitoftech.net). If the application is working as expected you should see tasks list associated with the email you provided (e.g. tjoudeh@bitoftech.net).

{: .note }
If you need to run both microservices together, you need to keep calling `dapr run` manually each time in the terminal. And when you have multiple microservices talking to each other you need to run at the same time to debug the solution. This can be a convoluted process. You can refer to the [debug and launch Dapr applications in VSCode](../../aca/20-appendix/01-run-debug-dapr-app-vscode.md) to see how to configure VScode for running and debugging Dapr applications.

In the next module, we will integrate the Dapr state store building block by saving tasks to Azure Cosmos DB. We will also deploy the updated applications to Azure Container Apps.