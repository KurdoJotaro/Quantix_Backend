using System.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Quantix.Core.DTOs;
using Quantix.Core.Entities;
using Quantix.Core.Interfaces;

namespace Quantix.Service;

public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public AuthService(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        ITokenService tokenService,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task<ApiResponse<string>> RegisterAsync(RegisterDto dto)
    {
        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser != null)
            return ApiResponse<string>.FailResult("Bu e-posta adresi zaten kayıtlı.");

        var user = new AppUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName,
            BirthDate = dto.BirthDate
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return ApiResponse<string>.FailResult(errors);
        }

        await _userManager.AddToRoleAsync(user, "User");

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = HttpUtility.UrlEncode(token);

        var baseUrl = _configuration["App:BaseUrl"] ?? "https://localhost:5001";
        var verifyUrl = $"{baseUrl}/api/auth/verify-email?userId={user.Id}&token={encodedToken}";

        var emailBody = $@"
            <h2>Quantix – E-Posta Doğrulama</h2>
            <p>Merhaba {user.FullName},</p>
            <p>Hesabınızı doğrulamak için aşağıdaki bağlantıya tıklayın:</p>
            <a href='{verifyUrl}'>E-Postamı Doğrula</a>
            <p>Bu bağlantı güvenlik amacıyla oluşturulmuştur.</p>";

        await _emailService.SendEmailAsync(user.Email!, "Quantix – E-Posta Doğrulama", emailBody);

        return ApiResponse<string>.SuccessResult(
            "Kayıt başarılı! Lütfen e-posta adresinizi doğrulayın.",
            "Doğrulama e-postası gönderildi.");
    }

    public async Task<ApiResponse<TokenDto>> LoginAsync(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
            return ApiResponse<TokenDto>.FailResult("Geçersiz e-posta veya şifre.");

        if (user.IsBanned)
            return ApiResponse<TokenDto>.FailResult("Hesabınız engellenmiştir.");

        if (!user.EmailConfirmed)
            return ApiResponse<TokenDto>.FailResult(
                "E-posta adresiniz doğrulanmamış. Lütfen gelen kutunuzu kontrol edin.");

        var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
            return ApiResponse<TokenDto>.FailResult("Geçersiz e-posta veya şifre.");

        var roles = await _userManager.GetRolesAsync(user);
        var token = _tokenService.CreateToken(user, roles);

        return ApiResponse<TokenDto>.SuccessResult(token, "Giriş başarılı.");
    }

    public async Task<ApiResponse<string>> VerifyEmailAsync(VerifyEmailDto dto)
    {
        var user = await _userManager.FindByIdAsync(dto.UserId);
        if (user == null)
            return ApiResponse<string>.FailResult("Kullanıcı bulunamadı.");

        var result = await _userManager.ConfirmEmailAsync(user, dto.Token);
        if (!result.Succeeded)
            return ApiResponse<string>.FailResult("Doğrulama bağlantısı geçersiz veya süresi dolmuş.");

        return ApiResponse<string>.SuccessResult(
            "E-posta doğrulama başarılı! Artık giriş yapabilirsiniz.");
    }
}
