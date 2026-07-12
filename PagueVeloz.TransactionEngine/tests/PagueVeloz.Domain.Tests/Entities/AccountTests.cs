using FluentAssertions;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Enums;
using PagueVeloz.Domain.Events;

namespace PagueVeloz.Domain.Tests.Entities;

public class AccountTests
{
    private readonly Guid _customerId = Guid.NewGuid();
    private const long CreditLimit = 500;
    private const string Currency = "BRL";

    #region Open

    [Fact]
    public void Open_WithValidCustomerId_ShouldCreateAccountWithZeroBalanceAndActiveStatus()
    {
        // Act
        var account = Account.Open(_customerId, CreditLimit);

        // Assert
        account.Should().NotBeNull();
        account.CustomerId.Should().Be(_customerId);
        account.AvailableBalance.Should().Be(0);
        account.ReservedBalance.Should().Be(0);
        account.CreditLimit.Should().Be(CreditLimit);
        account.Status.Should().Be(AccountStatus.Active);
        account.Operations.Should().BeEmpty();
    }

    [Fact]
    public void Open_WithNegativeCreditLimit_ShouldThrowArgumentException()
    {
        // Act
        Action act = () => Account.Open(_customerId, -100);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Credit limit cannot be negative.");
    }

    #endregion

    #region Credit

    [Fact]
    public void Credit_WithValidAmount_ShouldIncreaseBalanceAndReturnSuccessOperation()
    {
        // Arrange
        var account = Account.Open(_customerId);
        const long amount = 1000;
        const string referenceId = "cred-001";

        // Act
        var operation = account.Credit(amount, referenceId, Currency);

        // Assert
        operation.Status.Should().Be(OperationStatus.Success);
        operation.Amount.Should().Be(amount);
        operation.ReferenceId.Should().Be(referenceId);
        operation.Type.Should().Be(OperationType.Credit);
        account.AvailableBalance.Should().Be(amount);
        account.Operations.Should().Contain(operation);
        account.DomainEvents.Should().ContainSingle(e => e is AccountCreditedEvent);
    }

    [Fact]
    public void Credit_WithSameReferenceId_ShouldReturnExistingOperationAndNotChangeBalance()
    {
        // Arrange
        var account = Account.Open(_customerId);
        const long amount = 1000;
        const string referenceId = "cred-001";

        // Act
        var firstOperation = account.Credit(amount, referenceId, Currency);
        var secondOperation = account.Credit(amount, referenceId, Currency);

        // Assert
        secondOperation.Should().BeSameAs(firstOperation);
        account.AvailableBalance.Should().Be(amount);
        account.Operations.Should().HaveCount(1);
    }

