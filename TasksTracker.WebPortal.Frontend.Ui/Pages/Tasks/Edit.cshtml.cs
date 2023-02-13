using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TasksTracker.WebPortal.Frontend.Ui.Pages.Tasks.Models;

namespace TasksTracker.WebPortal.Frontend.Ui.Pages.Tasks
{
    public class EditModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly DaprClient _daprClient;

        [BindProperty]
        public TaskUpdateModel? TaskUpdate { get; set; }

        public EditModel(IHttpClientFactory httpClientFactory, DaprClient daprClient)
        {
            _httpClientFactory = httpClientFactory;
            _daprClient = daprClient;
        }

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // direct svc to svc http request
            // var httpClient = _httpClientFactory.CreateClient("BackEndApiExternal");
            // var Task = await httpClient.GetFromJsonAsync<TaskModel>($"api/tasks/{id}");

            //Dapr SideCar Invocation
            var Task = await _daprClient.InvokeMethodAsync<TaskModel>(HttpMethod.Get, "tasksmanager-backend-api", $"api/tasks/{id}");

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
                // var httpClient = _httpClientFactory.CreateClient("BackEndApiExternal");
                // var result = await httpClient.PutAsJsonAsync($"api/tasks/{TaskUpdate.TaskId}", TaskUpdate);

                //Dapr SideCar Invocation
                await _daprClient.InvokeMethodAsync<TaskUpdateModel>(HttpMethod.Put, "tasksmanager-backend-api", $"api/tasks/{TaskUpdate.TaskId}", TaskUpdate);

            }

            return RedirectToPage("./Index");
        }
    }
}
