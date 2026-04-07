namespace Backend.Services.Interfaces;

using Backend.Models.DTOs;

public interface ICommentService
{
    Task<CommentDto> AddCommentAsync(Guid taskId, Guid userId, CreateCommentDto dto);
    Task<List<CommentDto>> GetTaskCommentsAsync(Guid taskId);
    Task<CommentDto> UpdateCommentAsync(Guid commentId, Guid userId, UpdateCommentDto dto);
    Task DeleteCommentAsync(Guid commentId, Guid userId);
}
