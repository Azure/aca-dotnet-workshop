using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TasksTracker.WebPortal.Frontend.Ui.Pages.Tasks.Models;

namespace TasksTracker.WebPortal.Frontend.Ui.Pages.Tasks
{
    public class CreateModel : PageModel
    {

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly DaprClient _daprClient;

        public CreateModel(IHttpClientFactory httpClientFactory, DaprClient daprClient)
        {
            _httpClientFactory = httpClientFactory;
            _daprClient = daprClient;
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
                // var httpClient = _httpClientFactory.CreateClient("BackEndApiExternal");
                // var result = await httpClient.PostAsJsonAsync("api/tasks/", TaskAdd);

                //Dapr SideCar Invocation
                await _daprClient.InvokeMethodAsync(HttpMethod.Post, "tasksmanager-backend-api", $"api/tasks", TaskAdd);

            }
            return RedirectToPage("./Index");
        }
    }
}
