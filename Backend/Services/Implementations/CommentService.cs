namespace Backend.Services.Implementations;

using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Backend.Models.DTOs;
using Backend.Models.Entities;
using Backend.Data;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

public class CommentService : ICommentService
{
    private static readonly Regex MentionRegex = new(
        @"(?<!\w)@([A-Za-z0-9._-]{2,64})",
        RegexOptions.Compiled);

    private readonly AppDbContext _context;
    private readonly INotificationService? _notificationService;
    private readonly IEmailService? _emailService;
    private readonly IConfiguration? _configuration;

    public CommentService(
        AppDbContext context,
        INotificationService? notificationService = null,
        IEmailService? emailService = null,
        IConfiguration? configuration = null)
    {
        _context = context;
        _notificationService = notificationService;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task<CommentDto> AddCommentAsync(Guid taskId, Guid userId, CreateCommentDto dto)
    {
        // Verify task exists (including soft-deleted tasks for this feature)
        var task = await _context.Tasks
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
            throw new KeyNotFoundException("Task not found");

        // Verify task is not deleted before allowing comments
        if (task.IsDeleted)
            throw new UnauthorizedAccessException("Cannot add comments to deleted tasks");

        // Verify user is either the task creator/assignee or has elevated access
        // For now, we allow any authenticated user with task access to comment
        // You can extend this with more granular permissions

        var comment = new TaskComment
        {
            TaskId = taskId,
            AuthorId = userId,
            Content = dto.Content,
            CreatedAt = DateTime.UtcNow
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        await NotifyMentionedUsersAsync(task, comment, userId);

        return await MapToCommentDtoAsync(comment);
    }

    public async Task<List<CommentDto>> GetTaskCommentsAsync(Guid taskId)
    {
        // Verify task exists (including soft-deleted tasks for this feature)
        var task = await _context.Tasks
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
            throw new KeyNotFoundException("Task not found");

        // If task is deleted, we still show comments per requirements
        var comments = await _context.Comments
            .Include(c => c.Author)
            .AsNoTracking()
            .IgnoreQueryFilters() // Show comments even on deleted tasks
            .Where(c => c.TaskId == taskId && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var result = new List<CommentDto>();
        foreach (var c in comments)
        {
            result.Add(await MapToCommentDtoAsync(c));
        }
        return result;
    }

    public async Task<CommentDto> UpdateCommentAsync(Guid taskId, Guid commentId, Guid userId, UpdateCommentDto dto)
    {
        var comment = await GetCommentByIdOrThrowAsync(taskId, commentId);

        // Only author can edit their comment
        if (comment.AuthorId != userId)
            throw new UnauthorizedAccessException("You can only edit your own comments");

        comment.Content = dto.Content;
        comment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await MapToCommentDtoAsync(comment);
    }

    public async Task DeleteCommentAsync(Guid taskId, Guid commentId, Guid userId)
    {
        var comment = await GetCommentByIdOrThrowAsync(taskId, commentId);

        // Only author can delete their comment
        if (comment.AuthorId != userId)
            throw new UnauthorizedAccessException("You can only delete your own comments");

        comment.IsDeleted = true;
        await _context.SaveChangesAsync();
    }

    private async Task<TaskComment> GetCommentByIdOrThrowAsync(Guid taskId, Guid commentId)
    {
        var comment = await _context.Comments
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.TaskId == taskId && c.Id == commentId && !c.IsDeleted);

        if (comment == null)
            throw new KeyNotFoundException("Comment not found");

        return comment;
    }

    private async Task<CommentDto> MapToCommentDtoAsync(TaskComment comment)
    {
        var author = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == comment.AuthorId && !u.IsDeleted);

        return new CommentDto
        {
            Id = comment.Id,
            Content = comment.Content,
            TaskId = comment.TaskId,
            AuthorId = comment.AuthorId,
            AuthorName = author?.Name ?? "Unknown",
            AuthorEmail = author?.Email ?? "unknown@example.com",
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt
        };
    }

    private async Task NotifyMentionedUsersAsync(TaskItem task, TaskComment comment, Guid authorId)
    {
        var mentionHandles = ExtractMentionHandles(comment.Content);
        if (mentionHandles.Count == 0)
            return;

        var candidates = await GetMentionCandidatesAsync(task);
        if (candidates.Count == 0)
            return;

        var mentionedUsers = ResolveMentionedUsers(candidates, mentionHandles, authorId);
        if (mentionedUsers.Count == 0)
            return;

        var author = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == authorId && !u.IsDeleted);

        var authorName = author?.Name ?? "A teammate";
        var message = $"{authorName} mentioned you in task '{task.Title}'.";

        foreach (var mentionedUser in mentionedUsers)
        {
            if (_notificationService != null && mentionedUser.PushNotificationsEnabled)
            {
                try
                {
                    await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                    {
                        UserId = mentionedUser.Id,
                        Message = message,
                        Type = "MentionedInComment",
                        TaskId = task.Id,
                        CommentId = comment.Id
                    });
                }
                catch
                {
                    // Mention notification delivery must not fail comment creation.
                }
            }

            if (_emailService != null && mentionedUser.EmailNotificationsEnabled && !string.IsNullOrWhiteSpace(mentionedUser.Email))
            {
                try
                {
                    await _emailService.SendTaskMentionEmail(
                        mentionedUser.Email,
                        mentionedUser.Name,
                        authorName,
                        task.Title,
                        comment.Content);
                }
                catch
                {
                    // Email delivery is best effort and should not fail the request.
                }
            }
        }

