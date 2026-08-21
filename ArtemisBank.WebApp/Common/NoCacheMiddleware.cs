namespace ArtemisBank.WebApp.Common
{
    public class NoCacheMiddleware
    {
        private readonly RequestDelegate _next;

        public NoCacheMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            context.Response.OnStarting(() =>
            {
                var path = context.Request.Path.Value ?? string.Empty;

                var isStaticAsset =
                    path.StartsWith("/css", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/js", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/images", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase);

                if (!isStaticAsset)
                {
                    var headers = context.Response.Headers;

                    headers.CacheControl = "no-store, no-cache, must-revalidate, max-age=0";

                    headers.Pragma = "no-cache";
                    headers.Expires = "0";
                }

                return Task.CompletedTask;
            });

            await _next(context);
        }
    }

    public static class NoCacheMiddlewareExtensions
    {
        public static IApplicationBuilder UseNoCacheForPages(this IApplicationBuilder app)
            => app.UseMiddleware<NoCacheMiddleware>();
    }
}
