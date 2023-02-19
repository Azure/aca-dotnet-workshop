---
title: Module 2 - Communication between Microservices in ACA
has_children: false
nav_order: 2
canonical_url: 'https://bitoftech.net/2022/08/25/communication-microservices-azure-container-apps/'
---
# Module 2 - Communication between Microservices in ACA

In this module, we will add a the service named `ACA Web API – Frontend` as illustrated in the [architecture diagram](../../assets/images/00-workshop-intro/ACA-Architecture-workshop.jpg). This service will host a simple ASP.NET Razor pages web app which allow end users to manage their tasks. Then we will provision Azure resources needed to deploy the service to ACA using Azure CLI.
### 1. Create the frontend Web App project (Web APP)
1. Open a command-line terminal and navigate to root folder of your project. Create a new folder as shown below:
    ```shell
    mkdir TasksTracker.WebPortal.Frontend.Ui
    cd TasksTracker.WebPortal.Frontend.Ui
    ```

2. From VS Code Terminal tab, open developer command prompt or PowerShell terminal in the project folder `TasksTracker.WebPortal.Frontend.Ui` and initialize the project by typing: `dotnet new webapp  -o TasksTracker.WebPortal.Frontend.Ui` This will create and ASP.NET Razor Pages web app project.

3. We need to containerize this application so we can push it to Azure Container Registry as a docker image then deploy it to ACA, to do so Open the VS Code Command Palette (<kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>p</kbd>) and select `Docker: Add Docker Files to Workspace...`
    - Use `.NET: ASP.NET Core` when prompted for application platform.
    - Choose `Linux` when prompted to choose the operating system.
    - You will be asked if you want to add Docker Compose files. Select `No`.
    - Take a not of the provided **application port** as we will be using later on.
    - `Dockerfile` and `.dockerignore` files are added to the workspace.

4. Add a new folder named `Tasks` under the existing folder named `Pages` and then add a new folder named `Models`, then create a new file named `TaskModel.cs` and paste the code below:
    ```csharp
    public class TaskModel
    {
        public Guid TaskId { get; set; }
        public string TaskName { get; set; } = string.Empty;
        public string TaskCreatedBy { get; set; } = string.Empty;
        public DateTime TaskCreatedOn { get; set; }
        public DateTime TaskDueDate { get; set; }
        public string TaskAssignedTo { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public bool IsOverDue { get; set; }
    }

    public class TaskAddModel
    {
        [Display(Name = "Task Name")]
        [Required]
        public string TaskName { get; set; } = string.Empty;

        [Display(Name = "Task DueDate")]
        [Required]
        public DateTime TaskDueDate { get; set; }

        [Display(Name = "Assigned To")]
        [Required]
        public string TaskAssignedTo { get; set; } = string.Empty;
        public string TaskCreatedBy { get; set; } = string.Empty;
    }

    public class TaskUpdateModel
    {
        public Guid TaskId { get; set; }

        [Display(Name = "Task Name")]
        [Required]
        public string TaskName { get; set; } = string.Empty;

        [Display(Name = "Task DueDate")]
        [Required]
        public DateTime TaskDueDate { get; set; }

        [Display(Name = "Assigned To")]
        [Required]
        public string TaskAssignedTo { get; set; } = string.Empty;
    }
    ```