    [Fact]
    public void Credit_WhenAccountBlocked_ShouldReturnFailedOperation()
    {
        // Arrange
        var account = Account.Open(_customerId);
        account.Block();

        // Act
        var operation = account.Credit(100, "cred-002", Currency);

        // Assert
        operation.Status.Should().Be(OperationStatus.Failed);
        operation.FailureReason.Should().Contain("Blocked");
        account.AvailableBalance.Should().Be(0);
        account.Operations.Should().Contain(operation);
        account.DomainEvents.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public void Credit_WithZeroOrNegativeAmount_ShouldReturnFailedOperation(long amount)
    {
        // Arrange
        var account = Account.Open(_customerId);

        // Act
        var operation = account.Credit(amount, "cred-003", Currency);

        // Assert
        operation.Status.Should().Be(OperationStatus.Failed);
        operation.FailureReason.Should().Contain("greater than zero");
        account.AvailableBalance.Should().Be(0);
    }

    #endregion

    #region Debit

    [Fact]
    public void Debit_WithSufficientFunds_ShouldDecreaseBalance()
    {
        // Arrange
        var account = Account.Open(_customerId);
        account.Credit(1000, "cred-001", Currency);

        // Act
        var operation = account.Debit(300, "deb-001", Currency);

        // Assert
        operation.Status.Should().Be(OperationStatus.Success);
        account.AvailableBalance.Should().Be(700);
        account.Operations.Should().Contain(operation);
        account.DomainEvents.Should().ContainSingle(e => e is AccountDebitedEvent);
    }

    [Fact]
    public void Debit_WithAmountExceedingBalanceButWithinCreditLimit_ShouldDecreaseBalanceAndUseCredit()
    {
        // Arrange
        var account = Account.Open(_customerId, creditLimit: 500);
        account.Credit(300, "cred-001", Currency);

        // Act
        var operation = account.Debit(600, "deb-001", Currency);

        // Assert
        operation.Status.Should().Be(OperationStatus.Success);
        account.AvailableBalance.Should().Be(-300);
        account.Operations.Should().Contain(operation);
    }

    [Fact]
    public void Debit_WithAmountExceedingBalanceAndCreditLimit_ShouldReturnFailedOperation()
    {
        // Arrange
        var account = Account.Open(_customerId, creditLimit: 500);
        account.Credit(300, "cred-001", Currency);

        // Act
        var operation = account.Debit(900, "deb-001", Currency);

        // Assert
        operation.Status.Should().Be(OperationStatus.Failed);
        operation.FailureReason.Should().Contain("insufficient funds");
        account.AvailableBalance.Should().Be(300);
    }

    [Fact]
    public void Debit_WithSameReferenceId_ShouldReturnExistingOperation()
    {
        // Arrange
        var account = Account.Open(_customerId);
        account.Credit(1000, "cred-001", Currency);

        // Act
        var first = account.Debit(300, "deb-001", Currency);
        var second = account.Debit(300, "deb-001", Currency);

        // Assert
        second.Should().BeSameAs(first);
        account.AvailableBalance.Should().Be(700);
    }

    [Fact]
    public void Debit_WhenAccountBlocked_ShouldReturnFailedOperation()
    {
        // Arrange
        var account = Account.Open(_customerId);
        account.Block();

        // Act
        var operation = account.Debit(100, "deb-002", Currency);

        // Assert
        operation.Status.Should().Be(OperationStatus.Failed);
        operation.FailureReason.Should().Contain("Blocked");
        account.AvailableBalance.Should().Be(0);
    }

    #endregion

    #region Reserve

    [Fact]
    public void Reserve_WithSufficientAvailableBalance_ShouldDecreaseAvailableAndIncreaseReserved()
    {
        // Arrange
        var account = Account.Open(_customerId);
        account.Credit(1000, "cred-001", Currency);

        // Act
        var operation = account.Reserve(300, "res-001", Currency);

        // Assert
        operation.Status.Should().Be(OperationStatus.Success);
        account.AvailableBalance.Should().Be(700);
        account.ReservedBalance.Should().Be(300);
        account.DomainEvents.Should().ContainSingle(e => e is FundsReservedEvent);
    }

    [Fact]
    public void Reserve_WithInsufficientAvailableBalance_ShouldReturnFailedOperation()
    {
        // Arrange
        var account = Account.Open(_customerId);
        account.Credit(100, "cred-001", Currency);

        // Act
        var operation = account.Reserve(200, "res-001", Currency);

        // Assert
        operation.Status.Should().Be(OperationStatus.Failed);
        operation.FailureReason.Should().Contain("insufficient available balance");
        account.AvailableBalance.Should().Be(100);
        account.ReservedBalance.Should().Be(0);
    }

    [Fact]
    public void Reserve_WithSameReferenceId_ShouldReturnExistingOperation()
    {
        // Arrange
        var account = Account.Open(_customerId);
        account.Credit(1000, "cred-001", Currency);

        // Act
        var first = account.Reserve(300, "res-001", Currency);
        var second = account.Reserve(300, "res-001", Currency);

        // Assert
        second.Should().BeSameAs(first);
        account.AvailableBalance.Should().Be(700);
        account.ReservedBalance.Should().Be(300);
    }

    #endregion

    #region Capture

    [Fact]
    public void Capture_WithValidReservation_ShouldDecreaseReservedBalance()
    {
        // Arrange
        var account = Account.Open(_customerId);
        account.Credit(1000, "cred-001", Currency);
        var reserve = account.Reserve(300, "res-001", Currency);

        // Act
        var operation = account.Capture(reserve.Id, "cap-001", Currency);

        // Assert
        operation.Status.Should().Be(OperationStatus.Success);
        account.ReservedBalance.Should().Be(0);
        account.AvailableBalance.Should().Be(700);
        account.DomainEvents.Should().ContainSingle(e => e is FundsCapturedEvent);
    }

    [Fact]
    public void Capture_WithInvalidReservationId_ShouldReturnFailedOperation()
    {
        // Arrange
        var account = Account.Open(_customerId);
        account.Credit(1000, "cred-001", Currency);

        // Act
        var operation = account.Capture(Guid.NewGuid(), "cap-001", Currency);

        // Assert
        operation.Status.Should().Be(OperationStatus.Failed);
        operation.FailureReason.Should().Contain("reservation");
        account.ReservedBalance.Should().Be(0);
        account.AvailableBalance.Should().Be(1000);
    }

    [Fact]
    public void Capture_WithReservedBalanceInsufficient_ShouldReturnFailedOperation()
    {
        // Arrange
        var account = Account.Open(_customerId);
        account.Credit(1000, "cred-001", Currency);
        var reserve = account.Reserve(300, "res-001", Currency);
        account.Capture(reserve.Id, "cap-001", Currency);

        // Act
        var operation = account.Capture(reserve.Id, "cap-002", Currency);

        // Assert
        operation.Status.Should().Be(OperationStatus.Failed);
        operation.FailureReason.Should().Contain("insufficient reserved balance");
        account.ReservedBalance.Should().Be(0);
        account.AvailableBalance.Should().Be(700);
    }

    #endregion

    #region Reversal

    [Fact]
    public void Reversal_ForCreditOperation_ShouldDecreaseAvailableBalance()
    {
        // Arrange
        var account = Account.Open(_customerId);
        var credit = account.Credit(1000, "cred-001", Currency);

        // Act
        var operation = account.Reversal(credit.Id, "rev-001", Currency);

        // Assert
        operation.Status.Should().Be(OperationStatus.Success);
        account.AvailableBalance.Should().Be(0);
        account.DomainEvents.Should().ContainSingle(e => e is OperationReversedEvent);
    }

    [Fact]
    public void Reversal_ForDebitOperation_ShouldIncreaseAvailableBalance()
    {
        // Arrange
        var account = Account.Open(_customerId);
        account.Credit(1000, "cred-001", Currency);
        var debit = account.Debit(300, "deb-001", Currency);

        // Act
        var operation = account.Reversal(debit.Id, "rev-001", Currency);

        // Assert
        operation.Status.Should().Be(OperationStatus.Success);
        account.AvailableBalance.Should().Be(1000);
    }

    [Fact]
    public void Reversal_ForReserveOperation_ShouldDecreaseReservedAndIncreaseAvailable()
    {
        // Arrange
        var account = Account.Open(_customerId);
        account.Credit(1000, "cred-001", Currency);
        var reserve = account.Reserve(300, "res-001", Currency);

        // Act
        var operation = account.Reversal(reserve.Id, "rev-001", Currency);

        // Assert
        operation.Status.Should().Be(OperationStatus.Success);
        account.ReservedBalance.Should().Be(0);
        account.AvailableBalance.Should().Be(1000);
    }

    [Fact]
    public void Reversal_ForCaptureOperation_ShouldIncreaseAvailableBalance()
    {
        // Arrange
        var account = Account.Open(_customerId);
        account.Credit(1000, "cred-001", Currency);
        var reserve = account.Reserve(300, "res-001", Currency);
        var capture = account.Capture(reserve.Id, "cap-001", Currency);

        // Act
        var operation = account.Reversal(capture.Id, "rev-001", Currency);

        // Assert
        operation.Status.Should().Be(OperationStatus.Success);
        account.ReservedBalance.Should().Be(0);
        account.AvailableBalance.Should().Be(1000);
    }

    [Fact]
    public void Reversal_WithInvalidOperationId_ShouldReturnFailedOperation()
    {
        // Arrange
        var account = Account.Open(_customerId);

        // Act
        var operation = account.Reversal(Guid.NewGuid(), "rev-001", Currency);

        // Assert
        operation.Status.Should().Be(OperationStatus.Failed);
        operation.FailureReason.Should().Contain("not found");
        account.AvailableBalance.Should().Be(0);
    }

    [Fact]
    public void Reversal_WhenAccountBlocked_ShouldReturnFailedOperation()
    {
        // Arrange
        var account = Account.Open(_customerId);
        var credit = account.Credit(1000, "cred-001", Currency);
        account.Block();

        // Act
        var operation = account.Reversal(credit.Id, "rev-001", Currency);

        // Assert
        operation.Status.Should().Be(OperationStatus.Failed);
        operation.FailureReason.Should().Contain("Blocked");
        account.AvailableBalance.Should().Be(1000);
    }

    #endregion

    #region Status Changes

    [Fact]
    public void Activate_ShouldSetStatusToActive()
    {
        var account = Account.Open(_customerId);
        account.Deactivate();
        account.Activate();
        account.Status.Should().Be(AccountStatus.Active);
    }

    [Fact]
    public void Deactivate_ShouldSetStatusToInactive()
    {
        var account = Account.Open(_customerId);
        account.Deactivate();
        account.Status.Should().Be(AccountStatus.Inactive);
    }

    [Fact]
    public void Block_ShouldSetStatusToBlocked()
    {
        var account = Account.Open(_customerId);
        account.Block();
        account.Status.Should().Be(AccountStatus.Blocked);
    }

    #endregion
}
