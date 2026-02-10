using Microsoft.AspNetCore.Identity;

namespace Quantix.Core.Entities;

public class AppUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public bool IsBanned { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
