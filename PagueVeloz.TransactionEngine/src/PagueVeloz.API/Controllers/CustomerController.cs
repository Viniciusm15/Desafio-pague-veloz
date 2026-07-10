using MediatR;
using Microsoft.AspNetCore.Mvc;
using PagueVeloz.Application.Commands.Customers;
using PagueVeloz.Application.Queries.Customers;

namespace PagueVeloz.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomerController : ControllerBase
{
    private readonly IMediator _mediator;

    public CustomerController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerCommand command, CancellationToken cancellationToken)
    {
        var customer = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var customer = await _mediator.Send(new GetCustomerQuery(id), cancellationToken);
        if (customer is null) return NotFound();
        return Ok(customer);
    }
}
