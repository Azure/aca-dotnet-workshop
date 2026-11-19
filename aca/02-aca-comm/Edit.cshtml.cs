using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TasksTracker.WebPortal.Frontend.Ui.Pages.Tasks.Models;

namespace TasksTracker.WebPortal.Frontend.Ui.Pages.Tasks
{
    public class EditModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        [BindProperty]
        public TaskUpdateModel? TaskUpdate { get; set; }
        public string? TasksCreatedBy { get; set; }

        public EditModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            TasksCreatedBy = Request.Cookies["TasksCreatedByCookie"];

            if (String.IsNullOrEmpty(TasksCreatedBy)) {
                return RedirectToPage("../Index");
            }

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
