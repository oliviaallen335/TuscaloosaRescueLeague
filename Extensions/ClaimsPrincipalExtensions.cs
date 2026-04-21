using System.Security.Claims;

namespace AdoptionAgency.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var v = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return v == null ? throw new UnauthorizedAccessException() : int.Parse(v);
    }

    public static int? GetApplicantId(this ClaimsPrincipal user)
    {
        var v = user.FindFirstValue("ApplicantId");
        return v == null ? null : int.Parse(v);
    }

    public static bool IsEmployee(this ClaimsPrincipal user) =>
        user.IsInRole("Employee");
}
