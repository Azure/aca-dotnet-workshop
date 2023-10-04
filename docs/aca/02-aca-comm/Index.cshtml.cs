using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TasksTracker.WebPortal.Frontend.Ui.Pages.Tasks.Models;

namespace TasksTracker.WebPortal.Frontend.Ui.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        [BindProperty]
        public string? TasksCreatedBy { get; set; }

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