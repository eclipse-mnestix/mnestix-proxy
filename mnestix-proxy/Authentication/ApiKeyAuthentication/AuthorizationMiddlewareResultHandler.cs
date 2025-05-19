using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Authorization;

namespace mnestix_proxy.Authentication.ApiKeyAuthentication
{
    public class CustomAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
    {
        private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

        public async Task HandleAsync(RequestDelegate next, HttpContext context,
            AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
        {
            if (authorizeResult.Forbidden)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";

                var message = "Forbidden";
                if (authorizeResult.AuthorizationFailure?.FailureReasons?.Any() == true)
                {
                    message = authorizeResult.AuthorizationFailure.FailureReasons
                        .Select(r => r.Message)
                        .FirstOrDefault() ?? message;
                }

                await context.Response.WriteAsJsonAsync(new { error = message });
                return;
            }

            if (authorizeResult.Challenged)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";

                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Unauthorized: You must provide valid authentication credentials."
                });
                return;
            }

            // Proceed normally
            await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
        }
    }

}
