using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quantix.Core.Entities;
using Quantix.Data;

namespace Quantix.Tests;

internal sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;

    public TestDatabase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
        services.AddIdentity<AppUser, AppRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        _provider = services.BuildServiceProvider();
        _scope = _provider.CreateScope();
        Context = Services.GetRequiredService<AppDbContext>();
        Context.Database.EnsureCreated();
    }

    public IServiceProvider Services => _scope.ServiceProvider;
    public AppDbContext Context { get; }

    public async Task<AppUser> AddUserAsync(string userId, bool isBanned = false, bool emailConfirmed = true)
    {
        var user = new AppUser
        {
            Id = userId,
            UserName = $"{userId}@example.test",
            Email = $"{userId}@example.test",
            EmailConfirmed = emailConfirmed,
            IsBanned = isBanned
        };
        Context.Users.Add(user);
        await Context.SaveChangesAsync();
        return user;
    }

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
        _connection.Dispose();
    }
}
