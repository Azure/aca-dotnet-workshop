---
title: Module 1 - Deploy Backend API to ACA
has_children: false
nav_order: 1
canonical_url: 'https://bitoftech.net/2022/08/25/deploy-microservice-application-azure-container-apps/'
---
# Module 1 - Deploy Backend API to ACA

In this module, we will start by creating the first microservice named `ACA Web API – Backend` as illustrated in the [architecture diagram](../../assets/images/00-workshop-intro/ACA-Architecture-workshop.jpg). Then we will provision Azure resources needed to deploy the service to Azure Container Apps using Azure CLI.
### 1. Create the backend API project (Web API)
1. Open a command-line terminal and create a folder for your project. Use the `code` command to launch Visual Studio Code from that directory as shown:

   ```shell
    mkdir TasksTracker.TasksManager.Backend.Api
    cd TasksTracker.TasksManager.Backend.Api
    code .
    ```

2. From VS Code Terminal tab, open developer command prompt or PowerShell terminal in the project folder `TasksTracker.TasksManager.Backend.Api` and initialize the project by typing: `dotnet new webapi -o TasksTracker.TasksManager.Backend.Api` This will create and ASP.NET Web API project scaffolded with 1 single controller. 

3. We need to containerize this application so we can push it to Azure Container Registry as a docker image then deploy it to Azure Container Apps, to do so Open the VS Code Command Palette (<kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>p</kbd>) and select `Docker: Add Docker Files to Workspace...`
    - Use `.NET: ASP.NET Core` when prompted for application platform.
    - Choose `Linux` when prompted to choose the operating system.
    - You will be asked if you want to add Docker Compose files. Select `No`.
    - Take a not of the provided **application port** as we will be using later on.
    - `Dockerfile` and `.dockerignore` files are added to the workspace.

4. Add a new folder named `Models` and create a new file named `TaskModel.cs` and paste the code below, those are the DTOs that will be used across the projects.
    ```csharp
    namespace TasksTracker.TasksManager.Backend.Api.Models
    {
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
            public string TaskName { get; set; } = string.Empty;
            public string TaskCreatedBy { get; set; } = string.Empty;
            public DateTime TaskDueDate { get; set; }
            public string TaskAssignedTo { get; set; } = string.Empty;
        }

        public class TaskUpdateModel
        {
            public Guid TaskId { get; set; }
            public string TaskName { get; set; } = string.Empty;
            public DateTime TaskDueDate { get; set; }
            public string TaskAssignedTo { get; set; } = string.Empty;
        }
    ```

5. We will work initially with data in memory to keep things simple with very limited dependency on any other components or data store and focus on the deployment of the backend API to ACA, In the upcoming modules we will switch this implementation with a concrete one where we are going to store data in Redis and Azure Cosmos DB using Dapr State Store building block, so to add the Fake Tasks Manager service (In-memory), create new folder named `Services` and add a new file named `ITasksManager.cs`, this will be the interface of Tasks Manager service. Paste the code below:
    ```csharp
    using TasksTracker.TasksManager.Backend.Api.Models;
        namespace TasksTracker.TasksManager.Backend.Api.Services
        {
            public interface ITasksManager
            {
                Task<List<TaskModel>> GetTasksByCreator(string createdBy);
                Task<TaskModel?> GetTaskById(Guid taskId);
                Task<Guid> CreateNewTask(string taskName, string createdBy, string assignedTo, DateTime dueDate);
                Task<bool> UpdateTask(Guid taskId, string taskName, string assignedTo, DateTime dueDate);
                Task<bool> MarkTaskCompleted(Guid taskId);
                Task<bool> DeleteTask(Guid taskId);
            }
        }
    ```

    To implement the interface, add a new file named `FakeTasksManager.cs` and paste the code below:
    ```csharp
    using TasksTracker.TasksManager.Backend.Api.Models;

    namespace TasksTracker.TasksManager.Backend.Api.Services
    {
        public class FakeTasksManager : ITasksManager
        {
            private List<TaskModel> _tasksList = new List<TaskModel>();
            Random rnd = new Random();

            private void GenerateRandomTasks()
            {
                for (int i = 0; i < 10; i++)
                {
                    var task = new TaskModel()
                    {
                        TaskId = Guid.NewGuid(),
                        TaskName = $"Task number: {i}",
                        TaskCreatedBy = "tjoudeh@bitoftech.net",
                        TaskCreatedOn = DateTime.UtcNow.AddMinutes(i),
                        TaskDueDate = DateTime.UtcNow.AddDays(i),
                        TaskAssignedTo = $"assignee{rnd.Next(50)}@mail.com",
                    };
                    _tasksList.Add(task);
                }
            }

            public FakeTasksManager()
            {
                GenerateRandomTasks();
            }

            public Task<Guid> CreateNewTask(string taskName, string createdBy, string assignedTo, DateTime dueDate)
            {
                var task = new TaskModel()
                {
                    TaskId = Guid.NewGuid(),
                    TaskName = taskName,
                    TaskCreatedBy = createdBy,
                    TaskCreatedOn = DateTime.UtcNow,
                    TaskDueDate = dueDate,
                    TaskAssignedTo = assignedTo,
                };
                _tasksList.Add(task);
                return Task.FromResult(task.TaskId);
            }

            public Task<bool> DeleteTask(Guid taskId)
            {
                var task = _tasksList.FirstOrDefault(t => t.TaskId.Equals(taskId));
                if (task != null)
                {
                    _tasksList.Remove(task);
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            }

            public Task<TaskModel?> GetTaskById(Guid taskId)
            {
                var taskModel = _tasksList.FirstOrDefault(t => t.TaskId.Equals(taskId));
                return Task.FromResult(taskModel);
            }

            public Task<List<TaskModel>> GetTasksByCreator(string createdBy)
            {
                var tasksList = _tasksList.Where(t => t.TaskCreatedBy.Equals(createdBy)).OrderByDescending(o => o.TaskCreatedOn).ToList();
                return Task.FromResult(tasksList);
            }

            public Task<bool> MarkTaskCompleted(Guid taskId)
            {
                var task = _tasksList.FirstOrDefault(t => t.TaskId.Equals(taskId));
                if (task != null)
                {
                    task.IsCompleted = true;
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            }

            public Task<bool> UpdateTask(Guid taskId, string taskName, string assignedTo, DateTime dueDate)
            {
                var task = _tasksList.FirstOrDefault(t => t.TaskId.Equals(taskId));
                if (task != null)
                {
                    task.TaskName = taskName;
                    task.TaskAssignedTo = assignedTo;
                    task.TaskDueDate = dueDate;
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            }
        }
    }
    ```
    The code above is self-explanatory, it generates 10 tasks and stores them in a list in memory, as well it has some operations to add/remove/update those tasks.

