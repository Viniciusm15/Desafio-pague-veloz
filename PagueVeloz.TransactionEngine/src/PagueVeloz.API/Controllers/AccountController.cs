using Microsoft.AspNetCore.Mvc;
using PagueVeloz.Application.DTOs.Requests;
using PagueVeloz.Application.Exceptions;
using PagueVeloz.Application.Interfaces;

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
        try
        {
            var account = await _accountService.OpenAccountAsync(request.CustomerId, request.CreditLimit);
            return CreatedAtAction(nameof(GetById), new { id = account.Id }, account);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var account = await _accountService.GetByIdAsync(id);

        if (account is null)
            return NotFound();

        return Ok(account);
    }

    [HttpPost("{id}/block")]
    public async Task<IActionResult> Block(Guid id)
    {
        var account = await _accountService.BlockAsync(id);
        return Ok(account);
    }

    [HttpPost("{id}/reactivate")]
    public async Task<IActionResult> Reactivate(Guid id)
    {
        var account = await _accountService.ReactivateAsync(id);
        return Ok(account);
    }

    [HttpPost("{id}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var account = await _accountService.DeactivateAsync(id);
        return Ok(account);
    }

    [HttpPost("{id}/credit")]
    public async Task<IActionResult> Credit(Guid id, [FromBody] CreditAccountRequest request)
    {
        try
        {
            var account = await _accountService.CreditAsync(id, request.Amount, request.ReferenceId);
            return Ok(new
            {
                account.Id,
                account.AvailableBalance,
                Message = $"Credit of {request.Amount:C} completed successfully."
            });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/debit")]
    public async Task<IActionResult> Debit(Guid id, [FromBody] DebitAccountRequest request)
    {
        try
        {
            var account = await _accountService.DebitAsync(id, request.Amount, request.ReferenceId);
            return Ok(new
            {
                account.Id,
                account.AvailableBalance,
                Message = $"Debit of {request.Amount:C} completed successfully."
            });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/reserve")]
    public async Task<IActionResult> Reserve(Guid id, [FromBody] ReserveAccountRequest request)
    {
        try
        {
            var account = await _accountService.ReserveAsync(id, request.Amount, request.ReferenceId);

            return Ok(new
            {
                account.Id,
                account.CustomerId,
                account.AvailableBalance,
                account.ReservedBalance,
                Message = $"Reserve of {request.Amount:C} completed successfully.",
                Operations = account.Operations.Select(o => new
                {
                    o.Id,
                    o.AccountId,
                    o.Type,
                    o.Amount,
                    o.OccurredAt
                })
            });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/capture")]
    public async Task<IActionResult> Capture(Guid id, [FromBody] CaptureAccountRequest request)
    {
        try
        {
            var account = await _accountService.CaptureAsync(id, request.ReserveOperationId, request.ReferenceId);
            return Ok(new
            {
                account.Id,
                account.AvailableBalance,
                account.ReservedBalance,
                Message = "Capture completed successfully."
            });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/reversal")]
    public async Task<IActionResult> Reversal(Guid id, [FromBody] ReversalAccountRequest request)
    {
        try
        {
            var account = await _accountService.ReversalAsync(id, request.OriginalOperationId, request.ReferenceId);

            return Ok(new
            {
                account.Id,
                account.AvailableBalance,
                account.ReservedBalance,
                Message = "Reversal completed successfully."
            });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer([FromBody] TransferRequest request)
    {
        try
        {
            var (source, destination) = await _accountService.TransferAsync(
                request.SourceAccountId, request.DestinationAccountId, request.Amount, request.ReferenceId);

            return Ok(new
            {
                Source = new { source.Id, source.AvailableBalance },
                Destination = new { destination.Id, destination.AvailableBalance },
                Message = $"Transfer of {request.Amount:C} completed successfully."
            });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
