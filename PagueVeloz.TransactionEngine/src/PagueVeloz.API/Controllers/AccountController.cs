using Microsoft.AspNetCore.Mvc;
using PagueVeloz.API.Models;
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
            var account = await _accountService.OpenAccountAsync(request.CustomerId);
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
}
