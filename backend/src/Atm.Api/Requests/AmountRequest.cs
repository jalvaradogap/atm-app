using System.ComponentModel.DataAnnotations;
using Atm.Application.Accounts;

namespace Atm.Api.Requests;

/// <summary>
/// Body of a deposit or withdrawal. The range check mirrors the domain rule so that obviously bad input
/// gets a 400 before any work is done. The domain still enforces the rule and stays the authority.
/// </summary>
public sealed record AmountRequest(
    [Range(typeof(decimal), "0.01", "1000000", ErrorMessage = "Amount must be between 0.01 and 1,000,000.")]
    decimal Amount,
    [MaxLength(200)] string? Description);
