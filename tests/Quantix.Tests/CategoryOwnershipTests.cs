using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quantix.API.Controllers;
using Quantix.Core.DTOs;
using Quantix.Core.Entities;
using Quantix.Core.Enums;
using Quantix.Data;
using Quantix.Service;
using Xunit;

namespace Quantix.Tests;

public class CategoryOwnershipTests
{
    [Fact]
    public async Task AuthenticatedRegularUserPassesCreateAuthorizationAndOwnsCreatedCategory()
    {
        using var database = new TestDatabase();
        await database.AddUserAsync("regular-user");
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "regular-user"),
            new Claim(ClaimTypes.Role, "User")
        }, "test"));
        using var authorizationServices = new ServiceCollection()
            .AddLogging()
            .AddAuthorization()
            .BuildServiceProvider();
        var metadata = typeof(CategoriesController).GetCustomAttributes(true).OfType<IAuthorizeData>()
            .Concat(typeof(CategoriesController).GetMethod(nameof(CategoriesController.Create))!
                .GetCustomAttributes(true).OfType<IAuthorizeData>())
            .ToArray();
        var policy = await AuthorizationPolicy.CombineAsync(
            authorizationServices.GetRequiredService<IAuthorizationPolicyProvider>(), metadata);
        Assert.NotNull(policy);
        var authorization = authorizationServices.GetRequiredService<IAuthorizationService>();

        var authorized = await authorization.AuthorizeAsync(principal, null, policy);
        var anonymous = await authorization.AuthorizeAsync(new ClaimsPrincipal(new ClaimsIdentity()), null, policy);

        Assert.True(authorized.Succeeded);
        Assert.False(anonymous.Succeeded);
        Assert.False(principal.IsInRole("Admin"));
        var controller = new CategoriesController(new CategoryService(new UnitOfWork(database.Context)))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            }
        };
        var result = await controller.Create(new CreateCategoryDto
        {
            Name = "My category",
            Type = CategoryType.Expense
        });

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<ApiResponse<CategoryDto>>(created.Value);
        Assert.True(response.Success);
        database.Context.ChangeTracker.Clear();
        var stored = await database.Context.Categories.SingleAsync();
        Assert.Equal("regular-user", stored.AppUserId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CreatedCategoryBelongsToCallerIncludingAdministrators(bool isAdmin)
    {
        using var database = new TestDatabase();
        await database.AddUserAsync("creator");
        if (isAdmin)
        {
            database.Context.Roles.Add(new AppRole { Id = "admin-role", Name = "Admin", NormalizedName = "ADMIN" });
            database.Context.UserRoles.Add(new IdentityUserRole<string> { UserId = "creator", RoleId = "admin-role" });
            await database.Context.SaveChangesAsync();
        }
        var service = new CategoryService(new UnitOfWork(database.Context));

        var response = await service.CreateAsync(new CreateCategoryDto
        {
            Name = "Kişisel market",
            Type = CategoryType.Expense,
            IconCode = "shopping_cart"
        }, "creator");

        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.False(response.Data.IsShared);
        Assert.True(response.Data.CanManage);
        database.Context.ChangeTracker.Clear();
        var stored = await database.Context.Categories.SingleAsync();
        Assert.Equal("creator", stored.AppUserId);
        Assert.Equal(response.Data.Id, stored.Id);
        Assert.Equal("Kişisel market", stored.Name);
        Assert.Equal(CategoryType.Expense, stored.Type);
        Assert.Equal("shopping_cart", stored.IconCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ListingShowsOnlyOwnAndSharedCategoriesWithCallerPermissions(bool isAdmin)
    {
        using var database = new TestDatabase();
        await database.AddUserAsync("viewer");
        await database.AddUserAsync("other");
        var own = new Category { Name = "Own", AppUserId = "viewer" };
        var shared = new Category { Name = "Shared", AppUserId = null };
        var foreign = new Category { Name = "Private", AppUserId = "other" };
        database.Context.Categories.AddRange(own, shared, foreign,
            new Category { Name = "Deleted own", AppUserId = "viewer", IsDeleted = true },
            new Category { Name = "Deleted shared", AppUserId = null, IsDeleted = true });
        await database.Context.SaveChangesAsync();
        var service = new CategoryService(new UnitOfWork(database.Context));

        var response = await service.GetAllAsync("viewer", isAdmin);

        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        var listed = response.Data.ToList();
        Assert.Equal(2, listed.Count);
        var ownDto = Assert.Single(listed, category => category.Id == own.Id);
        Assert.False(ownDto.IsShared);
        Assert.True(ownDto.CanManage);
        var sharedDto = Assert.Single(listed, category => category.Id == shared.Id);
        Assert.True(sharedDto.IsShared);
        Assert.Equal(isAdmin, sharedDto.CanManage);
        Assert.DoesNotContain(listed, category => category.Id == foreign.Id);
    }

    [Fact]
    public async Task OwnerCanDeleteUnusedPrivateCategory()
    {
        using var database = new TestDatabase();
        await database.AddUserAsync("owner");
        var category = new Category { Name = "Own", AppUserId = "owner" };
        database.Context.Categories.Add(category);
        await database.Context.SaveChangesAsync();
        var service = new CategoryService(new UnitOfWork(database.Context));

        var response = await service.DeleteAsync(category.Id, "owner");

        Assert.True(response.Success);
        database.Context.ChangeTracker.Clear();
        Assert.Empty(await database.Context.Categories.ToListAsync());
        var stored = await database.Context.Categories.IgnoreQueryFilters().SingleAsync();
        Assert.True(stored.IsDeleted);
        Assert.Equal("owner", stored.AppUserId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AnotherUsersPrivateCategoryCannotBeDeletedEvenByAdministrator(bool isAdmin)
    {
        using var database = new TestDatabase();
        await database.AddUserAsync("owner");
        await database.AddUserAsync("caller");
        var category = new Category { Name = "Private", AppUserId = "owner" };
        database.Context.Categories.Add(category);
        await database.Context.SaveChangesAsync();
        var service = new CategoryService(new UnitOfWork(database.Context));

        var response = await service.DeleteAsync(category.Id, "caller", isAdmin);
        await database.Context.SaveChangesAsync();

        Assert.False(response.Success);
        database.Context.ChangeTracker.Clear();
        var stored = await database.Context.Categories.SingleAsync();
        Assert.False(stored.IsDeleted);
        Assert.Equal("owner", stored.AppUserId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OnlyAdministratorCanDeleteUnusedSharedCategory(bool isAdmin)
    {
        using var database = new TestDatabase();
        await database.AddUserAsync("caller");
        var category = new Category { Name = "Legacy shared", AppUserId = null };
        database.Context.Categories.Add(category);
        await database.Context.SaveChangesAsync();
        var service = new CategoryService(new UnitOfWork(database.Context));

        var response = await service.DeleteAsync(category.Id, "caller", isAdmin);

        Assert.Equal(isAdmin, response.Success);
        database.Context.ChangeTracker.Clear();
        var stored = await database.Context.Categories.IgnoreQueryFilters().SingleAsync();
        Assert.Equal(isAdmin, stored.IsDeleted);
        Assert.Null(stored.AppUserId);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task TransactionHistoryPreventsAuthorizedCategoryDeletion(bool isShared, bool deletedTransaction)
    {
        using var database = new TestDatabase();
        await database.AddUserAsync("caller");
        await database.AddUserAsync("other");
        var category = new Category { Name = "History", AppUserId = isShared ? null : "caller" };
        database.Context.Transactions.Add(new Transaction
        {
            AppUserId = isShared ? "other" : "caller",
            Category = category,
            Amount = 10,
            Date = DateTime.UtcNow,
            IsDeleted = deletedTransaction
        });
        await database.Context.SaveChangesAsync();
        var service = new CategoryService(new UnitOfWork(database.Context));

        var response = await service.DeleteAsync(category.Id, "caller", isAdmin: isShared);

        Assert.False(response.Success);
        database.Context.ChangeTracker.Clear();
        Assert.False((await database.Context.Categories.SingleAsync()).IsDeleted);
        var storedTransaction = await database.Context.Transactions.IgnoreQueryFilters().SingleAsync();
        Assert.Equal(deletedTransaction, storedTransaction.IsDeleted);
        Assert.Equal(category.Id, storedTransaction.CategoryId);
    }

    [Fact]
    public async Task MissingCategoryCannotBeDeleted()
    {
        using var database = new TestDatabase();
        await database.AddUserAsync("caller");
        var service = new CategoryService(new UnitOfWork(database.Context));

        var response = await service.DeleteAsync(int.MaxValue, "caller", isAdmin: true);

        Assert.False(response.Success);
        Assert.Empty(await database.Context.Categories.IgnoreQueryFilters().ToListAsync());
    }

    [Fact]
    public async Task TransactionCannotBeCreatedWithAnotherUsersPrivateCategory()
    {
        using var database = new TestDatabase();
        await database.AddUserAsync("owner");
        await database.AddUserAsync("caller");
        var category = new Category { Name = "Private", AppUserId = "owner" };
        database.Context.Categories.Add(category);
        await database.Context.SaveChangesAsync();
        var service = new TransactionService(new UnitOfWork(database.Context));

        var response = await service.CreateAsync(new CreateTransactionDto
        {
            Amount = 25,
            Date = DateTime.UtcNow,
            CategoryId = category.Id
        }, "caller");
        await database.Context.SaveChangesAsync();

        Assert.False(response.Success);
        database.Context.ChangeTracker.Clear();
        Assert.Empty(await database.Context.Transactions.IgnoreQueryFilters().ToListAsync());
        Assert.Equal("owner", (await database.Context.Categories.SingleAsync()).AppUserId);
    }

    [Fact]
    public async Task PrivateForeignCategoryCannotChangeAnyExistingTransactionFields()
    {
        using var database = new TestDatabase();
        await database.AddUserAsync("owner");
        await database.AddUserAsync("caller");
        var ownCategory = new Category { Name = "Own", AppUserId = "caller" };
        var foreignCategory = new Category { Name = "Foreign", AppUserId = "owner" };
        var originalDate = new DateTime(2026, 1, 2, 12, 0, 0, DateTimeKind.Utc);
        var transaction = new Transaction
        {
            AppUserId = "caller",
            Category = ownCategory,
            Amount = 25,
            Description = "Original description",
            Date = originalDate
        };
        database.Context.Transactions.Add(transaction);
        database.Context.Categories.Add(foreignCategory);
        await database.Context.SaveChangesAsync();
        var service = new TransactionService(new UnitOfWork(database.Context));

        var response = await service.UpdateAsync(transaction.Id, new UpdateTransactionDto
        {
            Amount = 999,
            Description = "Changed description",
            Date = originalDate.AddDays(10),
            CategoryId = foreignCategory.Id
        }, "caller");
        await database.Context.SaveChangesAsync();

        Assert.False(response.Success);
        database.Context.ChangeTracker.Clear();
        var stored = await database.Context.Transactions.SingleAsync();
        Assert.Equal(25, stored.Amount);
        Assert.Equal("Original description", stored.Description);
        Assert.Equal(originalDate, stored.Date);
        Assert.Equal(ownCategory.Id, stored.CategoryId);
        Assert.Equal("caller", stored.AppUserId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TransactionCanBeCreatedWithOwnOrSharedCategory(bool isShared)
    {
        using var database = new TestDatabase();
        await database.AddUserAsync("caller");
        var category = new Category { Name = "Available", AppUserId = isShared ? null : "caller", Type = CategoryType.Expense };
        database.Context.Categories.Add(category);
        await database.Context.SaveChangesAsync();
        var service = new TransactionService(new UnitOfWork(database.Context));

        var response = await service.CreateAsync(new CreateTransactionDto
        {
            Amount = 25.50m,
            Date = DateTime.UtcNow,
            CategoryId = category.Id
        }, "caller");

        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(category.Id, response.Data.CategoryId);
        Assert.Equal(CategoryType.Expense, response.Data.CategoryType);
        database.Context.ChangeTracker.Clear();
        var stored = await database.Context.Transactions.SingleAsync();
        Assert.Equal("caller", stored.AppUserId);
        Assert.Equal(category.Id, stored.CategoryId);
        Assert.Equal(25.50m, stored.Amount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TransactionCanBeMovedToOwnOrSharedCategory(bool isShared)
    {
        using var database = new TestDatabase();
        await database.AddUserAsync("caller");
        var targetCategory = new Category { Name = "Target", AppUserId = isShared ? null : "caller", Type = CategoryType.Income };
        var transaction = new Transaction
        {
            AppUserId = "caller",
            Category = new Category { Name = "Original", AppUserId = "caller", Type = CategoryType.Expense },
            Amount = 10,
            Date = DateTime.UtcNow
        };
        database.Context.Transactions.Add(transaction);
        database.Context.Categories.Add(targetCategory);
        await database.Context.SaveChangesAsync();
        var service = new TransactionService(new UnitOfWork(database.Context));

        var response = await service.UpdateAsync(transaction.Id, new UpdateTransactionDto
        {
            Amount = 50,
            Description = "Updated",
            Date = transaction.Date,
            CategoryId = targetCategory.Id
        }, "caller");

        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(targetCategory.Id, response.Data.CategoryId);
        Assert.Equal(CategoryType.Income, response.Data.CategoryType);
        database.Context.ChangeTracker.Clear();
        var stored = await database.Context.Transactions.SingleAsync();
        Assert.Equal("caller", stored.AppUserId);
        Assert.Equal(targetCategory.Id, stored.CategoryId);
        Assert.Equal(50, stored.Amount);
        Assert.Equal("Updated", stored.Description);
    }
}
