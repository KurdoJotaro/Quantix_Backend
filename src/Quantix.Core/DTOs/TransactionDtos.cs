using System.ComponentModel.DataAnnotations;
using Quantix.Core.Enums;

namespace Quantix.Core.DTOs;

public class TransactionDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateTime Date { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public CategoryType CategoryType { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateTransactionDto : IValidatableObject
{
    [Range(typeof(decimal), "0.01", "9999999999999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal Amount { get; set; }

    public string? Description { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Amount != decimal.Round(Amount, 2))
            yield return new ValidationResult("Tutar en fazla iki ondalık basamak içerebilir.", new[] { nameof(Amount) });

        if (Date == default)
            yield return new ValidationResult("İşlem tarihi zorunludur.", new[] { nameof(Date) });
    }
}

public class UpdateTransactionDto : CreateTransactionDto
{
}
