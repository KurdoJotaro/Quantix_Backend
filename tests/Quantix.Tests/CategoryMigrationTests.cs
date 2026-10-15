using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Quantix.Data;
using Quantix.Data.Migrations;
using Xunit;

namespace Quantix.Tests;

public class CategoryMigrationTests
{
    [Fact]
    public void OwnershipMigrationOnlyAddsNullableOwnerIndexAndRestrictedForeignKey()
    {
        var migration = new AddCategoryOwnership();

        Assert.Collection(migration.UpOperations,
            operation =>
            {
                var column = Assert.IsType<AddColumnOperation>(operation);
                Assert.Equal("Categories", column.Table);
                Assert.Equal("AppUserId", column.Name);
                Assert.True(column.IsNullable);
                Assert.Null(column.DefaultValue);
            },
            operation =>
            {
                var index = Assert.IsType<CreateIndexOperation>(operation);
                Assert.Equal("Categories", index.Table);
                Assert.Equal(new[] { "AppUserId" }, index.Columns);
            },
            operation =>
            {
                var foreignKey = Assert.IsType<AddForeignKeyOperation>(operation);
                Assert.Equal("Categories", foreignKey.Table);
                Assert.Equal("AspNetUsers", foreignKey.PrincipalTable);
                Assert.Equal(ReferentialAction.Restrict, foreignKey.OnDelete);
            });
    }

    [Fact]
    public void MySqlModelMatchesTheGeneratedMigrationSnapshotWithoutConnecting()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql("Server=127.0.0.1;Port=1;Database=QuantixDesignOnly;User=unused;Password=unused;",
                new MySqlServerVersion(new Version(8, 0, 0)))
            .Options;
        using var context = new AppDbContext(options);

        Assert.False(context.Database.HasPendingModelChanges());
    }
}
