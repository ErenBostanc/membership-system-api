using Microsoft.AspNetCore.Mvc;
using MembershipSystem.API.Data;
using MembershipSystem.API.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


namespace MembershipSystem.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

    [HttpPost("login")]
    public IActionResult Login(LoginRequest request)
    {
        var user = _context.Users
            .FirstOrDefault(x => x.Email == request.Email);

        if (user == null)
            return Unauthorized();

        var hasher =
            new Microsoft.AspNetCore.Identity.PasswordHasher<User>();

        var result = hasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (result ==
            Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed)
            return Unauthorized();

        var token = GenerateJwtToken(user);

        return Ok(new { token });
    }

        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("THIS_IS_A_VERY_LONG_SECRET_KEY_FOR_JWT_123456"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: "MembershipApp",
                audience: "MembershipAppUsers",
                claims: claims,
                expires: DateTime.Now.AddHours(2),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    [HttpPost("register")]
    public IActionResult Register(RegisterRequest request)
    {
        var existingUser =
            _context.Users.FirstOrDefault(x => x.Email == request.Email);

        if (existingUser != null)
            return BadRequest("User already exists");

        var hasher =
            new Microsoft.AspNetCore.Identity.PasswordHasher<User>();

        var user = new User
        {
            Email = request.Email,
            PasswordHash = hasher.HashPassword(null, request.Password),
            Role = "User"
        };

        _context.Users.Add(user);
        _context.SaveChanges();

        return Ok("User created");
    }

    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
}