5. Now we will add 3 Razor pages for CRUD operations which will be responsible for listing all the tasks, creating a new task, and updating existing tasks.

    So add a new empty Razor Page named `Index.cshtml` under the `Tasks` folder and copy the cshtml content from this [link](https://github.com/Azure/aca-dotnet-workshop/blob/c3783f3b23818615f298d52ce9595a059e22d8f6/TasksTracker.WebPortal.Frontend.Ui/Pages/Tasks/Create.cshtml).

    By looking at the cshtml content notice that the page is expecting a query string named `createdBy` which will be used to group tasks for application users, we are following this approach here to keep workshop simple, for production application Authentication should be applied and the user email should be retrieved from the claims identity of the authenticated users.

    Now we will add the code behind the `Index.cshtml` file, so open the file named `Index.cshtml.cs` and paste the code below:

    ```csharp
    namespace TasksTracker.WebPortal.Frontend.Ui.Pages.Tasks
    {
        public class IndexModel : PageModel
        {
            private readonly IHttpClientFactory _httpClientFactory;
            public List<TaskModel>? TasksList { get; set; }
    
            [BindProperty]
            public string? TasksCreatedBy { get; set; }
    
            public IndexModel(IHttpClientFactory httpClientFactory)
            {
                _httpClientFactory = httpClientFactory;
            }
    
            public async Task OnGetAsync()
            {
                TasksCreatedBy = Request.Cookies["TasksCreatedByCookie"];
                // direct svc to svc http request
                var httpClient = _httpClientFactory.CreateClient("BackEndApiExternal");
                TasksList = await httpClient.GetFromJsonAsync<List<TaskModel>>($"api/tasks?createdBy={TasksCreatedBy}");
            }
    
            public async Task<IActionResult> OnPostDeleteAsync(Guid id)
            {
                // direct svc to svc http request
                var httpClient = _httpClientFactory.CreateClient("BackEndApiExternal");
                var result = await httpClient.DeleteAsync($"api/tasks/{id}");
                return RedirectToPage();
            }
    
            public async Task<IActionResult> OnPostCompleteAsync(Guid id)
            {
                // direct svc to svc http request
                var httpClient = _httpClientFactory.CreateClient("BackEndApiExternal");
                var result = await httpClient.PutAsync($"api/tasks/{id}/markcomplete", null);
                return RedirectToPage();
            }
        }
    }
    ```

    What we have done is straightforward, we've injected named HttpClientFactory which is responsible to call the Backend API service as HTTP request. The index page supports deleting and marking tasks as completed along with listing tasks for certain users based on the `createdBy` property stored in a cookie named `TasksCreatedByCookie`, more about we are filling this property later in the post.

    Now we will add a new Razor page named `Create.cshtml` under the `Tasks` folder and copy the cshtml content from this [link](https://github.com/Azure/aca-dotnet-workshop/blob/c3783f3b23818615f298d52ce9595a059e22d8f6/TasksTracker.WebPortal.Frontend.Ui/Pages/Tasks/Create.cshtml).

    Next, we will add the code behind the `Create.cshtml` file, so open the file named `Create.cshtml.cs` and paste the code below:

    ```csharp
    namespace TasksTracker.WebPortal.Frontend.Ui.Pages.Tasks
    {
        public class CreateModel : PageModel
        {
            private readonly IHttpClientFactory _httpClientFactory;
            public CreateModel(IHttpClientFactory httpClientFactory)
            {
                _httpClientFactory = httpClientFactory;
            }

            public IActionResult OnGet()
            {
                return Page();
            }

            [BindProperty]
            public TaskAddModel TaskAdd { get; set; }

            public async Task<IActionResult> OnPostAsync()
            {
                if (!ModelState.IsValid)
                {
                    return Page();
                }

                if (TaskAdd != null)
                {
                    var createdBy = Request.Cookies["TasksCreatedByCookie"];
                    
                    TaskAdd.TaskCreatedBy = createdBy;

                    // direct svc to svc http request
                    var httpClient = _httpClientFactory.CreateClient("BackEndApiExternal");
                    var result = await httpClient.PostAsJsonAsync("api/tasks/", TaskAdd);

                }
                return RedirectToPage("./Index");
            }
        }
    }
    ```
    The code is self-explanatory here, we just injected the type HttpClientFactory in order to issue a POST request and create a new task.

    Lastly, we will add a new Razor page named `Edit.cshtml` under the `Tasks` folder and copy the cshtml content from this [link](https://github.com/Azure/aca-dotnet-workshop/blob/c3783f3b23818615f298d52ce9595a059e22d8f6/TasksTracker.WebPortal.Frontend.Ui/Pages/Tasks/Edit.cshtml).

    Add the code behind to the `Edit.cshtml` file, so open the file named `Edit.cshtml.cs` and paste the code below:

    ```csharp
    namespace TasksTracker.WebPortal.Frontend.Ui.Pages.Tasks
    {
        public class EditModel : PageModel
        {
            private readonly IHttpClientFactory _httpClientFactory;

            [BindProperty]
            public TaskUpdateModel? TaskUpdate { get; set; }

            public EditModel(IHttpClientFactory httpClientFactory)
            {
                _httpClientFactory = httpClientFactory;
            }

            public async Task<IActionResult> OnGetAsync(Guid? id)
            {
                if (id == null)
                {
                    return NotFound();
                }

                // direct svc to svc http request
                var httpClient = _httpClientFactory.CreateClient("BackEndApiExternal");
                var Task = await httpClient.GetFromJsonAsync<TaskModel>($"api/tasks/{id}");

                if (Task == null)
                {
                    return NotFound();
                }

                TaskUpdate = new TaskUpdateModel()
                {
                    TaskId = Task.TaskId,
                    TaskName = Task.TaskName,
                    TaskAssignedTo = Task.TaskAssignedTo,
                    TaskDueDate = Task.TaskDueDate,
                };

                return Page();
            }

            public async Task<IActionResult> OnPostAsync()
            {
                if (!ModelState.IsValid)
                {
                    return Page();
                }

                if (TaskUpdate != null)
                {
                    // direct svc to svc http request
                    var httpClient = _httpClientFactory.CreateClient("BackEndApiExternal");
                    var result = await httpClient.PutAsJsonAsync($"api/tasks/{TaskUpdate.TaskId}", TaskUpdate);
                }

                return RedirectToPage("./Index");
            }
        }
    }
    ```
    The code added is similar to the create operation, the Edit page accepts the TaskId as a Guid, loads the task, then updates the task by sending HTTP PUT operation.

6. Now we will inject HTTP client factory and define environment variables, to do so we will register the HttpClientFactory named `BackEndApiExternal` to make it available for injection in controllers, so open file `Program.cs` and paste the code below:

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

                builder.Services.AddHttpClient("BackEndApiExternal", httpClient =>
                {
                    httpClient.BaseAddress = new Uri(builder.Configuration.GetValue<string>("BackendApiConfig:BaseUrlExternalHttp"));
        
                });

                var app = builder.Build();

                // Code removed for brevity 
            }
        }
    }
    ```
    Next, we will add a new environment variable named `BackendApiConfig:BaseUrlExternalHttp` into `appsettings.json` file, this variable will contain the Base URL for the backend API deployed in the previous module to ACA. Later on in this post, we will see how we can set the environment variable once we deploy it to ACA.

    ```json
    {
        "Logging": {
            "LogLevel": {
            "Default": "Information",
            "Microsoft.AspNetCore": "Warning"
            }
        },
        "AllowedHosts": "*",
        "BackendApiConfig": {
            "BaseUrlExternalHttp": "https://tasksmanager-backend-api.agreeablestone-8c14c04c.eastus.azurecontainerapps.io"
        }
    }
    ```

7. Lastly, we will update the web app landing page `Index.html` to capture the email of the tasks owner user and assign this email to a cookie named `TasksCreatedByCookie`, Navigate to the page named `Pages\Index.csthml` and replace the HTML with the one in this [link](https://github.com/Azure/aca-dotnet-workshop/blob/c3783f3b23818615f298d52ce9595a059e22d8f6/TasksTracker.WebPortal.Frontend.Ui/Pages/Index.cshtml), then update code behind of `Index.csthml.cs` by pasting the code below:

    ```csharp
    namespace TasksTracker.WebPortal.Frontend.Ui.Pages
    {
        [IgnoreAntiforgeryToken(Order = 1001)]
        public class IndexModel : PageModel
        {
            private readonly ILogger<IndexModel> _logger;
            [BindProperty]
            public string TasksCreatedBy { get; set; }

            public IndexModel(ILogger<IndexModel> logger)
            {
                _logger = logger;
            }

            public void OnGet()
            {
            }

            public IActionResult OnPost()
            {
                if (!string.IsNullOrEmpty(TasksCreatedBy))
                {
                    Response.Cookies.Append("TasksCreatedByCookie", TasksCreatedBy);
                }

                return RedirectToPage("./Tasks/Index");
            }
        }
    }
    ```

### 2. Deploy Razor Pages Web App Frontend Project to ACA

I will assume that you still have the same PowerShell console session opened from the last module, we need to add the below PS variables:

```powershell
$FRONTEND_WEBAPP_NAME="tasksmanager-frontend-webapp"
```
1. Now we will build the Web App project on ACR and push the docker image to ACR. Use the below command to initiate the image build and push process using ACR. The `.` at the end of the command represents the docker build context, in our case, we need to be on the parent directory which hosts the .csproject.

    ```powershell
    cd {YourLocalPath}\TasksTracker.ContainerApps 
    az acr build --registry $ACR_NAME --image "tasksmanager/$FRONTEND_WEBAPP_NAME" --file 'TasksTracker.WebPortal.Frontend.Ui/Dockerfile' .
    ```
    Once this step is completed you can verify the results by going to the Azure portal and checking that a new repository named `tasksmanager/tasksmanager-frontend-webapp` has been created and there is a new docker image with a `latest` tag is created.

2. Next, we will create and deploy the Web App to ACA following the below command:

    ```powershell
    az containerapp create `
   --name "$FRONTEND_WEBAPP_NAME"  `
   --resource-group $RESOURCE_GROUP `
   --environment $ENVIRONMENT `
   --image "$ACR_NAME.azurecr.io/tasksmanager/$FRONTEND_WEBAPP_NAME" `
   --registry-server "$ACR_NAME.azurecr.io" `
   --env-vars BackendApiConfig_BaseUrlExternalHttp=https://tasksmanager-backend-api.agreeablestone-8c14c04c.eastus.azurecontainerapps.io/ `
   --target-port 80 `
   --ingress 'external' `
   --min-replicas 1 `
   --max-replicas 1 `
   --cpu 0.25 --memory 0.5Gi `
   --query configuration.ingress.fqdn
    ```
    Notice how we used the property `env-vars` to set the value of the environment variable named `BackendApiConfig_BaseUrlExternalHttp` which we added in the AppSettings.json file. You can set multiple environment variables at the same time by using a space between each variable

    The `ingress` property is set to `external` as the Web frontend App will be exposed to the public internet for users.

    After your run the command, copy the FQDN (Application URL) of the Azure container app named `tasksmanager-frontend-webapp` and open it in your browser and you should be able to browse the frontend web app and manage your tasks.

