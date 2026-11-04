using System.ComponentModel.DataAnnotations;

namespace Atm.Api.Requests;

public sealed record TransferRequest(
    [Required] Guid? FromAccountId,
    [Required] Guid? ToAccountId,
    [Range(typeof(decimal), "0.01", "1000000", ErrorMessage = "Amount must be between 0.01 and 1,000,000.")]
    decimal Amount,
    [MaxLength(200)] string? Description);
