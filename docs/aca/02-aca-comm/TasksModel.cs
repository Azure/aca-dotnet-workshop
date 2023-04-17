using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace TasksTracker.WebPortal.Frontend.Ui.Pages.Tasks.Models
{
    public class TaskModel
    {
        public Guid TaskId { get; set; }
        public string TaskName { get; set; } = string.Empty;
        public string TaskCreatedBy { get; set; } = string.Empty;
        public DateTime TaskCreatedOn { get; set; }
        public DateTime TaskDueDate { get; set; }
        public string TaskAssignedTo { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public bool IsOverDue { get; set; }
    }

    public class TaskAddModel
    {
        [Display(Name = "Task Name")]
        [Required]
        public string TaskName { get; set; } = string.Empty;

        [Display(Name = "Task DueDate")]
        [Required]
        public DateTime TaskDueDate { get; set; }

        [Display(Name = "Assigned To")]
        [Required]
        public string TaskAssignedTo { get; set; } = string.Empty;
        public string TaskCreatedBy { get; set; } = string.Empty;
    }

    public class TaskUpdateModel
    {
        public Guid TaskId { get; set; }

        [Display(Name = "Task Name")]
        [Required]
        public string TaskName { get; set; } = string.Empty;

        [Display(Name = "Task DueDate")]
        [Required]
        public DateTime TaskDueDate { get; set; }

        [Display(Name = "Assigned To")]
        [Required]
        public string TaskAssignedTo { get; set; } = string.Empty;
    }
}