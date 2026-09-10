namespace mnestix_proxy.Middleware
{
    /// <summary>
    /// This class is responsible for blocking incoming requests that list all shells or submodels,
    /// both on the repository (/repo/shells, /repo/submodels) and on the registry
    /// (/registry/shell-descriptors, /registry/submodel-descriptors), ensuring protecting of data
    /// </summary>
    public static class PathRestrictionMiddleware
    {
        private static readonly string[] RestrictedPaths =
        {
            "/repo/shells",
            "/repo/submodels",
            "/registry/shell-descriptors",
            "/registry/submodel-descriptors"
        };
        private const string Message = "Access to the requested path is restricted.";

        /// <summary>
        /// Verifying if requested path is equal to restricted paths and returns status code 405 if true
        /// else will do nothing and continue
        /// </summary>
        /// <returns></returns>
        public static Func<HttpContext, Func<Task>, Task> PathRestrictionHandling()
        {
            return (context, next) =>
            {
                var requestPath = Normalize(context.Request.Path);
                var requestMethod = context.Request.Method;

                if (!RestrictedPaths.Any(path => path.Equals(requestPath, StringComparison.OrdinalIgnoreCase)) || requestMethod != "GET")
                    return next();

                context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;

                return context.Response.WriteAsync(Message);
            };
        }

        /// <summary>
        /// Trims a trailing slash so /registry/shell-descriptors/ cannot be used to bypass the check.
        /// The catch-all routes match an empty remainder and would return the full collection.
        /// </summary>
        private static string Normalize(PathString requestPath)
        {
            var path = requestPath.Value ?? string.Empty;

            return path.Length > 1 ? path.TrimEnd('/') : path;
        }
    }
}