using Dapr.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TasksTracker.TasksManager.Backend.Api.Services;

namespace TasksTracker.TasksManager.Backend.Api.Controllers
{
    [Route("api/health")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;
        private readonly ITasksManager _tasksManager;
        public HealthController(ILogger<HealthController> logger, ITasksManager tasksManager)
        {
            _logger = logger;
            _tasksManager = tasksManager;
          
        }

        [HttpGet("Readiness")]
        public async Task<IActionResult> Readiness()
        {
            try
            {
                string? _instanceId = Environment.GetEnvironmentVariable("HOSTNAME");

                _logger.LogInformation("Readiness::Invoked readiness ednpoint by instance {0}", _instanceId);

                _logger.LogInformation("Readiness::Testing state store availability by creating new dummy task by instance {0}", _instanceId);
                var taskId = await _tasksManager.CreateNewTask("Health Readiness Task",
                                         "Readiness Prob",
                                         "temp@mail.com",
                                         DateTime.UtcNow.AddDays(1));

                _logger.LogInformation("Readiness::Deleting dummy task from state store by instance {0}. Deleted Task Id {1}", _instanceId, taskId);
                await _tasksManager.DeleteTask(taskId);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Exception while invoking Reasiness prob");
                return new StatusCodeResult(StatusCodes.Status503ServiceUnavailable);
            }

            return Ok();

        }
    }
}
