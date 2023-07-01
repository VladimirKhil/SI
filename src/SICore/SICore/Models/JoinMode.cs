using System.ComponentModel.DataAnnotations;

namespace SICore.Models;

/// <summary>
/// Allowed join mode.
/// </summary>
public enum JoinMode
{
    /// <summary>
    /// Join as any role.
    /// </summary>
    [Display(Description = "JoinModeAnyRole")]
    AnyRole,

    /// <summary>
    /// Join only as viewer.
    /// </summary>
    [Display(Description = "JoinModeViewer")]
    OnlyViewer,

    /// <summary>
    /// Join is forbidden.
    /// </summary>
    [Display(Description = "JoinModeForbidden")]
    Forbidden
}
