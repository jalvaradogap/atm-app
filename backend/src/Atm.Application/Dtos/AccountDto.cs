namespace Atm.Application.Dtos;

public sealed record AccountDto(Guid Id, string Name, string Number, decimal Balance);
