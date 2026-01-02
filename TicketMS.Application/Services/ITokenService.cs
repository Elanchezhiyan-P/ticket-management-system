using System.Security.Claims;
using TicketMS.Infrastructure.Entities;

namespace TicketMS.Application.Services
{
    public interface ITokenService
    {
        string GenerateAccessToken(ApplicationUser user, IList<string> roles);
        string GenerateRefreshToken();
        ClaimsPrincipal? ValidateToken(string token);
    }
}
