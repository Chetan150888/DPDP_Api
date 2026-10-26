
using System.Threading.RateLimiting;
using System.Text;
using DPDP_Api.Data;
using DPDP_Api.Middleware;
using DPDP_Api.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// JWT Configuration
var jwtKey = builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException(
        "JWT signing key is missing.");

if (jwtKey.Length < 64)
{
    throw new InvalidOperationException(
        "JWT signing key must contain at least 64 characters.");
}

// Controllers
builder.Services.AddControllers();

// Application Services
builder.Services.AddSingleton<Db>();
builder.Services.AddSingleton<PasswordService>();
builder.Services.AddSingleton<TokenService>();

// JWT Authentication
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],

                ValidateAudience = true,
                ValidAudience = builder.Configuration["Jwt:Audience"],

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey)),

                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
    });

// Authorization
builder.Services.AddAuthorization();

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter =
        PartitionedRateLimiter.Create<HttpContext, string>(
            context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString()
                    ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 120,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));
});

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("ApiCors", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:4200",
                "https://localhost:4200",
                "https://dpdp-beta.vercel.app",
                "https://uatapi.convergedesk.com",

                   // Production Frontend
                "https://convergedpdp.com",
                "https://www.convergedpdp.com"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Swagger
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "DPDP API",
            Version = "v1"
        });

    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header
        });

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
});

var app = builder.Build();

// Global Exception Handling
app.UseMiddleware<ExceptionMiddleware>();

// Swagger enabled for UAT
app.UseSwagger();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "v1/swagger.json",
        "DPDP API v1");
});

// HTTPS
app.UseHttpsRedirection();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// Rate Limiting
app.UseRateLimiter();

// Security Headers
app.Use(async (context, next) =>
{
    context.Response.Headers[
        "X-Content-Type-Options"] = "nosniff";

    context.Response.Headers[
        "X-Frame-Options"] = "DENY";

    context.Response.Headers[
        "Referrer-Policy"] = "no-referrer";

    context.Response.Headers[
        "Permissions-Policy"] =
        "camera=(), microphone=(), geolocation=()";

    await next();
});

// CORS must execute before authentication
app.UseCors("ApiCors");

// Authentication
app.UseAuthentication();

// Session Validation
app.UseMiddleware<SessionValidationMiddleware>();

// Authorization
app.UseAuthorization();

// Controllers
app.MapControllers();

app.Run();