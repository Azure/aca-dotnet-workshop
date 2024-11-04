using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using TasksTracker.Processor.Backend.Svc.Models;

namespace TasksTracker.Processor.Backend.Svc.Controllers
{
    [Route("ScheduledTasksManager")]
    [ApiController]
    public class ScheduledTasksManagerController : ControllerBase
    {
        private readonly ILogger<ScheduledTasksManagerController> _logger;
        private readonly DaprClient _daprClient;
        public ScheduledTasksManagerController(ILogger<ScheduledTasksManagerController> logger, DaprClient daprClient)
        {
            _logger = logger;
            _daprClient = daprClient;
        }

        [HttpPost]
        public async Task CheckOverDueTasksJob()
        {
            var runAt = DateTime.UtcNow;

            _logger.LogInformation($"ScheduledTasksManager::Timer Services triggered at: {runAt}");

            var overdueTasksList = new List<TaskModel>();

            var tasksList = await _daprClient.InvokeMethodAsync<List<TaskModel>>(HttpMethod.Get, "tasksmanager-backend-api", $"api/overduetasks");

            _logger.LogInformation($"ScheduledTasksManager::completed query state store for tasks, retrieved tasks count: {tasksList?.Count()}");

            tasksList?.ForEach(taskModel =>
            {
                if (runAt.Date> taskModel.TaskDueDate.Date)
                {
                    overdueTasksList.Add(taskModel);
                }
            });

            if (overdueTasksList.Count> 0)
            {
                _logger.LogInformation($"ScheduledTasksManager::marking {overdueTasksList.Count()} as overdue tasks");

                await _daprClient.InvokeMethodAsync(HttpMethod.Post, "tasksmanager-backend-api", $"api/overduetasks/markoverdue", overdueTasksList);
            }
        }
    }
}