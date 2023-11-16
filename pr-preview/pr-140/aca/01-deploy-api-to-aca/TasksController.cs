using Microsoft.AspNetCore.Mvc;
using TasksTracker.TasksManager.Backend.Api.Models;
using TasksTracker.TasksManager.Backend.Api.Services;

namespace TasksTracker.TasksManager.Backend.Api.Controllers
{
    [Route("api/tasks")]
    [ApiController]
    public class TasksController : ControllerBase
    {
        private readonly ILogger<TasksController> _logger;
        private readonly ITasksManager _tasksManager;

        public TasksController(ILogger<TasksController> logger, ITasksManager tasksManager)
        {
            _logger = logger;
            _tasksManager = tasksManager;
        }

        [HttpGet]
        public async Task<IEnumerable<TaskModel>> Get(string createdBy)
        {
            return await _tasksManager.GetTasksByCreator(createdBy);
        }

        [HttpGet("{taskId}")]
        public async Task<IActionResult> GetTask(Guid taskId)
        {
            var task = await _tasksManager.GetTaskById(taskId);

            return (task != null) ? Ok(task) : NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] TaskAddModel taskAddModel)
        {
            var taskId = await _tasksManager.CreateNewTask(
                taskAddModel.TaskName,
                taskAddModel.TaskCreatedBy,
                taskAddModel.TaskAssignedTo,
                taskAddModel.TaskDueDate
            );

            return Created($"/api/tasks/{taskId}", null);

        }

        [HttpPut("{taskId}")]
        public async Task<IActionResult> Put(Guid taskId, [FromBody] TaskUpdateModel taskUpdateModel)
        {
            var updated = await _tasksManager.UpdateTask(
                taskId,
                taskUpdateModel.TaskName,
                taskUpdateModel.TaskAssignedTo,
                taskUpdateModel.TaskDueDate
            );

            return updated ? Ok() : BadRequest();
        }

        [HttpPut("{taskId}/markcomplete")]
        public async Task<IActionResult> MarkComplete(Guid taskId)
        {
            var updated = await _tasksManager.MarkTaskCompleted(taskId);

            return updated ? Ok() : BadRequest();
        }

        [HttpDelete("{taskId}")]
        public async Task<IActionResult> Delete(Guid taskId)
        {
            var deleted = await _tasksManager.DeleteTask(taskId);

            return deleted ? Ok() : NotFound();
        }
    }
}
