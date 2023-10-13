using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TasksTracker.WebPortal.Frontend.Ui.Pages.Tasks.Models;

namespace TasksTracker.WebPortal.Frontend.Ui.Pages.Tasks
{
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

            // Invoke via internal URL (Not Dapr)
            //var httpClient = _httpClientFactory.CreateClient("BackEndApiExternal");
            //TasksList = await httpClient.GetFromJsonAsync<List<TaskModel>>($"api/tasks?createdBy={TasksCreatedBy}");

            // Invoke via Dapr SideCar URL
            //var port = 3500;//Environment.GetEnvironmentVariable("DAPR_HTTP_PORT");
            //HttpClient client = new HttpClient();
            //var result = await client.GetFromJsonAsync<List<TaskModel>>($"http://localhost:{port}/v1.0/invoke/tasksmanager-backend-api/method/api/tasks?createdBy={TasksCreatedBy}");
            //TasksList = result;

            // Invoke via DaprSDK (Invoke HTTP services using HttpClient) --> Use Dapr Appi ID (Option 1)
            //var daprHttpClient = DaprClient.CreateInvokeHttpClient(appId: "tasksmanager-backend-api"); 
            //TasksList = await daprHttpClient.GetFromJsonAsync<List<TaskModel>>($"api/tasks?createdBy={TasksCreatedBy}");

            // Invoke via DaprSDK (Invoke HTTP services using HttpClient) --> Specify Custom Port (Option 2)
            //var daprHttpClient = DaprClient.CreateInvokeHttpClient(daprEndpoint: "http://localhost:3500"); 
            //TasksList = await daprHttpClient.GetFromJsonAsync<List<TaskModel>>($"http://tasksmanager-backend-api/api/tasks?createdBy={TasksCreatedBy}");

            // Invoke via DaprSDK (Invoke HTTP services using DaprClient)
            TasksList = await _daprClient.InvokeMethodAsync<List<TaskModel>>(HttpMethod.Get, "tasksmanager-backend-api", $"api/tasks?createdBy={TasksCreatedBy}");
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            // Dapr SideCar Invocation
            await _daprClient.InvokeMethodAsync(HttpMethod.Delete, "tasksmanager-backend-api", $"api/tasks/{id}");

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCompleteAsync(Guid id)
        {
            // Dapr SideCar Invocation
            await _daprClient.InvokeMethodAsync(HttpMethod.Put, "tasksmanager-backend-api", $"api/tasks/{id}/markcomplete");

            return RedirectToPage();
        }
    }
}
