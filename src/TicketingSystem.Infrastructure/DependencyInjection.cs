using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketingSystem.Application.Services;
using TicketingSystem.Domain.Repositories;
using TicketingSystem.Infrastructure.Options;
using TicketingSystem.Infrastructure.Persistence;
using TicketingSystem.Infrastructure.Persistence.Repositories;
using TicketingSystem.Infrastructure.Services;

namespace TicketingSystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration["POSTGRES_CONNECTION_STRING"]
                ?? throw new InvalidOperationException("POSTGRES_CONNECTION_STRING is required.")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IVerificationTokenRepository, VerificationTokenRepository>();
        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<IEpicRepository, EpicRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();

        services.Configure<JwtSettings>(opts =>
        {
            opts.SecretKey = configuration["JWT_SECRET_KEY"]
                ?? throw new InvalidOperationException("JWT_SECRET_KEY is required.");
            if (int.TryParse(configuration["JWT_EXPIRATION_MINUTES"], out var minutes))
                opts.ExpirationMinutes = minutes;
        });

        services.Configure<SmtpSettings>(opts =>
        {
            opts.Host = configuration["SMTP_HOST"] ?? string.Empty;
            opts.Port = int.TryParse(configuration["SMTP_PORT"], out var port) ? port : 587;
            opts.User = configuration["SMTP_USER"] ?? string.Empty;
            opts.Password = configuration["SMTP_PASSWORD"] ?? string.Empty;
        });

        services.Configure<AppSettings>(opts =>
        {
            opts.BaseUrl = configuration["APP_BASE_URL"] ?? "http://localhost";
        });

        services.Configure<Argon2HashingSettings>(_ => { });

        services.AddScoped<IPasswordHasher, ArgonPasswordHasher>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        return services;
    }
}
