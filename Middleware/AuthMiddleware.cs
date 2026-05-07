namespace AxpigeonApp.Middleware
{
    public class AuthMiddleware
    {
        private readonly RequestDelegate _next;

        public AuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            // Check if user is authenticated
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                // Optionally, ignore static files and login page
                var path = context.Request.Path.Value?.ToLower();
                if (!path.Contains("/auth/login") && !path.Contains("/css") && !path.Contains("/js"))
                {
                    context.Response.Redirect("/Auth/Login");
                    return;
                }
            }

            // Call the next middleware
            await _next(context);
        }
    }
}
