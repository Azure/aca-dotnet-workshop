 using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

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