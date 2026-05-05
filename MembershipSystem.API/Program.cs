using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MembershipSystem.API.Data;
using MembershipSystem.API.Models;
using MembershipSystem.API.Services;
using Hangfire;
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

// 🔹 Swagger
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

// builder.Services.AddHangfire(config =>
//    config.UseSqlServerStorage(
//        builder.Configuration.GetConnectionString("DefaultConnection")));

// builder.Services.AddHangfireServer();

// 🔹 Authorization
builder.Services.AddAuthorization();

builder.Services.AddControllers();

var app = builder.Build();

// 🔹 Swagger aktif
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 🔹 Migration ve Seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

// Debug: connection string'i logla
var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine("CONNECTION STRING: " + connStr);

    db.Database.Migrate();

    if (!db.Users.Any())
    {
        var hasher = new PasswordHasher<User>();

        db.Users.Add(new User
        {
            Email = "admin@test.com",
            PasswordHash = hasher.HashPassword(new User(), "1234"),
            Role = "Admin"
        });

        db.SaveChanges();
    }
}

//using (var scope = app.Services.CreateScope())
//{
//    var recurringJobs =
//        scope.ServiceProvider
//            .GetRequiredService<IRecurringJobManager>();

//    recurringJobs.AddOrUpdate<MembershipReminderService>(
//        "membership-reminders",
//        x => x.CheckExpiringMemberships(),
//        Cron.Daily);
//}

// 🔹 Middleware
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
//app.UseHangfireDashboard();

app.Run();