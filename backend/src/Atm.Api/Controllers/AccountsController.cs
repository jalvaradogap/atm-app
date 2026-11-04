using System.ComponentModel.DataAnnotations;
using Atm.Api.Requests;
using Atm.Application.Accounts;
using Atm.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Atm.Api.Controllers;

[ApiController]
[Route("api/accounts")]
public sealed class AccountsController(AccountService accounts) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AccountDto>>> List(CancellationToken cancellationToken) =>
        Ok(await accounts.GetAccountsAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AccountDto>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await accounts.GetAccountAsync(id, cancellationToken));

    [HttpGet("{id:guid}/transactions")]
    public async Task<ActionResult<PagedResult<TransactionDto>>> Transactions(
        Guid id,
        [FromQuery] [Range(1, int.MaxValue)] int page,
        [FromQuery] [Range(1, AccountService.MaxPageSize)] int pageSize,
        CancellationToken cancellationToken) =>
        Ok(await accounts.GetTransactionsAsync(id, page, pageSize, cancellationToken));

    [HttpPost("{id:guid}/deposits")]
    public async Task<ActionResult<AccountDto>> Deposit(Guid id, [FromBody] AmountRequest request, CancellationToken cancellationToken) =>
        Ok(await accounts.DepositAsync(id, request.Amount, request.Description, cancellationToken));

    [HttpPost("{id:guid}/withdrawals")]
    public async Task<ActionResult<AccountDto>> Withdraw(Guid id, [FromBody] AmountRequest request, CancellationToken cancellationToken) =>
        Ok(await accounts.WithdrawAsync(id, request.Amount, request.Description, cancellationToken));
}