6. Now we need to register FakeTasksManager on project startup, Open file `Program.cs` and register the newly created service by adding the line `builder.Services.AddSingleton<ITasksManager, FakeTasksManager>();` as the snippet below

    ```csharp
    var builder = WebApplication.CreateBuilder(args);
    // Add services to the container.
    builder.Services.AddSingleton<ITasksManager, FakeTasksManager>();
    // Code removed for brevity
    app.Run();
    ```

7. Finally, we need to create API endpoints to manage tasks, add a new controller named `TasksController.cs` under the folder `Controllers` which will contain simple action methods to enable basic CRUD operations on tasks

    ```csharp
    namespace TasksTracker.TasksManager.Backend.Api.Controllers
    {
        [Route("api/tasks")]
        [ApiController]
        public class TasksController : ControllerBase
        {
            private readonly ILogger<TasksController> _logger;
            private readonly ITasksManager _tasksManager;

            public TasksController(ILogger<TasksController> logger, ITasksManager tasksManager)
            {
                _logger = logger;
                _tasksManager = tasksManager;
            }

            [HttpGet]
            public async Task<IEnumerable<TaskModel>> Get(string createdBy)
            {
                return await _tasksManager.GetTasksByCreator(createdBy);
            }

            [HttpGet("{taskId}")]
            public async Task<IActionResult> GetTask(Guid taskId)
            {
                var task = await _tasksManager.GetTaskById(taskId);
                if (task != null)
                {
                    return Ok(task);
                }
                return NotFound();

            }

            [HttpPost]
            public async Task<IActionResult> Post([FromBody] TaskAddModel taskAddModel)
            {
                var taskId = await _tasksManager.CreateNewTask(taskAddModel.TaskName,
                                    taskAddModel.TaskCreatedBy,
                                    taskAddModel.TaskAssignedTo,
                                    taskAddModel.TaskDueDate);
                return Created($"/api/tasks/{taskId}", null);

            }

            [HttpPut("{taskId}")]
            public async Task<IActionResult> Put(Guid taskId, [FromBody] TaskUpdateModel taskUpdateModel)
            {
                var updated = await _tasksManager.UpdateTask(taskId,
                                                        taskUpdateModel.TaskName,
                                                        taskUpdateModel.TaskAssignedTo,
                                                        taskUpdateModel.TaskDueDate);
                if (updated)
                {
                    return Ok();
                }
                return BadRequest();
            }

            [HttpPut("{taskId}/markcomplete")]
            public async Task<IActionResult> MarkComplete(Guid taskId)
            {
                var updated = await _tasksManager.MarkTaskCompleted(taskId);
                if (updated)
                {
                    return Ok();
                }
                return BadRequest();
            }

            [HttpDelete("{taskId}")]
            public async Task<IActionResult> Delete(Guid taskId)
            {
                var deleted = await _tasksManager.DeleteTask(taskId);
                if (deleted)
                {
                    return Ok();
                }
                return NotFound();
            }
        }
    }
    ```

### 2. Deploy Web API Backend Project to ACA
We will be using Azure CLI to deploy the Web API Backend to ACA, to do so follow the below steps:
1. We want to Install/Upgrade Azure Container Apps Extension, Open PowerShell console and Login to Azure by using the command `az login` if you have multiple subscriptions, set the subscription you want to use for this workshop before proceeding, you can do this by using `az account set --subscription <name or id>` As well calling `az upgrade` is a good practice to ensure you are running the latest Azure CLI Command. To install or update the Azure Container Apps extension for the CLI run the following command `az extension add --name containerapp --upgrade`

