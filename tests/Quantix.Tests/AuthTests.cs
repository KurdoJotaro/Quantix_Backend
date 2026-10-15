using System.Net.Mail;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Quantix.API.Authentication;
using Quantix.API.Middleware;
using Quantix.Core.DTOs;
using Quantix.Core.Entities;
using Quantix.Core.Interfaces;
using Quantix.Data.Seeds;
using Quantix.Service;
using Xunit;

namespace Quantix.Tests;

public class AuthTests
{
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task ExistingTokensAreRejectedWhenTheAccountLosesAccess(bool isBanned, bool emailConfirmed)
    {
        using var database = new TestDatabase();
        await database.AddUserAsync("user", isBanned, emailConfirmed);
        var tokenContext = CreateTokenContext("user");
        var events = new ActiveUserJwtEvents(database.Services.GetRequiredService<UserManager<AppUser>>());

        await events.TokenValidated(tokenContext);

        Assert.NotNull(tokenContext.Result?.Failure);
    }

    [Fact]
    public async Task DeletedUserTokensAreRejected()
    {
        using var database = new TestDatabase();
        var tokenContext = CreateTokenContext("missing");
        var events = new ActiveUserJwtEvents(database.Services.GetRequiredService<UserManager<AppUser>>());

        await events.TokenValidated(tokenContext);

        Assert.NotNull(tokenContext.Result?.Failure);
    }

    [Fact]
    public async Task TokenWithoutAUserIdentifierIsRejected()
    {
        using var database = new TestDatabase();
        var tokenContext = CreateTokenContext(null);
        var events = new ActiveUserJwtEvents(database.Services.GetRequiredService<UserManager<AppUser>>());

        await events.TokenValidated(tokenContext);

        Assert.NotNull(tokenContext.Result?.Failure);
    }

    [Fact]
    public async Task ActiveConfirmedUserTokenIsAccepted()
    {
        using var database = new TestDatabase();
        await database.AddUserAsync("user");
        var tokenContext = CreateTokenContext("user");
        var events = new ActiveUserJwtEvents(database.Services.GetRequiredService<UserManager<AppUser>>());

        await events.TokenValidated(tokenContext);

        Assert.Null(tokenContext.Result?.Failure);
    }

    [Fact]
    public async Task ServerErrorsDoNotExposeExceptionDetails()
    {
        var middleware = new ExceptionHandlerMiddleware(
            _ => throw new InvalidOperationException("private-database-detail"),
            NullLogger<ExceptionHandlerMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.Invoke(context);
        context.Response.Body.Position = 0;
        using var response = await JsonDocument.ParseAsync(context.Response.Body);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.False(response.RootElement.GetProperty("success").GetBoolean());
        Assert.False(response.RootElement.TryGetProperty("detail", out _));
        Assert.DoesNotContain("private-database-detail", response.RootElement.GetRawText());
    }

    [Fact]
    public async Task FailedVerificationEmailRollsBackRegistrationAndAllowsRetry()
    {
        using var database = new TestDatabase();
        await DataSeeder.SeedAsync(database.Services);
        var email = new RecordingEmailService { FailDelivery = true };
        var service = CreateAuthService(database, email);
        var registration = new RegisterDto
        {
            Email = "registration@example.test", FullName = "Test User", Password = "Learning123!"
        };

        var failed = await service.RegisterAsync(registration);

        Assert.False(failed.Success);
        Assert.Empty(await database.Context.Users.ToListAsync());
        email.FailDelivery = false;
        Assert.True((await service.RegisterAsync(registration)).Success);
        Assert.Single(await database.Context.Users.ToListAsync());
    }

    [Fact]
    public async Task VerificationEmailUsesFrontendRouteAndEscapesUserContent()
    {
        using var database = new TestDatabase();
        await DataSeeder.SeedAsync(database.Services);
        var email = new RecordingEmailService();
        var service = CreateAuthService(database, email);

        var response = await service.RegisterAsync(new RegisterDto
        {
            Email = "registration@example.test", FullName = "<b>User</b>", Password = "Learning123!"
        });

        Assert.True(response.Success);
        Assert.Contains("https://client.example.test/verify-email?userId=", email.Body);
        Assert.Contains("&lt;b&gt;User&lt;/b&gt;", email.Body);
        Assert.DoesNotContain("<b>User</b>", email.Body);
    }

    [Fact]
    public async Task AdminIsNotCreatedWithoutExplicitConfiguration()
    {
        using var database = new TestDatabase();

        await DataSeeder.SeedAsync(database.Services);

        Assert.Empty(await database.Context.Users.ToListAsync());
        Assert.Equal(2, await database.Context.Roles.CountAsync());
    }

    private static TokenValidatedContext CreateTokenContext(string? userId)
    {
        var claims = userId == null ? Array.Empty<Claim>() : new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
        return new TokenValidatedContext(new DefaultHttpContext(),
            new AuthenticationScheme("Bearer", null, typeof(JwtBearerHandler)), new JwtBearerOptions())
        {
            Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"))
        };
    }

    private static AuthService CreateAuthService(TestDatabase database, IEmailService emailService)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["App:FrontendUrl"] = "https://client.example.test/"
        }).Build();

        return new AuthService(database.Services.GetRequiredService<UserManager<AppUser>>(),
            database.Services.GetRequiredService<SignInManager<AppUser>>(),
            new TokenService(configuration), emailService, configuration);
    }

    private sealed class RecordingEmailService : IEmailService
    {
        public bool FailDelivery { get; set; }
        public string Body { get; private set; } = string.Empty;

        public Task SendEmailAsync(string to, string subject, string body)
        {
            if (FailDelivery)
                throw new SmtpException("Simulated delivery failure");

            Body = body;
            return Task.CompletedTask;
        }
    }
}
