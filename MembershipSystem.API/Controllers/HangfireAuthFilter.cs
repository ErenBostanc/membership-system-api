using Hangfire.Dashboard;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace MembershipSystem.API
{
    public class HangfireAuthFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            
            var token = httpContext.Request.Cookies["HangfireToken"] ?? 
                        httpContext.Request.Headers["Authorization"]
                            .ToString().Replace("Bearer ", "");

            if (string.IsNullOrEmpty(token))
                return false;

            try
            {
                var key = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(
                        httpContext.RequestServices
                            .GetRequiredService<IConfiguration>()["Jwt__Secret"] ??
                        httpContext.RequestServices
                            .GetRequiredService<IConfiguration>()["Jwt:Secret"]));

                var handler = new JwtSecurityTokenHandler();
                handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = "MembershipApp",
                    ValidateAudience = true,
                    ValidAudience = "MembershipAppUsers"
                }, out _);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}