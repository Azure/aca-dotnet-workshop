using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TasksTracker.WebPortal.Frontend.Ui.Pages.Tasks.Models;

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