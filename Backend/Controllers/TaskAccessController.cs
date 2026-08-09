namespace Backend.Controllers;

using Backend.Services.Interfaces;

/// <summary>
/// Base controller for task-scoped endpoints that need read/write access checks.
/// Provides EnsureTaskReadAccessAsync and EnsureTaskWriteAccessAsync, eliminating
/// their duplication across TaskController, CommentController, ChecklistController,
/// and TaskAttachmentController.
/// </summary>
public abstract class TaskAccessController : BaseApiController
{
    protected readonly ITaskService TaskService;
    protected readonly IProjectService ProjectService;

    protected TaskAccessController(ITaskService taskService, IProjectService projectService)
    {
        TaskService = taskService;
        ProjectService = projectService;
    }

    /// <summary>
    /// Ensures the current user has read access to the task's project.
    /// Admins and Managers bypass the check.
    /// </summary>
    protected async Task<Backend.Models.Entities.TaskItem> EnsureTaskReadAccessAsync(Guid taskId)
    {
        var task = await TaskService.GetById(taskId);

        if (HasElevatedAccess(includeManager: false))
            return task;

        var currentUserId = GetCurrentUserId();
        var canRead = await ProjectService.HasReadAccess(task.ProjectId, currentUserId, elevatedAccess: false);

        if (!canRead)
            throw new UnauthorizedAccessException("You do not have read access to this task");

        return task;
    }

    /// <summary>
    /// Ensures the current user has write access to the task's project.
    /// Admins and Managers bypass the check.
    /// </summary>
    protected async Task<Backend.Models.Entities.TaskItem> EnsureTaskWriteAccessAsync(Guid taskId)
    {
        var task = await TaskService.GetById(taskId);

        if (HasElevatedAccess(includeManager: false))
            return task;

        var currentUserId = GetCurrentUserId();
        var canWrite = await ProjectService.HasWriteAccess(task.ProjectId, currentUserId, elevatedAccess: false);

        if (!canWrite)
            throw new UnauthorizedAccessException("You do not have write access to this task");

        return task;
    }
}
