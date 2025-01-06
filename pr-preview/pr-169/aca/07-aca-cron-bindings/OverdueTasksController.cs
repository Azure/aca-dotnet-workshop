using Microsoft.AspNetCore.Mvc;
using TasksTracker.TasksManager.Backend.Api.Models;
using TasksTracker.TasksManager.Backend.Api.Services;

namespace TasksTracker.TasksManager.Backend.Api.Controllers
{
    [Route("api/overduetasks")]
    [ApiController]
    public class OverdueTasksController : ControllerBase
    {
        private readonly ILogger<TasksController> _logger;
        private readonly ITasksManager _tasksManager;

        public OverdueTasksController(ILogger<TasksController> logger, ITasksManager tasksManager)
        {
            _logger = logger;
            _tasksManager = tasksManager;
        }

        [HttpGet]
        public async Task<IEnumerable<TaskModel>> Get()
        {
            return await _tasksManager.GetYesterdaysDueTasks();
        }

        [HttpPost("markoverdue")]
        public async Task<IActionResult> Post([FromBody] List<TaskModel> overdueTasksList)
        {
            await _tasksManager.MarkOverdueTasks(overdueTasksList);

            return Ok();
        }
    }
}