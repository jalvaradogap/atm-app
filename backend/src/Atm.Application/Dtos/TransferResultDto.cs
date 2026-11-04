namespace Atm.Application.Dtos;

/// <summary>Both legs of a transfer share <see cref="TransferId"/>. Both balances are returned after the transfer.</summary>
public sealed record TransferResultDto(Guid TransferId, AccountDto From, AccountDto To);
