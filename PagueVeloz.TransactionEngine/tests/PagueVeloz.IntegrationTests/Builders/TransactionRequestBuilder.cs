using PagueVeloz.Domain.Enums;

namespace PagueVeloz.IntegrationTests.Builders
{
    public class TransactionRequestBuilder
    {
        private OperationType _operation = OperationType.Credit;
        private Guid _accountId = Guid.NewGuid();
        private long _amount = 1000;
        private string _referenceId = Guid.NewGuid().ToString();
        private string _currency = "BRL";
        private Dictionary<string, object>? _metadata;
        private Guid? _reserveOperationId;
        private Guid? _originalOperationId;
        private Guid? _destinationAccountId;

        public TransactionRequestBuilder WithOperation(OperationType operation)
        {
            _operation = operation;
            return this;
        }

        public TransactionRequestBuilder WithAccountId(Guid accountId)
        {
            _accountId = accountId;
            return this;
        }

        public TransactionRequestBuilder WithAmount(long amount)
        {
            _amount = amount;
            return this;
        }

        public TransactionRequestBuilder WithReferenceId(string referenceId)
        {
            _referenceId = referenceId;
            return this;
        }

        public TransactionRequestBuilder WithCurrency(string currency)
        {
            _currency = currency;
            return this;
        }

        public TransactionRequestBuilder WithMetadata(Dictionary<string, object> metadata)
        {
            _metadata = metadata;
            return this;
        }

        public TransactionRequestBuilder WithReserveOperationId(Guid reserveOperationId)
        {
            _reserveOperationId = reserveOperationId;
            return this;
        }

        public TransactionRequestBuilder WithOriginalOperationId(Guid originalOperationId)
        {
            _originalOperationId = originalOperationId;
            return this;
        }

        public TransactionRequestBuilder WithDestinationAccountId(Guid destinationAccountId)
        {
            _destinationAccountId = destinationAccountId;
            return this;
        }

        public object Build()
        {
            var operationName = _operation.ToString().ToLowerInvariant();

            return new
            {
                operation = operationName,
                account_id = _accountId,
                amount = _amount,
                reference_id = _referenceId,
                currency = _currency,
                metadata = _metadata,
                reserve_operation_id = _reserveOperationId,
                original_operation_id = _originalOperationId,
                destination_account_id = _destinationAccountId
            };
        }
    }
}
