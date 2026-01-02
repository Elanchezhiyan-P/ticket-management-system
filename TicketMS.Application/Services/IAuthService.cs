using TicketMS.Application.Common;
using TicketMS.Application.DTOs;

namespace TicketMS.Application.Services
{
    public interface IAuthService
    {
        Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterDto dto);
        Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto dto);
        Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(string refreshToken);
        Task<ApiResponse<UserDto>> GetCurrentUserAsync(string userId);
        Task<ApiResponse> RevokeTokenAsync(string userId);
    }
}
