using Atm.Api.Requests;
using Atm.Application.Accounts;
using Atm.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Atm.Api.Controllers;

[ApiController]
[Route("api/transfers")]
public sealed class TransfersController(AccountService accounts) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<TransferResultDto>> Transfer([FromBody] TransferRequest request, CancellationToken cancellationToken) =>
        // [Required] on the request guarantees both ids are present once model validation has passed.
        Ok(await accounts.TransferAsync(
            request.FromAccountId!.Value,
            request.ToAccountId!.Value,
            request.Amount,
            request.Description,
            cancellationToken));
}
