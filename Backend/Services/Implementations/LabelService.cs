namespace Backend.Services.Implementations;

using Backend.Models.DTOs;
using Backend.Models.Entities;
using Backend.Data;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

public class LabelService : ILabelService
{
    private readonly AppDbContext _context;

    public LabelService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<LabelDto> CreateLabelAsync(CreateLabelDto labelDto, Guid projectId)
    {
        // Verify project exists
        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
        if (project == null)
            throw new KeyNotFoundException("Project not found");

        var label = new Label
        {
            Id = Guid.NewGuid(),
            Name = labelDto.Name,
            Color = labelDto.Color,
            Description = labelDto.Description,
            ProjectId = projectId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Labels.Add(label);
        await _context.SaveChangesAsync();

        return MapToLabelDto(label);
    }

    public async Task<List<LabelDto>> GetLabelsByProjectAsync(Guid projectId)
    {
        return await _context.Labels
            .Where(l => l.ProjectId == projectId)
            .Select(l => MapToLabelDto(l))
            .ToListAsync();
    }

    public async Task<LabelDto> GetLabelByIdAsync(Guid labelId)
    {
        var label = await _context.Labels.FirstOrDefaultAsync(l => l.Id == labelId);
        if (label == null)
            throw new KeyNotFoundException("Label not found");

        return MapToLabelDto(label);
    }

    public async Task<LabelDto> UpdateLabelAsync(Guid labelId, UpdateLabelDto labelDto)
    {
        var label = await _context.Labels.FirstOrDefaultAsync(l => l.Id == labelId);
        if (label == null)
            throw new KeyNotFoundException("Label not found");

        label.Name = labelDto.Name;
        label.Color = labelDto.Color;
        label.Description = labelDto.Description;

        _context.Labels.Update(label);
        await _context.SaveChangesAsync();

        return MapToLabelDto(label);
    }

    public async Task DeleteLabelAsync(Guid labelId)
    {
        var label = await _context.Labels.FirstOrDefaultAsync(l => l.Id == labelId);
        if (label == null)
            throw new KeyNotFoundException("Label not found");

        label.IsDeleted = true;
        _context.Labels.Update(label);
        await _context.SaveChangesAsync();
    }

    public async Task AddLabelToTaskAsync(Guid taskId, Guid labelId)
    {
        var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId);
        if (task == null)
            throw new KeyNotFoundException("Task not found");

        var label = await _context.Labels.FirstOrDefaultAsync(l => l.Id == labelId);
        if (label == null)
            throw new KeyNotFoundException("Label not found");

        // Check if task-label mapping already exists
        var existingMapping = await _context.Set<TaskLabel>()
            .FirstOrDefaultAsync(tl => tl.TaskId == taskId && tl.LabelId == labelId);

        if (existingMapping != null)
            throw new InvalidOperationException("Label already assigned to task");

        var taskLabel = new TaskLabel
        {
            TaskId = taskId,
            LabelId = labelId
        };

        _context.Set<TaskLabel>().Add(taskLabel);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveLabelFromTaskAsync(Guid taskId, Guid labelId)
    {
        var taskLabel = await _context.Set<TaskLabel>()
            .FirstOrDefaultAsync(tl => tl.TaskId == taskId && tl.LabelId == labelId);

        if (taskLabel == null)
            throw new KeyNotFoundException("Label not assigned to task");

        _context.Set<TaskLabel>().Remove(taskLabel);
        await _context.SaveChangesAsync();
    }

    public async Task<List<LabelDto>> GetTaskLabelsAsync(Guid taskId)
    {
        return await _context.Set<TaskLabel>()
            .Where(tl => tl.TaskId == taskId)
            .Include(tl => tl.Label)
            .Select(tl => MapToLabelDto(tl.Label))
            .ToListAsync();
    }

    private LabelDto MapToLabelDto(Label label)
    {
        return new LabelDto
        {
            Id = label.Id,
            Name = label.Name,
            Color = label.Color,
            Description = label.Description,
            ProjectId = label.ProjectId,
            CreatedAt = label.CreatedAt
        };
    }
}
