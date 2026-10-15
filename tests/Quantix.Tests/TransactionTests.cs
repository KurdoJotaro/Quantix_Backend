using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Quantix.Core.DTOs;
using Quantix.Core.Entities;
using Quantix.Core.Enums;
using Quantix.Data;
using Quantix.Service;
using Xunit;

namespace Quantix.Tests;

public class TransactionTests
{
    [Theory]
    [InlineData("tr-TR")]
    [InlineData("en-US")]
    public void ValidMoneyIsAcceptedRegardlessOfServerCulture(string cultureName)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(cultureName);
            var transaction = new CreateTransactionDto { Amount = 123.45m, CategoryId = 1, Date = DateTime.UtcNow };
            Assert.True(Validator.TryValidateObject(transaction, new ValidationContext(transaction), new List<ValidationResult>(), true));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1.001")]
    [InlineData("10000000000000000")]
    public void InvalidMoneyAmountsAreRejected(string amount)
    {
        var transaction = new CreateTransactionDto
        {
            Amount = decimal.Parse(amount, CultureInfo.InvariantCulture),
            Date = DateTime.UtcNow,
            CategoryId = 1
        };
        var errors = new List<ValidationResult>();

        Assert.False(Validator.TryValidateObject(transaction, new ValidationContext(transaction), errors, true));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(transaction.Amount)));
    }

    [Fact]
    public void MissingDateIsRejected()
    {
        var transaction = new UpdateTransactionDto { Amount = 10, CategoryId = 1 };
        var errors = new List<ValidationResult>();

        Assert.False(Validator.TryValidateObject(transaction, new ValidationContext(transaction), errors, true));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(transaction.Date)));
    }

    [Fact]
    public void UnknownCategoryTypeIsRejected()
    {
        var category = new CreateCategoryDto { Name = "Invalid", Type = (CategoryType)99 };
        Assert.False(Validator.TryValidateObject(category, new ValidationContext(category), new List<ValidationResult>(), true));
    }

    [Fact]
    public async Task ExpenseRetainsPositiveAmountAndReturnsCategoryType()
    {
        using var database = new TestDatabase();
        await database.AddUserAsync("owner");
        var category = new Category { Name = "Market", Type = CategoryType.Expense };
        database.Context.Categories.Add(category);
        await database.Context.SaveChangesAsync();
        var service = new TransactionService(new UnitOfWork(database.Context));

        var response = await service.CreateAsync(new CreateTransactionDto
        {
            Amount = 123.45m,
            Date = DateTime.UtcNow,
            CategoryId = category.Id
        }, "owner");
        var listed = await service.GetUserTransactionsAsync("owner");
        var fetched = await service.GetByIdAsync(response.Data!.Id, "owner");

        Assert.True(response.Success);
        Assert.Equal(123.45m, response.Data.Amount);
        Assert.Equal(CategoryType.Expense, response.Data.CategoryType);
        Assert.Equal(CategoryType.Expense, Assert.Single(listed.Data!).CategoryType);
        Assert.Equal(CategoryType.Expense, fetched.Data!.CategoryType);
    }

    [Fact]
    public async Task MissingCategoryCannotChangeAnExistingTransaction()
    {
        using var database = new TestDatabase();
        await database.AddUserAsync("owner");
        var category = new Category { Name = "Salary", Type = CategoryType.Income };
        var transaction = new Transaction { AppUserId = "owner", Category = category, Amount = 50, Date = DateTime.UtcNow };
        database.Context.Transactions.Add(transaction);
        await database.Context.SaveChangesAsync();
        var service = new TransactionService(new UnitOfWork(database.Context));

        var response = await service.UpdateAsync(transaction.Id, new UpdateTransactionDto
        {
            Amount = 100,
            Date = DateTime.UtcNow,
            CategoryId = int.MaxValue
        }, "owner");
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();
        var stored = await database.Context.Transactions.SingleAsync();

        Assert.False(response.Success);
        Assert.Equal(50, stored.Amount);
        Assert.Equal(category.Id, stored.CategoryId);
    }

    [Fact]
    public async Task DeletedTrackedCategoryCannotBeUsedForNewTransactions()
    {
        using var database = new TestDatabase();
        await database.AddUserAsync("owner");
        var category = new Category { Name = "Deleted", IsDeleted = true };
        database.Context.Categories.Add(category);
        await database.Context.SaveChangesAsync();
        var service = new TransactionService(new UnitOfWork(database.Context));

        var response = await service.CreateAsync(new CreateTransactionDto
        {
            Amount = 10,
            Date = DateTime.UtcNow,
            CategoryId = category.Id
        }, "owner");

        Assert.False(response.Success);
        Assert.Empty(await database.Context.Transactions.ToListAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CategoryWithTransactionHistoryCannotBeDeleted(bool deletedTransaction)
    {
        using var database = new TestDatabase();
        await database.AddUserAsync("owner");
        var category = new Category { Name = "History", Type = CategoryType.Expense };
        var transaction = new Transaction
        {
            AppUserId = "owner", Category = category, Amount = 10, Date = DateTime.UtcNow, IsDeleted = deletedTransaction
        };
        database.Context.Transactions.Add(transaction);
        await database.Context.SaveChangesAsync();
        var service = new CategoryService(new UnitOfWork(database.Context));

        var response = await service.DeleteAsync(category.Id);

        Assert.False(response.Success);
        Assert.False(category.IsDeleted);
        Assert.Single(await database.Context.Categories.ToListAsync());
    }

    [Fact]
    public async Task AnotherUserCannotReadUpdateOrDeleteATransaction()
    {
        using var database = new TestDatabase();
        await database.AddUserAsync("owner");
        await database.AddUserAsync("other");
        var transaction = new Transaction
        {
            AppUserId = "owner", Category = new Category { Name = "Private" }, Amount = 25, Date = DateTime.UtcNow
        };
        database.Context.Transactions.Add(transaction);
        await database.Context.SaveChangesAsync();
        var service = new TransactionService(new UnitOfWork(database.Context));

        Assert.False((await service.GetByIdAsync(transaction.Id, "other")).Success);
        Assert.Empty((await service.GetUserTransactionsAsync("other")).Data!);
        Assert.False((await service.UpdateAsync(transaction.Id, new UpdateTransactionDto
        {
            Amount = 999, CategoryId = transaction.CategoryId, Date = DateTime.UtcNow
        }, "other")).Success);
        Assert.False((await service.DeleteAsync(transaction.Id, "other")).Success);
        Assert.Equal(25, transaction.Amount);
        Assert.False(transaction.IsDeleted);
    }
}
