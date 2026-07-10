using MediatR;
using Microsoft.AspNetCore.Mvc;
using PagueVeloz.Application.Commands.Accounts;
using PagueVeloz.Application.Commands.Transactions;
using PagueVeloz.Application.DTOs.Transactions.Requests;
using PagueVeloz.Application.DTOs.Transactions.Responses;
using PagueVeloz.Application.Queries.Accounts;
using PagueVeloz.Domain.Enums;

namespace PagueVeloz.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly IMediator _mediator;

    public AccountController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] OpenAccountCommand command, CancellationToken cancellationToken)
    {
        var account = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = account.Id }, account);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var account = await _mediator.Send(new GetAccountQuery(id), cancellationToken);
        if (account is null) return NotFound();
        return Ok(account);
    }

    [HttpPost("{id}/block")]
    public async Task<IActionResult> Block(Guid id, CancellationToken cancellationToken)
    {
        var account = await _mediator.Send(new BlockAccountCommand(id), cancellationToken);
        return Ok(account);
    }

    [HttpPost("{id}/reactivate")]
    public async Task<IActionResult> Reactivate(Guid id, CancellationToken cancellationToken)
    {
        var account = await _mediator.Send(new ReactivateAccountCommand(id), cancellationToken);
        return Ok(account);
    }

    [HttpPost("{id}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var account = await _mediator.Send(new DeactivateAccountCommand(id), cancellationToken);
        return Ok(account);
    }

    [HttpPost("transactions")]
    public async Task<IActionResult> Execute([FromBody] TransactionRequest request, CancellationToken cancellationToken)
    {
        IRequest<TransactionResponse> command = request.Operation switch
        {
            OperationType.Credit =>
                new CreditCommand(request.AccountId, request.Amount, request.ReferenceId, request.Currency, request.Metadata),
            OperationType.Debit =>
                new DebitCommand(request.AccountId, request.Amount, request.ReferenceId, request.Currency, request.Metadata),
            OperationType.Reserve =>
                new ReserveCommand(request.AccountId, request.Amount, request.ReferenceId, request.Currency, request.Metadata),
            OperationType.Capture =>
                new CaptureCommand(request.AccountId, request.ReserveOperationId!.Value, request.ReferenceId, request.Currency, request.Metadata),
            OperationType.Reversal =>
                new ReversalCommand(request.AccountId, request.OriginalOperationId!.Value, request.ReferenceId, request.Currency, request.Metadata),
            OperationType.Transfer =>
                new TransferCommand(request.AccountId, request.DestinationAccountId!.Value, request.Amount, request.ReferenceId, request.Currency, request.Metadata),
            _ => throw new ArgumentException($"Unsupported operation: {request.Operation}")
        };

        var response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }
}
