namespace Backend.Services.Implementations;

using Backend.Models.DTOs;
using Backend.Models.Entities;
using Backend.Data;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

public class CommentService : ICommentService
{
    private readonly AppDbContext _context;

    public CommentService(AppDbContext context)
    {
        _context = context;
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

    public async Task<CommentDto> UpdateCommentAsync(Guid commentId, Guid userId, UpdateCommentDto dto)
    {
        var comment = await GetCommentByIdOrThrowAsync(commentId);

        // Only author can edit their comment
        if (comment.AuthorId != userId)
            throw new UnauthorizedAccessException("You can only edit your own comments");

        comment.Content = dto.Content;
        comment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await MapToCommentDtoAsync(comment);
    }

    public async Task DeleteCommentAsync(Guid commentId, Guid userId)
    {
        var comment = await GetCommentByIdOrThrowAsync(commentId);

        // Only author can delete their comment
        if (comment.AuthorId != userId)
            throw new UnauthorizedAccessException("You can only delete your own comments");

        comment.IsDeleted = true;
        await _context.SaveChangesAsync();
    }

    private async Task<TaskComment> GetCommentByIdOrThrowAsync(Guid commentId)
    {
        var comment = await _context.Comments
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted);

        if (comment == null)
            throw new KeyNotFoundException("Comment not found");

        return comment;
    }

    private async Task<CommentDto> MapToCommentDtoAsync(TaskComment comment)
    {
        var author = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == comment.AuthorId);

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
}
