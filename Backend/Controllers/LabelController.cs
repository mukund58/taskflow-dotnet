namespace Backend.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Models.DTOs;
using Backend.Services.Interfaces;
using System.Security.Claims;

[ApiController]
[Route("api/projects/{projectId}/labels")]
[Authorize]
public class LabelController : ControllerBase
{
    private readonly ILabelService _labelService;
    private readonly IProjectService _projectService;

    public LabelController(ILabelService labelService, IProjectService projectService)
    {
        _labelService = labelService;
        _projectService = projectService;
    }

    /// <summary>
    /// Get all labels for a project
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "ProjectRead")]
    public async Task<IActionResult> GetProjectLabels(Guid projectId)
    {
        try
        {
            var labels = await _labelService.GetLabelsByProjectAsync(projectId);
            return Ok(ApiResponseDto<List<LabelDto>>.Ok(labels, "Labels retrieved successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponseDto<object>.Fail($"Internal server error: {ex.Message}"));
        }
    }

    /// <summary>
    /// Create a new label in a project
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "ProjectWrite")]
    public async Task<IActionResult> CreateLabel(Guid projectId, [FromBody] CreateLabelDto dto)
    {
        try
        {
            var label = await _labelService.CreateLabelAsync(dto, projectId);
            return CreatedAtAction(nameof(GetLabelById), new { projectId, labelId = label.Id },
                ApiResponseDto<LabelDto>.Ok(label, "Label created successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponseDto<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponseDto<object>.Fail($"Internal server error: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get a specific label by ID
    /// </summary>
    [HttpGet("{labelId}")]
    [Authorize(Policy = "ProjectRead")]
    public async Task<IActionResult> GetLabelById(Guid projectId, Guid labelId)
    {
        try
        {
            var label = await _labelService.GetLabelByIdAsync(labelId);
            if (label.ProjectId != projectId)
                return NotFound(ApiResponseDto<object>.Fail("Label not found in this project"));

            return Ok(ApiResponseDto<LabelDto>.Ok(label, "Label retrieved successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponseDto<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponseDto<object>.Fail($"Internal server error: {ex.Message}"));
        }
    }

    /// <summary>
    /// Update a label
    /// </summary>
    [HttpPut("{labelId}")]
    [Authorize(Policy = "ProjectWrite")]
    public async Task<IActionResult> UpdateLabel(Guid projectId, Guid labelId, [FromBody] UpdateLabelDto dto)
    {
        try
        {
            var label = await _labelService.UpdateLabelAsync(labelId, dto);
            if (label.ProjectId != projectId)
                return NotFound(ApiResponseDto<object>.Fail("Label not found in this project"));

            return Ok(ApiResponseDto<LabelDto>.Ok(label, "Label updated successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponseDto<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponseDto<object>.Fail($"Internal server error: {ex.Message}"));
        }
    }

    /// <summary>
    /// Delete a label
    /// </summary>
    [HttpDelete("{labelId}")]
    [Authorize(Policy = "ProjectWrite")]
    public async Task<IActionResult> DeleteLabel(Guid projectId, Guid labelId)
    {
        try
        {
            var label = await _labelService.GetLabelByIdAsync(labelId);
            if (label.ProjectId != projectId)
                return NotFound(ApiResponseDto<object>.Fail("Label not found in this project"));

            await _labelService.DeleteLabelAsync(labelId);
            return Ok(ApiResponseDto<object>.Ok(null, "Label deleted successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponseDto<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponseDto<object>.Fail($"Internal server error: {ex.Message}"));
        }
    }

    /// <summary>
    /// Add a label to a task
    /// </summary>
    [HttpPost("tasks/{taskId}/assign")]
    [Authorize(Policy = "TaskWrite")]
    public async Task<IActionResult> AddLabelToTask(Guid projectId, Guid taskId, [FromQuery] Guid labelId)
    {
        try
        {
            await _labelService.AddLabelToTaskAsync(taskId, labelId);
            return Ok(ApiResponseDto<object>.Ok(null, "Label added to task successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponseDto<object>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponseDto<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponseDto<object>.Fail($"Internal server error: {ex.Message}"));
        }
    }

    /// <summary>
    /// Remove a label from a task
    /// </summary>
    [HttpDelete("tasks/{taskId}/remove")]
    [Authorize(Policy = "TaskWrite")]
    public async Task<IActionResult> RemoveLabelFromTask(Guid projectId, Guid taskId, [FromQuery] Guid labelId)
    {
        try
        {
            await _labelService.RemoveLabelFromTaskAsync(taskId, labelId);
            return Ok(ApiResponseDto<object>.Ok(null, "Label removed from task successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponseDto<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponseDto<object>.Fail($"Internal server error: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get all labels for a specific task
    /// </summary>
    [HttpGet("tasks/{taskId}")]
    [Authorize(Policy = "TaskRead")]
    public async Task<IActionResult> GetTaskLabels(Guid projectId, Guid taskId)
    {
        try
        {
            var labels = await _labelService.GetTaskLabelsAsync(taskId);
            return Ok(ApiResponseDto<List<LabelDto>>.Ok(labels, "Task labels retrieved successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponseDto<object>.Fail($"Internal server error: {ex.Message}"));
        }
    }
}
