using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MembershipSystem.API.Data;
using MembershipSystem.API.Models;
using MembershipSystem.API.Services;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// 🔹 DbContext
var connectionString = 
    Environment.GetEnvironmentVariable("SQLAZURECONNSTR_DefaultConnection") ??
    Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ??
    builder.Configuration.GetConnectionString("DefaultConnection");

Console.WriteLine("USING CONNECTION: " + connectionString?.Substring(0, 50));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddControllers();
builder.Services.AddHttpClient<VippsService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<MembershipReminderService>();
builder.Services.AddScoped<VippsService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// 🔹 JWT Authentication
var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt__Secret"]
    ?? builder.Configuration["Jwt:Secret"]
    ?? throw new Exception("JWT Secret is not configured!"));

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "MembershipApp",
            ValidAudience = "MembershipAppUsers",
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// 🔹 Swagger her ortamda aktif
app.UseSwagger();
app.UseSwaggerUI();

// 🔹 Middleware
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();