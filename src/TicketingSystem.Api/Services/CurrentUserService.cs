using System.IdentityModel.Tokens.Jwt;
using TicketingSystem.Application.Services;

namespace TicketingSystem.Api.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    public Guid UserId
    {
        get
        {
            var subject = _httpContextAccessor.HttpContext?.User
                .FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            return Guid.TryParse(subject, out var userId)
                ? userId
                : throw new InvalidOperationException("No authenticated user is available.");
        }
    }
}
