using Quantix.Core.Enums;

namespace Quantix.Core.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public CategoryType Type { get; set; }
    public string? IconCode { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
