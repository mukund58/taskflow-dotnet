namespace Backend.Controllers;

using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

/// <summary>
/// Base controller providing common helper methods shared across all API controllers.
/// Eliminates duplication of GetCurrentUserId() and HasElevatedAccess() across the codebase.
/// </summary>
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Extracts the current authenticated user's ID from the JWT claims.
    /// </summary>
    protected Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid user context");

        return userId;
    }

    /// <summary>
    /// Checks whether the current user has elevated access (Admin or Manager roles).
    /// Set <paramref name="includeManager"/> to false for Admin-only checks.
    /// </summary>
    protected bool HasElevatedAccess(bool includeManager = true)
    {
        if (User.IsInRole("Admin"))
            return true;

        return includeManager && User.IsInRole("Manager");
    }
}