### 3. Update Backend Web API Container App Ingress property

So far the Frontend App is sending HTTP requests to publicly exposed Web API, any REST client can invoke this API, we need to change the Web API ingress settings and make it only accessible for applications deployed within our Azure Container Environment only, any application outside the Azure Container Environment will not be able to access the Web API.

1. To change the settings of the Backend API, fire the below CLI command:

    ```powershell
    az containerapp ingress enable `
    --name  $BACKEND_API_NAME  `
    --resource-group  $RESOURCE_GROUP `
    --target-port 80 `
    --type "internal"
    ```

    When you do this change, the FQDN (Application URL) will change and it will be similar to the below, notice how there is an `Internal` part of the URL. `https://tasksmanager-backend-api.internal.agreeablestone-8c14c04c.eastus.azurecontainerapps.io/api/tasks/`

    If you try to invoke the URL from the browser directly it will return 404 as this Internal Url can only be accessed from container apps within the container environment.

    The FQDN consists of multiple parts, for example, all our Container Apps we are going to create will be under the Environment unique identifier `agreeablestone-8c14c04c` and the Container App will vary based on the name provided, check the image below for a better explanation.
    ![Container Apps FQDN](../../assets/images/02-aca-comm/container-apps-fqdn.jpg)

2. Update the Frontend Web App environment variable to point to the internal backend Web API FQDN. The last thing we need to do here is to update the Frontend WebApp environment variable named `BackendApiConfig_BaseUrlExternalHttp` with the new value of the internal Backend Web API base URL, to do so we need to update the Web App container app and it will create a new revision implicitly (More about revisions in the upcoming modules) to do so, execute the below command:

    ```powershell
    az containerapp update `
    --name "$FRONTEND_WEBAPP_NAME"  `
    --resource-group $RESOURCE_GROUP `
    --set-env-vars BackendApiConfig__BaseUrlExternalHttp=https://tasksmanager-backend-api.internal.agreeablestone-8c14c04c.eastus.azurecontainerapps.io `
    ```
    Browse the web app again and you should be able to see the same results and access the backend API endpoints from the Web App.

    In the next module, we will start integrating Dapr and use the service to service Building block for services discovery and invocation.