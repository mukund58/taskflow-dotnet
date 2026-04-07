namespace Backend.Services.Interfaces;

using Backend.Models.DTOs;

public interface ILabelService
{
    Task<LabelDto> CreateLabelAsync(CreateLabelDto labelDto, Guid projectId);
    Task<List<LabelDto>> GetLabelsByProjectAsync(Guid projectId);
    Task<LabelDto> GetLabelByIdAsync(Guid labelId);
    Task<LabelDto> UpdateLabelAsync(Guid labelId, UpdateLabelDto labelDto);
    Task DeleteLabelAsync(Guid labelId);
    Task AddLabelToTaskAsync(Guid taskId, Guid labelId);
    Task RemoveLabelFromTaskAsync(Guid taskId, Guid labelId);
    Task<List<LabelDto>> GetTaskLabelsAsync(Guid taskId);
}
