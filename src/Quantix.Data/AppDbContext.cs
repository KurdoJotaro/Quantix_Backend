using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Quantix.Core.Entities;

namespace Quantix.Data;

public class AppDbContext : IdentityDbContext<AppUser, AppRole, string>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Transaction>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Amount).HasColumnType("decimal(18,2)");
            e.HasOne(t => t.AppUser)
             .WithMany(u => u.Transactions)
             .HasForeignKey(t => t.AppUserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(t => t.Category)
             .WithMany(c => c.Transactions)
             .HasForeignKey(t => t.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(t => !t.IsDeleted);
        });

        builder.Entity<Category>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).HasMaxLength(100).IsRequired();
            e.HasQueryFilter(c => !c.IsDeleted);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
