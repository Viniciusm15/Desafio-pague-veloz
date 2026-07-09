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

    [HttpPost("transactions")]
    public async Task<IActionResult> Execute([FromBody] TransactionRequest request)
    {
        switch (request.Operation)
        {
            case OperationType.Credit:
                var creditResult = await _accountService.CreditAsync(request.AccountId,
                    new CreditAccountRequest(
                        request.Amount,
                        request.ReferenceId,
                        request.Currency,
                        request.Metadata));
                return Ok(TransactionResponse.From(creditResult.Account, creditResult.Operation));

            case OperationType.Debit:
                var debitResult = await _accountService.DebitAsync(request.AccountId,
                    new DebitAccountRequest(
                        request.Amount,
                        request.ReferenceId,
                        request.Currency,
                        request.Metadata));
                return Ok(TransactionResponse.From(debitResult.Account, debitResult.Operation));

            case OperationType.Reserve:
                var reserveResult = await _accountService.ReserveAsync(request.AccountId,
                    new ReserveAccountRequest(
                        request.Amount,
                        request.ReferenceId,
                        request.Currency,
                        request.Metadata));
                return Ok(TransactionResponse.From(reserveResult.Account, reserveResult.Operation));

            case OperationType.Capture:
                var captureResult = await _accountService.CaptureAsync(request.AccountId,
                    new CaptureAccountRequest(
                        request.ReserveOperationId!.Value,
                        request.ReferenceId,
                        request.Currency,
                        request.Metadata));
                return Ok(TransactionResponse.From(captureResult.Account, captureResult.Operation));

            case OperationType.Reversal:
                var reversalResult = await _accountService.ReversalAsync(request.AccountId,
                    new ReversalAccountRequest(
                        request.OriginalOperationId!.Value,
                        request.ReferenceId,
                        request.Currency,
                        request.Metadata));
                return Ok(TransactionResponse.From(reversalResult.Account, reversalResult.Operation));

            case OperationType.Transfer:
                var transferResult = await _accountService.TransferAsync(
                    new TransferAccountRequest(
                        request.AccountId,
                        request.DestinationAccountId!.Value,
                        request.Amount,
                        request.ReferenceId,
                        request.Currency,
                        request.Metadata));
                return Ok(TransactionResponse.From(transferResult.Account, transferResult.Operation));

            default:
                throw new ArgumentException($"Unsupported operation: {request.Operation}");
        }
    }
}
