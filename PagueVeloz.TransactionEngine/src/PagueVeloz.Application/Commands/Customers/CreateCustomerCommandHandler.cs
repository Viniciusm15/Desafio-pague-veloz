using MediatR;
using Microsoft.Extensions.Logging;
using PagueVeloz.Application.DTOs.Customers.Responses;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Interfaces;

namespace PagueVeloz.Application.Commands.Customers;

public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, CustomerResponse>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateCustomerCommandHandler> _logger;

    public CreateCustomerCommandHandler(
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateCustomerCommandHandler> logger)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CustomerResponse> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Creating customer. Name {Name}, Document {Document}",
            request.Name,
            request.Document);

        var customer = Customer.Create(request.Name, request.Document);

        await _customerRepository.AddAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Customer created successfully. CustomerId {CustomerId}, Name {Name}, Document {Document}",
            customer.Id,
            customer.Name,
            customer.Document);

        return CustomerResponse.From(customer);
    }
}
