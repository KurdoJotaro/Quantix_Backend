using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Quantix.Core.Entities;

namespace Quantix.API.Authentication;

public class ActiveUserJwtEvents : JwtBearerEvents
{
    private readonly UserManager<AppUser> _userManager;

    public ActiveUserJwtEvents(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    public override async Task TokenValidated(TokenValidatedContext context)
    {
        var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            context.Fail("Geçersiz kullanıcı.");
            return;
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || user.IsBanned || !user.EmailConfirmed)
            context.Fail("Hesap erişime kapalı.");
    }
}
