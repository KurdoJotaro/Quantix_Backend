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
    public DateTime CreatedAt { get; set; }
}

public class CreateTransactionDto
{
    [Required]
    public decimal Amount { get; set; }

    public string? Description { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Required]
    public int CategoryId { get; set; }
}

public class UpdateTransactionDto
{
    [Required]
    public decimal Amount { get; set; }

    public string? Description { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Required]
    public int CategoryId { get; set; }
}
