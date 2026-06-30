using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using TicketingSystem.Api.HealthChecks;
using TicketingSystem.Api.Middleware;
using TicketingSystem.Api.Services;
using TicketingSystem.Application;
using TicketingSystem.Application.Services;
using TicketingSystem.Infrastructure;
using TicketingSystem.Infrastructure.Options;
using TicketingSystem.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

// Source the validation key from the same JwtSettings that JwtService signs with,
// resolved lazily after all configuration sources are composed. Reading the key
// eagerly here would capture it before late-added sources (e.g. test config) are
// applied, splitting the signer's and validator's keys and breaking validation.
builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtSettings>>((options, jwtSettings) =>
    {
        var secretKey = jwtSettings.Value.SecretKey;
        if (string.IsNullOrWhiteSpace(secretKey))
            throw new InvalidOperationException("JWT_SECRET_KEY is required.");

        // The JWT is issued with a raw "sub" claim; keep it unmapped so
        // CurrentUserService can read JwtRegisteredClaimNames.Sub directly.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        };
    });

builder.Services.AddAuthorization(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

const string CorsPolicy = "AngularSpa";
builder.Services.AddCors(options =>
    options.AddPolicy(CorsPolicy, policy => policy
        .WithOrigins(builder.Configuration["ANGULAR_ORIGIN"] ?? "http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod()));

// Liveness has no checks (process up); readiness probes the database via the
// "ready" tag. Used by the container HEALTHCHECK and compose startup ordering.
builder.Services
    .AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health endpoints are public — AllowAnonymous opts them out of the global
// RequireAuthenticatedUser fallback policy.
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false })
    .AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") })
    .AllowAnonymous();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.Run();

// Exposed for WebApplicationFactory<Program> in the integration test project.
public partial class Program;
