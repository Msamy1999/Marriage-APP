using Microsoft.AspNetCore.Identity;
using MarriageApp.Core.Entities;

namespace MarriageApp.Infrastructure.Identity;

/// <summary>
/// ASP.NET Core Identity user. Auth concerns only — all matchmaking data lives on the
/// linked <see cref="Profile"/> (one-to-one). Kept in Infrastructure so the Core domain
/// stays free of any Identity/persistence dependency.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>The applicant profile owned by this user (null until they fill the form).</summary>
    public Profile? Profile { get; set; }
}
