namespace Quantix.Core.Entities;

public class Transaction : BaseEntity
{
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateTime Date { get; set; }

    public string AppUserId { get; set; } = string.Empty;
    public int CategoryId { get; set; }

    public AppUser AppUser { get; set; } = null!;
    public Category Category { get; set; } = null!;
}
