using ArtemisBank.Core.Application.Features.CreditCards.Commands;
using ArtemisBank.Core.Application.Features.CreditCards.Queries;
using ArtemisBank.Core.Application.Features.HermesPay.Commands;
using ArtemisBank.Core.Application.Features.Loans.Commands;
using ArtemisBank.Core.Application.Features.Loans.Queries;
using FluentAssertions;
using Xunit;

namespace ArtemisBank.Tests.Unit.Features
{
    /// <summary>Unit tests de los validadores de los Commands y Queries de Manuel.</summary>
    public class CreditProductsCqrsTests
    {
        // ------------------------------------------------------------ prestamos

        [Theory]
        [InlineData(0, 20, "activos", false)]
        [InlineData(1, 0, "activos", false)]
        [InlineData(1, 50, "activos", false)]
        [InlineData(1, 20, "inventado", false)]
        [InlineData(1, 20, "activos", true)]
        [InlineData(2, 20, "completados", true)]
        [InlineData(1, 20, "todos", true)]
        public void GetLoansQueryValidator_ValidaParametros(int page, int pageSize, string status, bool expected)
        {
            new GetLoansQueryValidator()
                .Validate(new GetLoansQuery { Page = page, PageSize = pageSize, Status = status })
                .IsValid.Should().Be(expected);
        }

        [Theory]
        [InlineData("cliente-1", 50000, 12, 12, true)]
        [InlineData("cliente-1", 50000, 7, 12, false)]  // plazo no multiplo de 6
        [InlineData("cliente-1", 50000, 66, 12, false)] // plazo fuera de rango
        [InlineData("cliente-1", 0, 12, 12, false)]     // capital cero
        [InlineData("cliente-1", 50000, 12, -1, false)] // tasa negativa
        [InlineData("", 50000, 12, 12, false)]          // sin cliente
        [InlineData("cliente-1", 50000, 12, 0, true)]   // tasa cero es valida
        public void AssignLoanCommandValidator_ValidaReglas(string clientId, decimal capital, int term,
            decimal rate, bool expected)
        {
            new AssignLoanCommandValidator()
                .Validate(new AssignLoanCommand
                {
                    ClientId = clientId,
                    CapitalAmount = capital,
                    TermInMonths = term,
                    AnnualInterestRate = rate
                })
                .IsValid.Should().Be(expected);
        }

        [Theory]
        [InlineData(1, 15, true)]
        [InlineData(0, 15, false)]
        [InlineData(1, -1, false)]
        [InlineData(1, 0, true)]
        public void UpdateLoanRateCommandValidator_ValidaReglas(int id, decimal rate, bool expected)
        {
            new UpdateLoanRateCommandValidator()
                .Validate(new UpdateLoanRateCommand { Id = id, AnnualInterestRate = rate })
                .IsValid.Should().Be(expected);
        }

        // ------------------------------------------------------------ tarjetas

        [Theory]
        [InlineData("cliente-1", 50000, true)]
        [InlineData("cliente-1", 0, false)]
        [InlineData("cliente-1", -100, false)]
        [InlineData("", 50000, false)]
        public void AssignCreditCardCommandValidator_ValidaReglas(string clientId, decimal limit, bool expected)
        {
            new AssignCreditCardCommandValidator()
                .Validate(new AssignCreditCardCommand { ClientId = clientId, CreditLimit = limit })
                .IsValid.Should().Be(expected);
        }

        [Theory]
        [InlineData(1, 50000, true)]
        [InlineData(0, 50000, false)]
        [InlineData(1, 0, false)]
        public void UpdateCardLimitCommandValidator_ValidaReglas(int id, decimal limit, bool expected)
        {
            new UpdateCardLimitCommandValidator()
                .Validate(new UpdateCardLimitCommand { Id = id, CreditLimit = limit })
                .IsValid.Should().Be(expected);
        }

        // ------------------------------------------------------------ Hermes Pay

        [Theory]
        [InlineData(5, "4000000000000001", "12", "29", "123", 8000, true)]
        [InlineData(0, "4000000000000001", "12", "29", "123", 8000, false)] // comercio invalido
        [InlineData(5, "40000001", "12", "29", "123", 8000, false)]         // tarjeta no 16 digitos
        [InlineData(5, "400000000000000X", "12", "29", "123", 8000, false)] // tarjeta no numerica
        [InlineData(5, "4000000000000001", "12", "29", "12", 8000, false)]  // cvc no 3 digitos
        [InlineData(5, "4000000000000001", "12", "29", "123", 0, false)]    // monto cero
        public void ProcessPaymentCommandValidator_ValidaReglas(int commerceId, string card, string month,
            string year, string cvc, decimal amount, bool expected)
        {
            new ProcessPaymentCommandValidator()
                .Validate(new ProcessPaymentCommand
                {
                    CommerceId = commerceId,
                    CardNumber = card,
                    MonthExpirationCard = month,
                    YearExpirationCard = year,
                    Cvc = cvc,
                    TransactionAmount = amount
                })
                .IsValid.Should().Be(expected);
        }
    }
}
