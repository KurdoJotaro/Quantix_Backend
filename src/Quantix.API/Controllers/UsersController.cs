using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quantix.Core.DTOs;
using Quantix.Core.Entities;

namespace Quantix.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;

    public UsersController(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userManager.Users
            .Select(u => new UserDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email!,
                EmailConfirmed = u.EmailConfirmed,
                IsBanned = u.IsBanned,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<IEnumerable<UserDto>>.SuccessResult(users));
    }

    [HttpPut("{id}/ban")]
    public async Task<IActionResult> ToggleBan(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound(ApiResponse<string>.FailResult("Kullanıcı bulunamadı."));

        user.IsBanned = !user.IsBanned;
        await _userManager.UpdateAsync(user);

        var status = user.IsBanned ? "engellendi" : "engeli kaldırıldı";
        return Ok(ApiResponse<string>.SuccessResult($"Kullanıcı {status}."));
    }
}
