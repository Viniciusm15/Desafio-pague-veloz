using Microsoft.AspNetCore.Mvc;
using PagueVeloz.Application.DTOs.Requests.Account;
using PagueVeloz.Application.DTOs.Responses;
using PagueVeloz.Application.Interfaces;
using PagueVeloz.Domain.Enums;

namespace PagueVeloz.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAccountRequest request)
    {
        var account = await _accountService.OpenAccountAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = account.Id }, account);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var account = await _accountService.GetByIdAsync(id);
        return account is null ? NotFound() : Ok(account);
    }

    [HttpPost("{id}/block")]
    public async Task<IActionResult> Block(Guid id) => Ok(await _accountService.BlockAsync(id));

    [HttpPost("{id}/reactivate")]
    public async Task<IActionResult> Reactivate(Guid id) => Ok(await _accountService.ReactivateAsync(id));

    [HttpPost("{id}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id) => Ok(await _accountService.DeactivateAsync(id));

    [HttpPost("{id}/credit")]
    public async Task<IActionResult> Credit(Guid id, [FromBody] CreditAccountRequest request)
    {
        var (account, operation) = await _accountService.CreditAsync(id, request);
        var response = TransactionResponse.From(account, operation);

        return operation.Status == OperationStatus.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost("{id}/debit")]
    public async Task<IActionResult> Debit(Guid id, [FromBody] DebitAccountRequest request)
    {
        var (account, operation) = await _accountService.DebitAsync(id, request);
        var response = TransactionResponse.From(account, operation);

        return operation.Status == OperationStatus.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost("{id}/reserve")]
    public async Task<IActionResult> Reserve(Guid id, [FromBody] ReserveAccountRequest request)
    {
        var (account, operation) = await _accountService.ReserveAsync(id, request);
        var response = TransactionResponse.From(account, operation);

        return operation.Status == OperationStatus.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost("{id}/capture")]
    public async Task<IActionResult> Capture(Guid id, [FromBody] CaptureAccountRequest request)
    {
        var (account, operation) = await _accountService.CaptureAsync(id, request);
        var response = TransactionResponse.From(account, operation);

        return operation.Status == OperationStatus.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost("{id}/reversal")]
    public async Task<IActionResult> Reversal(Guid id, [FromBody] ReversalAccountRequest request)
    {
        var (account, operation, otherAccount, otherOperation) = await _accountService.ReversalAsync(id, request);

        if (otherAccount is not null && otherOperation is not null)
        {
            var pairedResponse = new
            {
                Account = TransactionResponse.From(account, operation),
                PairedAccount = TransactionResponse.From(otherAccount, otherOperation)
            };

            var success = operation.Status == OperationStatus.Success && otherOperation.Status == OperationStatus.Success;
            return success ? Ok(pairedResponse) : BadRequest(pairedResponse);
        }

        var response = TransactionResponse.From(account, operation);
        return operation.Status == OperationStatus.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer([FromBody] TransferAccountRequest request)
    {
        var (source, sourceOp, destination, destinationOp) = await _accountService.TransferAsync(request);

        var response = new
        {
            Source = TransactionResponse.From(source, sourceOp),
            Destination = TransactionResponse.From(destination, destinationOp)
        };

        var success = sourceOp.Status == OperationStatus.Success && destinationOp.Status == OperationStatus.Success;
        return success ? Ok(response) : BadRequest(response);
    }
}
