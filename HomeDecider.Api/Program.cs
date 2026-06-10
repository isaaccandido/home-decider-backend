using System.Text;
using DotNetEnv;
using HomeDecider.Api.Middleware;
using HomeDecider.Api.Data;
using HomeDecider.Api.Models;
using HomeDecider.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    var host     = builder.Configuration["DB_HOST"]     ?? "localhost";
    var port     = builder.Configuration["DB_PORT"]     ?? "5432";
    var name     = builder.Configuration["DB_NAME"]     ?? throw new InvalidOperationException("DB_NAME is not set.");
    var user     = builder.Configuration["DB_USERNAME"] ?? throw new InvalidOperationException("DB_USERNAME is not set.");
    var password = builder.Configuration["DB_PASSWORD"] ?? throw new InvalidOperationException("DB_PASSWORD is not set.");
    connectionString = $"Host={host};Port={port};Database={name};Username={user};Password={password}";
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDecisionService, DecisionService>();

var jwtKey = builder.Configuration["JWT_SECRET"] ?? throw new InvalidOperationException("JWT_SECRET is not set.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(builder.Configuration["ALLOWED_ORIGIN"] ?? throw new InvalidOperationException("ALLOWED_ORIGIN is not set."))
              .AllowAnyHeader()
              .AllowAnyMethod()));

var app = builder.Build();

await app.SeedAsync();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
