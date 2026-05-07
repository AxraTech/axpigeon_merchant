namespace AxpigeonApp.Middleware
{
    public class RoleMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _requiredRole;

        public RoleMiddleware(RequestDelegate next, string requiredRole)
        {
            _next = next;
            _requiredRole = requiredRole;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                context.Response.Redirect("/Auth/Login");
                return;
            }

            var role = context.User.FindFirst("Role")?.Value;
            if (role != _requiredRole)
            {
                context.Response.Redirect("/Auth/AccessDenied");
                return;
            }

            await _next(context);
        }
    }
}
