using Microsoft.AspNetCore.Authorization;

namespace mnestix_proxy.Authentication.ApiKeyAuthorization;

/// <summary>
/// Represents the requirement, that an API key must be present
/// </summary>
public class ApiKeyRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Constructor
    /// </summary>
    public ApiKeyRequirement()
    {
    }
}