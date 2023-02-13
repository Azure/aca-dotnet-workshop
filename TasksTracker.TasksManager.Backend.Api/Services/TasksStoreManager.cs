using System.Text.Json.Nodes;
using Dapr.Client;
using Microsoft.Azure.Cosmos;
using TasksTracker.TasksManager.Backend.Api.Models;

namespace TasksTracker.TasksManager.Backend.Api.Services
{
    public class TasksStoreManager : ITasksManager
    {
        private static string STORE_NAME = "statestore";
        // private static string PUBSUB_NAME = "taskspubsub";
        private static string PUBSUB_SVCBUS_NAME = "dapr-pubsub-servicebus";
        private static string TASK_SAVED_TOPICNAME = "tasksavedtopic";
        private static string databaseName = "tasksmanagerdb";
        private static string containerName = "taskscollection";
        private readonly DaprClient _daprClient;
        private readonly IConfiguration _config;
        private readonly ILogger<TasksStoreManager> _logger;

        public TasksStoreManager(DaprClient daprClient, IConfiguration config, ILogger<TasksStoreManager> logger)
        {
            _daprClient = daprClient;
            _config = config;
            _logger = logger;
        }
        public async Task<Guid> CreateNewTask(string taskName, string createdBy, string assignedTo, DateTime dueDate)
        {
            var taskModel = new TaskModel()
            {
                TaskId = Guid.NewGuid(),
                TaskName = taskName,
                TaskCreatedBy = createdBy,
                TaskCreatedOn = DateTime.UtcNow,
                TaskDueDate = dueDate,
                TaskAssignedTo = assignedTo,
            };

            _logger.LogInformation("Save a new task with name: '{0}' to state store", taskModel.TaskName);

            await _daprClient.SaveStateAsync<TaskModel>(STORE_NAME, taskModel.TaskId.ToString(), taskModel);

            await PublishTaskSavedEvent(taskModel);

            return taskModel.TaskId;
        }

        public async Task<bool> DeleteTask(Guid taskId)
        {
            _logger.LogInformation("Delete task with Id: '{0}'", taskId);

            await _daprClient.DeleteStateAsync(STORE_NAME, taskId.ToString());
            return true;
        }

        public async Task<TaskModel?> GetTaskById(Guid taskId)
        {
            _logger.LogInformation("Getting task with Id: '{0}'", taskId);

            var taskModel = await _daprClient.GetStateAsync<TaskModel>(STORE_NAME, taskId.ToString());

            return taskModel;
        }

        public async Task<List<TaskModel>> GetTasksByCreator(string createdBy)
        {
            //Currently, the query API for Cosmos DB is not working when deploying it to Azure Container Apps, this is an open
            //issue and prodcut team is wokring on it. Details of the issue is here: https://github.com/microsoft/azure-container-apps/issues/155
            //Due to this issue, we will query directly the cosmos db to list tasks per created by user.

            _logger.LogInformation("Query tasks created by: '{0}'", createdBy);

            var query = "{" +
                   "\"filter\": {" +
                       "\"EQ\": { \"taskCreatedBy\": \"" + createdBy + "\" }" +
                   "}}";

            var queryResponse = await _daprClient.QueryStateAsync<TaskModel>(STORE_NAME, query);

            var tasksList = queryResponse.Results.Select(q => q.Data).OrderByDescending(o=>o.TaskCreatedOn);

            return tasksList.ToList();

            //Workaround: Query cosmos DB directly

            // var result = await QueryCosmosDb(createdBy);

            // return result;

        }

