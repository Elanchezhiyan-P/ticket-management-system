using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TicketMS.Application.Common;
using TicketMS.Application.DTOs;
using TicketMS.Infrastructure.Data;
using TicketMS.Infrastructure.Entities;

namespace TicketMS.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ITokenService tokenService,
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _context = context;
            _configuration = configuration;
        }

        public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterDto dto)
        {
            // Check if email exists
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                return ApiResponse<AuthResponseDto>.FailureResponse("Email is already registered");
            }

            // Check if phone exists
            var phoneExists = await _context.Users.AnyAsync(u => u.PhoneNumber == dto.PhoneNumber);
            if (phoneExists)
            {
                return ApiResponse<AuthResponseDto>.FailureResponse("Phone number is already registered");
            }

            // Validate age (must be at least 18)
            var age = CalculateAge(dto.DateOfBirth);
            if (age < 18)
            {
                return ApiResponse<AuthResponseDto>.FailureResponse("User must be at least 18 years old");
            }

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                PhoneNumber = dto.PhoneNumber,
                DateOfBirth = dto.DateOfBirth,
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return ApiResponse<AuthResponseDto>.FailureResponse("Registration failed", errors);
            }

            // Assign role (default to Developer if not specified)
            var role = string.IsNullOrEmpty(dto.Role) ? "Developer" : dto.Role;
            await _userManager.AddToRoleAsync(user, role);

            // Generate tokens
            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = _tokenService.GenerateAccessToken(user, roles);
            var refreshToken = await CreateRefreshTokenAsync(user.Id);

            var response = new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:AccessTokenExpiryMinutes"] ?? "60")),
                User = MapToUserDto(user, roles.ToList())
            };

            return ApiResponse<AuthResponseDto>.SuccessResponse(response, "Registration successful");
        }

        public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);

            if (user == null)
            {
                return ApiResponse<AuthResponseDto>.FailureResponse("Invalid email or password");
            }

            if (!user.IsActive)
            {
                return ApiResponse<AuthResponseDto>.FailureResponse("Account is deactivated. Please contact admin.");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);

            if (result.IsLockedOut)
            {
                return ApiResponse<AuthResponseDto>.FailureResponse("Account is locked. Please try again later.");
            }

            if (!result.Succeeded)
            {
                return ApiResponse<AuthResponseDto>.FailureResponse("Invalid email or password");
            }

            // Generate tokens
            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = _tokenService.GenerateAccessToken(user, roles);
            var refreshToken = await CreateRefreshTokenAsync(user.Id);

            var response = new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:AccessTokenExpiryMinutes"] ?? "60")),
                User = MapToUserDto(user, roles.ToList())
            };

            return ApiResponse<AuthResponseDto>.SuccessResponse(response, "Login successful");
        }

        public async Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(string refreshToken)
        {
            var storedToken = await _context.RefreshTokens
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Token == refreshToken);

            if (storedToken == null)
            {
                return ApiResponse<AuthResponseDto>.FailureResponse("Invalid refresh token");
            }

            if (!storedToken.IsActive)
            {
                return ApiResponse<AuthResponseDto>.FailureResponse("Refresh token has expired or been revoked");
            }

            var user = storedToken.User;

            if (!user.IsActive)
            {
                return ApiResponse<AuthResponseDto>.FailureResponse("Account is deactivated");
            }

            // Revoke old refresh token
            storedToken.IsRevoked = true;
            storedToken.RevokedAt = DateTime.UtcNow;

            // Generate new tokens
            var roles = await _userManager.GetRolesAsync(user);
            var newAccessToken = _tokenService.GenerateAccessToken(user, roles);
            var newRefreshToken = await CreateRefreshTokenAsync(user.Id);

            await _context.SaveChangesAsync();

            var response = new AuthResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken.Token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:AccessTokenExpiryMinutes"] ?? "60")),
                User = MapToUserDto(user, roles.ToList())
            };

            return ApiResponse<AuthResponseDto>.SuccessResponse(response, "Token refreshed successfully");
        }

        public async Task<ApiResponse<UserDto>> GetCurrentUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return ApiResponse<UserDto>.FailureResponse("User not found");
            }

            var roles = await _userManager.GetRolesAsync(user);

            return ApiResponse<UserDto>.SuccessResponse(MapToUserDto(user, roles.ToList()));
        }

        public async Task<ApiResponse> RevokeTokenAsync(string userId)
        {
            var tokens = await _context.RefreshTokens
                .Where(r => r.UserId == userId && !r.IsRevoked)
                .ToListAsync();

            foreach (var token in tokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return ApiResponse.SuccessResponse("All tokens revoked successfully");
        }

        private async Task<RefreshToken> CreateRefreshTokenAsync(string userId)
        {
            var refreshTokenExpiryDays = int.Parse(_configuration["Jwt:RefreshTokenExpiryDays"] ?? "7");

            var refreshToken = new RefreshToken
            {
                Token = _tokenService.GenerateRefreshToken(),
                UserId = userId,
                ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpiryDays)
            };

            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();

            return refreshToken;
        }

        private static UserDto MapToUserDto(ApplicationUser user, List<string> roles)
        {
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber!,
                DateOfBirth = user.DateOfBirth,
                Age = user.Age,
                IsActive = user.IsActive,
                Roles = roles
            };
        }

        private static int CalculateAge(DateTime dateOfBirth)
        {
            var today = DateTime.Today;
            var age = today.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > today.AddYears(-age)) age--;
            return age;
        }

    }
}
