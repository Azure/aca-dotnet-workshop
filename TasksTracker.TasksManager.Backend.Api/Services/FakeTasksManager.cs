using TasksTracker.TasksManager.Backend.Api.Models;

namespace TasksTracker.TasksManager.Backend.Api.Services
{
    public class FakeTasksManager : ITasksManager
    {
        private List<TaskModel> _tasksList = new List<TaskModel>();
        Random rnd = new Random();

        private void GenerateRandomTasks()
        {
            for (int i = 0; i < 10; i++)
            {
                var task = new TaskModel()
                {
                    TaskId = Guid.NewGuid(),
                    TaskName = $"Task number: {i}",
                    TaskCreatedBy = "tjoudeh@bitoftech.net",
                    TaskCreatedOn = DateTime.UtcNow.AddMinutes(i),
                    TaskDueDate = DateTime.UtcNow.AddDays(i),
                    TaskAssignedTo = $"assignee{rnd.Next(50)}@mail.com",
                };

                _tasksList.Add(task);
            }
        }

        public FakeTasksManager()
        {
            GenerateRandomTasks();
        }

        public Task<Guid> CreateNewTask(string taskName, string createdBy, string assignedTo, DateTime dueDate)
        {
            var task = new TaskModel()
            {
                TaskId = Guid.NewGuid(),
                TaskName = taskName,
                TaskCreatedBy = createdBy,
                TaskCreatedOn = DateTime.UtcNow,
                TaskDueDate = dueDate,
                TaskAssignedTo = assignedTo,
            };

            _tasksList.Add(task);
            return Task.FromResult(task.TaskId);
        }

        public Task<bool> DeleteTask(Guid taskId)
        {
            var task = _tasksList.FirstOrDefault(t => t.TaskId.Equals(taskId));

            if (task != null)
            {
                _tasksList.Remove(task);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public Task<TaskModel?> GetTaskById(Guid taskId)
        {
            var taskModel = _tasksList.FirstOrDefault(t => t.TaskId.Equals(taskId));

            return Task.FromResult(taskModel);
        }

        public Task<List<TaskModel>> GetTasksByCreator(string createdBy)
        {
            var tasksList = _tasksList.Where(t => t.TaskCreatedBy.Equals(createdBy)).OrderByDescending(o => o.TaskCreatedOn).ToList();

            return Task.FromResult(tasksList);
        }

        public Task<bool> MarkTaskCompleted(Guid taskId)
        {
            var task = _tasksList.FirstOrDefault(t => t.TaskId.Equals(taskId));

            if (task != null)
            {
                task.IsCompleted = true;
                 return Task.FromResult(true);
            }

             return Task.FromResult(false);
        }

        public Task<bool> UpdateTask(Guid taskId, string taskName, string assignedTo, DateTime dueDate)
        {
            var task = _tasksList.FirstOrDefault(t => t.TaskId.Equals(taskId));

            if (task != null)
            {
                task.TaskName = taskName;
                task.TaskAssignedTo = assignedTo;
                task.TaskDueDate = dueDate;
                 return Task.FromResult(true);
            }

             return Task.FromResult(false);
        }

        public Task MarkOverdueTasks(List<TaskModel> overDueTasksList)
        {
            throw new NotImplementedException();
        }

        public Task<List<TaskModel>> GetYesterdaysDueTasks()
        {
            var tasksList = _tasksList.Where(t => t.TaskDueDate.Equals(DateTime.Today.AddDays(-1))).ToList();

            return Task.FromResult(tasksList);
        }
    }
}
