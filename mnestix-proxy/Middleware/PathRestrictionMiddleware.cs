namespace mnestix_proxy.Middleware
{
    /// <summary>
    /// This class is responsible for blocking incoming requests that list all shells or submodels,
    /// both on the repository (/repo/shells, /repo/submodels) and on the registry
    /// (/registry/shell-descriptors, /registry/submodel-descriptors), ensuring protecting of data
    /// </summary>
    public static class PathRestrictionMiddleware
    {
        private static readonly string[] ProxyPrefixes = { "/repo", "/registry" };
        private static readonly string[] RestrictedListingPaths =
        {
            "/shells",
            "/submodels",
            "/shell-descriptors",
            "/submodel-descriptors"
        };
        private const string Message = "Access to the requested path is restricted.";

        /// <summary>
        /// Verifies if the request targets a restricted listing endpoint and returns status code 405 if true
        /// else will do nothing and continue
        /// </summary>
        /// <returns></returns>
        public static Func<HttpContext, Func<Task>, Task> PathRestrictionHandling()
        {
            return (context, next) =>
            {
                var forwardedPath = GetForwardedPath(context.Request.Path);
                var requestMethod = context.Request.Method;

                if (!RestrictedListingPaths.Contains(forwardedPath, StringComparer.OrdinalIgnoreCase)
                    || (!HttpMethods.IsGet(requestMethod) && !HttpMethods.IsHead(requestMethod)))
                    return next();

                context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;

                return context.Response.WriteAsync(Message);
            };
        }

        /// <summary>
        /// Computes the path the backend will effectively see: the request path with the leading
        /// proxy prefix (/repo or /registry) removed, after collapsing empty segments, resolving
        /// ./.. segments and unescaping percent-encoded characters, so that variants like
        /// /repo//shells or /repo/%252e/shells cannot bypass the restriction.
        /// </summary>
        private static string GetForwardedPath(PathString requestPath)
        {
            var segments = new List<string>();

            foreach (var segment in (requestPath.Value ?? string.Empty).Split('/', StringSplitOptions.RemoveEmptyEntries))
            {
                var unescaped = Uri.UnescapeDataString(segment);

                if (unescaped == ".")
                    continue;

                if (unescaped == "..")
                {
                    if (segments.Count > 0)
                        segments.RemoveAt(segments.Count - 1);
                    continue;
                }

                segments.Add(unescaped);
            }

            if (segments.Count > 0 && ProxyPrefixes.Contains("/" + segments[0], StringComparer.OrdinalIgnoreCase))
                segments.RemoveAt(0);

            return "/" + string.Join("/", segments);
        }
    }
}