using Quantix.Core.DTOs;

namespace Quantix.Core.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<string>> RegisterAsync(RegisterDto dto);
    Task<ApiResponse<TokenDto>> LoginAsync(LoginDto dto);
    Task<ApiResponse<string>> VerifyEmailAsync(VerifyEmailDto dto);
}
