using System.ComponentModel.DataAnnotations;

namespace Quantix.Core.DTOs;

public class RegisterDto
{
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;

    public DateTime? BirthDate { get; set; }
}

public class LoginDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class TokenDto
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }
}

public class VerifyEmailDto
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;
}