        private async Task<List<TaskModel>> QueryCosmosDb(string createdBy)
        {

            var cosmosKey = _config.GetValue<string>("cosmosDb:key");
            var account = _config.GetValue<string>("cosmosDb:accountUrl");
            var cosmosClient = new CosmosClient(account, cosmosKey);
            var container = cosmosClient.GetContainer(databaseName, containerName);

            var queryString = $"SELECT * FROM C['value'] as tasksList Where tasksList.taskCreatedBy = @taskCreatedBy";
            var queryDefinition = new QueryDefinition(queryString).WithParameter("@taskCreatedBy", createdBy);

            using FeedIterator<TaskModel> feed = container.GetItemQueryIterator<TaskModel>(queryDefinition: queryDefinition);

            var results = new List<TaskModel>();

            while (feed.HasMoreResults)
            {
                FeedResponse<TaskModel> response = await feed.ReadNextAsync();

                results.AddRange(response.OrderByDescending(o => o.TaskCreatedOn).ToList());

            }

            return results;
        }


        public async Task<List<TaskModel>> GetTasksByTime(DateTime waterMark)
        {

            var cosmosKey = _config.GetValue<string>("cosmosDb:key");
            var account = _config.GetValue<string>("cosmosDb:accountUrl");
            var cosmosClient = new CosmosClient(account, cosmosKey);
            var container = cosmosClient.GetContainer(databaseName, containerName);

            var queryString = $"SELECT * FROM C['value'] as tasksList Where tasksList.taskCreatedOn > @taskCreatedOn";
            var queryDefinition = new QueryDefinition(queryString).WithParameter("@taskCreatedOn", waterMark);

            using FeedIterator<TaskModel> feed = container.GetItemQueryIterator<TaskModel>(queryDefinition: queryDefinition);

            var results = new List<TaskModel>();

            while (feed.HasMoreResults)
            {
                FeedResponse<TaskModel> response = await feed.ReadNextAsync();

                results.AddRange(response.ToList());

            }

            return results;
        }

        public async Task<bool> MarkTaskCompleted(Guid taskId)
        {
            _logger.LogInformation("Mark task with Id: '{0}' as completed", taskId);

            var taskModel = await _daprClient.GetStateAsync<TaskModel>(STORE_NAME, taskId.ToString());

            if (taskModel != null)
            {
                taskModel.IsCompleted = true;
                await _daprClient.SaveStateAsync<TaskModel>(STORE_NAME, taskModel.TaskId.ToString(), taskModel);
                return true;
            }

            return false;
        }

        public async Task MarkOverdueTasks(List<TaskModel> overDueTasksList)
        {
            foreach (var taskModel in overDueTasksList)
            {
                _logger.LogInformation("Mark task with Id: '{0}' as OverDue task", taskModel.TaskId);
                taskModel.IsOverDue = true;
                await _daprClient.SaveStateAsync<TaskModel>(STORE_NAME, taskModel.TaskId.ToString(), taskModel);
            }
           
        }

        public async Task<bool> UpdateTask(Guid taskId, string taskName, string assignedTo, DateTime dueDate)
        {
            _logger.LogInformation("Update task with Id: '{0}'", taskId);

            var taskModel = await _daprClient.GetStateAsync<TaskModel>(STORE_NAME, taskId.ToString());

            var currentAssignee = taskModel.TaskAssignedTo;

            if (taskModel != null)
            {
                taskModel.TaskName = taskName;
                taskModel.TaskAssignedTo = assignedTo;
                taskModel.TaskDueDate = dueDate;

                await _daprClient.SaveStateAsync<TaskModel>(STORE_NAME, taskModel.TaskId.ToString(), taskModel);

                if (!taskModel.TaskAssignedTo.Equals(currentAssignee, StringComparison.OrdinalIgnoreCase))
                {
                    await PublishTaskSavedEvent(taskModel);
                }

                return true;
            }

            return false;
        }

        private async Task PublishTaskSavedEvent(TaskModel taskModel)
        {
            _logger.LogInformation("Publish Task Saved event for task with Id: '{0}' and Name: '{1}' for Assigne: '{2}'",
                                                                taskModel.TaskId, taskModel.TaskName, taskModel.TaskAssignedTo);

            await _daprClient.PublishEventAsync(PUBSUB_SVCBUS_NAME, TASK_SAVED_TOPICNAME, taskModel);
        }
    }
}