2. Define the below variables in the PowerShell console to use them across the different modules in the workshop, you should change the values of those variables to be able to create the resources successfully, some of those variables should be unique across all Azure subscriptions such as Azure Container Registry name.
    ```shell
    $RESOURCE_GROUP="tasks-tracker-rg"
    $LOCATION="eastus"
    $ENVIRONMENT="tasks-tracker-containerapps-env"
    $BACKEND_API_NAME="tasksmanager-backend-api"
    $ACR_NAME="taskstrackeracr"
    ```

3. Create a `resource group` to organize the services related to the application, run the below command:
    ```shell
    az group create `
    --name $RESOURCE_GROUP `
    --location "$LOCATION"
    ```

4. Create an Azure Container Registry (ACR) instance in the resource group to store images of all Microservices we are going to build during this tutorial, make sure that you set the `admin-enabled` flag to true in order to seamlessly authenticate Azure container app when trying to create the container app using the image stored in ACR

    ```shell
    az acr create `
    --resource-group $RESOURCE_GROUP `
    --name $ACR_NAME `
    --sku Basic `
    --admin-enabled true
    ```
    {: .note }
    Notice that we create the registry with admin rights `--admin-enabled` flag set to `true` which is not suited for real production, but good for our workshop.

5. Build the Web API project on ACR and push the docker image to ACR. Use the below command to initiate the image build and push process using ACR. The `.` at the end of the command represents the docker build context, in our case, we need to be on the parent directory which hosts the `.csproj`.

    ```shell
    cd {YourLocalPath}\TasksTracker.ContainerApps
    az acr build --registry $ACR_NAME --image "tasksmanager/$BACKEND_API_NAME" --file 'TasksTracker.TasksManager.Backend.Api/Dockerfile' .
    ```

6. Create an Azure Container Apps Environment, as shared in the [workshop introduction](../../aca/00-workshop-intro/1-aca-core-components.md), it acts as a secure boundary around a group of all container apps we are going to provision during this tutorial, to create it, run the below command:
    ```shell
    az containerapp env create `
    --name $ENVIRONMENT `
    --resource-group $RESOURCE_GROUP `
    --location $LOCATION
    ```

7. The last step here is to create and deploy the Web API to ACA following the below command:

    ```shell
    az containerapp create `
    --name $BACKEND_API_NAME  `
    --resource-group $RESOURCE_GROUP `
    --environment $ENVIRONMENT `
    --image "$ACR_NAME.azurecr.io/tasksmanager/$BACKEND_API_NAME" `
    --registry-server "$ACR_NAME.azurecr.io" `
    --target-port 80 `
    --ingress 'external' `
    --min-replicas 1 `
    --max-replicas 2 `
    --cpu 0.25 --memory 0.5Gi `
    --query configuration.ingress.fqdn
    ```
    What we've done in the above command is the following:
    * Ingress param is set to `external` which means that this container app (Web API) project will be accessible from the public internet. When Ingress is set to `Internal` or `External` it will be assigned a fully qualified domain name (FQDN). Important notes about IP addresses and domain names can be found [here](https://learn.microsoft.com/en-us/azure/container-apps/ingress?tabs=bash#ip-addresses-and-domain-names).
    * The target port param is set to 80, this is the port our Web API container listens to for incoming requests.
    * We didn't specify the ACR registry username and password, `az containerapp create` command was able to look up ACR username and password and add them as a secret under the created Azure container app for future container updates.
    * The minimum and the maximum number of replicas are set, more about this when we cover Autoscaling in the future modules. In the meantime, only 1 single instance of this container app will be provisioned as Auto scale is not configured.
    * We are setting the size of the Container App, the total amount of CPUs and memory requested for the container app must add up to certain combinations, for full details check the link [here](https://docs.microsoft.com/en-us/azure/container-apps/containers#configuration).
    * The `query` property will filter the response coming from the command and just return the FQDN.

    For full details on all available parameters for this command, please visit this [page](https://docs.microsoft.com/en-us/cli/azure/containerapp?view=azure-cli-latest#az-containerapp-create).  

8. You can now verify the deployment of the first ACA by navigating to the Azure Portal, login with your credentials, and select the resource group named `tasks-tracker-rg` created, you should see the 4 recourses created below. By default when you create an Azure Container Environment, a `Log Analytics `Workspace will be created, we will cover this in the Monitoring and observability future module.
![Azure Resources](../../assets/images/01-deploy-api-to-aca/Resources.jpg)

    To execute an e2e test copy the FQDN (Application URL) of the Azure container app named `tasksmanager-backend-api` and issue a `GET` request similar to this one: `https://tasksmanager-backend-api.<your-aca-env-unique-id>.eastus.azurecontainerapps.io/api/tasks/?createdby=tjoudeh@bitoftech.net` and you should receive an array of the 10 tasks similar to the below image
    ![Web API Response](../../assets/images/01-deploy-api-to-aca/Response.jpg)

In the next module, we will see how we will add a new Frontend Web App as a microservice and how it will communicate with the backend API.