        await SendWebhookNotificationsAsync(
            task,
            comment.Content,
            authorName,
            mentionedUsers.Select(user => user.Name).ToList());
    }

    private async Task<List<User>> GetMentionCandidatesAsync(TaskItem task)
    {
        var memberUserIds = await _context.ProjectMembers
            .AsNoTracking()
            .Where(pm => pm.ProjectId == task.ProjectId)
            .Select(pm => pm.UserId)
            .Distinct()
            .ToListAsync();

        if (task.AssignedUserId.HasValue && !memberUserIds.Contains(task.AssignedUserId.Value))
            memberUserIds.Add(task.AssignedUserId.Value);

        if (memberUserIds.Count == 0)
            return [];

        return await _context.Users
            .AsNoTracking()
            .Where(user => !user.IsDeleted && memberUserIds.Contains(user.Id))
            .OrderBy(user => user.Name)
            .ToListAsync();
    }

    private static List<User> ResolveMentionedUsers(List<User> candidates, HashSet<string> mentionHandles, Guid authorId)
    {
        var mentionedUsersById = new Dictionary<Guid, User>();

        foreach (var handle in mentionHandles)
        {
            var normalizedHandle = NormalizeHandle(handle);
            if (string.IsNullOrWhiteSpace(normalizedHandle))
                continue;

            var matchedUser = candidates.FirstOrDefault(user =>
                string.Equals(GetEmailLocalPart(user.Email), handle, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(NormalizeHandle(user.Name), normalizedHandle, StringComparison.OrdinalIgnoreCase));

            if (matchedUser == null || matchedUser.Id == authorId)
                continue;

            mentionedUsersById.TryAdd(matchedUser.Id, matchedUser);
        }

        return mentionedUsersById.Values.ToList();
    }

    private static HashSet<string> ExtractMentionHandles(string? content)
    {
        var handles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(content))
            return handles;

        foreach (Match match in MentionRegex.Matches(content))
        {
            var value = match.Groups[1].Value.Trim();
            if (value.Length >= 2)
                handles.Add(value);
        }

        return handles;
    }

    private static string GetEmailLocalPart(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return string.Empty;

        var atIndex = email.IndexOf('@');
        if (atIndex <= 0)
            return string.Empty;

        return email[..atIndex];
    }

    private static string NormalizeHandle(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return new string(value
            .Trim()
            .ToLowerInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());
    }

    private async Task SendWebhookNotificationsAsync(
        TaskItem task,
        string commentContent,
        string authorName,
        IReadOnlyCollection<string> mentionedUserNames)
    {
        var slackWebhookUrl = Environment.GetEnvironmentVariable("SLACK_WEBHOOK_URL")
            ?? _configuration?["Notifications:SlackWebhookUrl"];

        var teamsWebhookUrl = Environment.GetEnvironmentVariable("TEAMS_WEBHOOK_URL")
            ?? _configuration?["Notifications:TeamsWebhookUrl"];

        if (string.IsNullOrWhiteSpace(slackWebhookUrl) && string.IsNullOrWhiteSpace(teamsWebhookUrl))
            return;

        var mentionList = string.Join(", ", mentionedUserNames);
        var preview = BuildCommentPreview(commentContent);
        var message = $"{authorName} mentioned {mentionList} on task '{task.Title}'. Comment: \"{preview}\"";

        using var httpClient = new HttpClient();
        var sendTasks = new List<Task>();

        if (!string.IsNullOrWhiteSpace(slackWebhookUrl))
        {
            sendTasks.Add(PostWebhookSafelyAsync(httpClient, slackWebhookUrl, new
            {
                text = message
            }));
        }

        if (!string.IsNullOrWhiteSpace(teamsWebhookUrl))
        {
            var teamsPayload = new Dictionary<string, object>
            {
                ["@type"] = "MessageCard",
                ["@context"] = "https://schema.org/extensions",
                ["summary"] = "Task mention notification",
                ["title"] = "Task mention",
                ["text"] = message
            };

            sendTasks.Add(PostWebhookSafelyAsync(httpClient, teamsWebhookUrl, teamsPayload));
        }

        await Task.WhenAll(sendTasks);
    }

    private static async Task PostWebhookSafelyAsync(HttpClient httpClient, string webhookUrl, object payload)
    {
        try
        {
            await httpClient.PostAsJsonAsync(webhookUrl, payload);
        }
        catch
        {
            // External webhook delivery is best effort.
        }
    }

    private static string BuildCommentPreview(string? commentContent)
    {
        if (string.IsNullOrWhiteSpace(commentContent))
            return "(empty comment)";

        var collapsed = commentContent.ReplaceLineEndings(" ").Trim();
        return collapsed.Length <= 220 ? collapsed : collapsed[..220] + "...";
    }
}
