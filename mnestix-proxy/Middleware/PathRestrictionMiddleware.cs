namespace mnestix_proxy.Middleware
{
    /// <summary>
    /// This class is responsible for blocking incoming requests for /repo/shells and /repo/submodels
    /// ensuring protecting of data
    /// </summary>
    public static class PathRestrictionMiddleware
    {
        private static readonly string[] RestrictedPaths = { "/repo/shells", "/repo/submodels" };
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
                var requestPath = context.Request.Path;
                var requestMethod = context.Request.Method;

                if (!RestrictedPaths.Any(path => path.Equals(requestPath, StringComparison.OrdinalIgnoreCase)) || requestMethod != "GET")
                    return next();

                context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;

                return context.Response.WriteAsync(Message);
            };
        }
    }
}