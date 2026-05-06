using Hangfire.Dashboard;

namespace MembershipSystem.API
{
    public class HangfireAuthFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();

            var authHeader = httpContext.Request.Headers["Authorization"].ToString();

            if (authHeader.StartsWith("Basic "))
            {
                var encoded = authHeader.Substring("Basic ".Length).Trim();
                var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
                var parts = decoded.Split(':');

                if (parts.Length == 2 && parts[0] == "admin@test.com" && parts[1] == "1234")
                    return true;
            }

            httpContext.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Hangfire Dashboard\"";
            httpContext.Response.StatusCode = 401;
            return false;
        }
    }
}