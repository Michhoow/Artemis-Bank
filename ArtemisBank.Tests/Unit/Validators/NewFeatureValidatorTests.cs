using ArtemisBank.Core.Application.Features.Commerces.Commands;
using ArtemisBank.Core.Application.Features.Commerces.Queries;
using ArtemisBank.Core.Application.Features.CreditCards.Commands;
using ArtemisBank.Core.Application.Features.CreditCards.Queries;
using ArtemisBank.Core.Application.Features.HermesPay.Commands;
using ArtemisBank.Core.Application.Features.HermesPay.Queries;
using ArtemisBank.Core.Application.Features.Loans.Commands;
using ArtemisBank.Core.Application.Features.Loans.Queries;
using ArtemisBank.Core.Application.Features.Users.Commands;
using ArtemisBank.Core.Application.Features.Users.Queries;
using FluentAssertions;
using Xunit;

namespace ArtemisBank.Tests.Unit.Validators
{
    public class NewFeatureValidatorTests
    {
        private static CreateLoanCommand ValidLoan() => new CreateLoanCommand
        {
            ClientId = "cliente-1",
            CapitalAmount = 100000m,
            TermInMonths = 12,
            AnnualInterestRate = 12m
        };

        [Fact]
        public void CreateLoan_ConDatosValidos_Pasa()
            => new CreateLoanCommandValidator().Validate(ValidLoan()).IsValid.Should().BeTrue();

        [Fact]
        public void CreateLoan_SinCliente_Falla()
        {
            var command = ValidLoan();
            command.ClientId = string.Empty;

            new CreateLoanCommandValidator().Validate(command).IsValid.Should().BeFalse();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void CreateLoan_ConMontoNoPositivo_Falla(decimal capital)
        {
            var command = ValidLoan();
            command.CapitalAmount = capital;

            new CreateLoanCommandValidator().Validate(command).IsValid.Should().BeFalse();
        }

        [Theory]
        [InlineData(5)]
        [InlineData(13)]
        [InlineData(0)]
        [InlineData(72)]
        public void CreateLoan_ConPlazoFueraDeLaLista_Falla(int term)
        {
            var command = ValidLoan();
            command.TermInMonths = term;

            new CreateLoanCommandValidator().Validate(command).IsValid.Should().BeFalse();
        }

        [Theory]
        [InlineData(6)]
        [InlineData(18)]
        [InlineData(60)]
        public void CreateLoan_ConPlazoPermitido_Pasa(int term)
        {
            var command = ValidLoan();
            command.TermInMonths = term;

            new CreateLoanCommandValidator().Validate(command).IsValid.Should().BeTrue();
        }

        [Fact]
        public void CreateLoan_ConTasaNegativa_Falla()
        {
            var command = ValidLoan();
            command.AnnualInterestRate = -0.01m;

            new CreateLoanCommandValidator().Validate(command).IsValid.Should().BeFalse();
        }

        [Fact]
        public void CreateLoan_ConTasaCero_Pasa()
        {
            var command = ValidLoan();
            command.AnnualInterestRate = 0m;

            new CreateLoanCommandValidator().Validate(command).IsValid.Should().BeTrue();
        }

        [Fact]
        public void UpdateLoanRate_ConTasaNegativa_Falla()
            => new UpdateLoanRateCommandValidator()
                .Validate(new UpdateLoanRateCommand { LoanId = 1, AnnualInterestRate = -5m })
                .IsValid.Should().BeFalse();

        [Fact]
        public void GetLoans_ConPageSizeSobreElMaximo_Falla()
            => new GetLoansQueryValidator()
                .Validate(new GetLoansQuery { Page = 1, PageSize = 50 })
                .IsValid.Should().BeFalse();

        [Theory]
        [InlineData("activo")]
        [InlineData("completado")]
        [InlineData("todos")]
        [InlineData(null)]
        public void GetLoans_ConEstadoValido_Pasa(string? status)
            => new GetLoansQueryValidator()
                .Validate(new GetLoansQuery { Status = status })
                .IsValid.Should().BeTrue();

        [Fact]
        public void GetLoans_ConEstadoDesconocido_Falla()
            => new GetLoansQueryValidator()
                .Validate(new GetLoansQuery { Status = "pendiente" })
                .IsValid.Should().BeFalse();

        [Fact]
        public void CreateCreditCard_ConLimiteNoPositivo_Falla()
            => new CreateCreditCardCommandValidator()
                .Validate(new CreateCreditCardCommand { ClientId = "cliente-1", CreditLimit = 0m })
                .IsValid.Should().BeFalse();

        [Fact]
        public void CreateCreditCard_SinCliente_Falla()
            => new CreateCreditCardCommandValidator()
                .Validate(new CreateCreditCardCommand { ClientId = "", CreditLimit = 50000m })
                .IsValid.Should().BeFalse();

        [Fact]
        public void UpdateCardLimit_ConDatosValidos_Pasa()
            => new UpdateCardLimitCommandValidator()
                .Validate(new UpdateCardLimitCommand { CreditCardId = 1, CreditLimit = 75000m })
                .IsValid.Should().BeTrue();

        [Fact]
        public void CancelCreditCard_ConIdInvalido_Falla()
            => new CancelCreditCardCommandValidator()
                .Validate(new CancelCreditCardCommand { CreditCardId = 0 })
                .IsValid.Should().BeFalse();

        [Fact]
        public void GetCreditCards_ConEstadoDesconocido_Falla()
            => new GetCreditCardsQueryValidator()
                .Validate(new GetCreditCardsQuery { Status = "bloqueada" })
                .IsValid.Should().BeFalse();

        private static CashAdvanceCommand ValidAdvance() => new CashAdvanceCommand
        {
            CreditCardId = 1,
            TargetAccountNumber = "100000001",
            Amount = 5000m,
            ClientId = "cliente-1"
        };

        [Fact]
        public void CashAdvance_ConDatosValidos_Pasa()
            => new CashAdvanceCommandValidator().Validate(ValidAdvance()).IsValid.Should().BeTrue();

        [Theory]
        [InlineData("12345")]
        [InlineData("1234567890")]
        [InlineData("abcdefghi")]
        public void CashAdvance_ConCuentaMalFormada_Falla(string accountNumber)
        {
            var command = ValidAdvance();
            command.TargetAccountNumber = accountNumber;

            new CashAdvanceCommandValidator().Validate(command).IsValid.Should().BeFalse();
        }

        [Fact]
        public void CashAdvance_SinClienteAutenticado_Falla()
        {
            var command = ValidAdvance();
            command.ClientId = string.Empty;

            new CashAdvanceCommandValidator().Validate(command).IsValid.Should().BeFalse();
        }

        private static ProcessPaymentCommand ValidPayment() => new ProcessPaymentCommand
        {
            CardNumber = "4000000000000001",
            MonthExpirationCard = "02",
            YearExpirationCard = "2029",
            Cvc = "123",
            TransactionAmount = 1500m,
            CommerceId = 1
        };

        [Fact]
        public void ProcessPayment_ConDatosValidos_Pasa()
            => new ProcessPaymentCommandValidator().Validate(ValidPayment()).IsValid.Should().BeTrue();

        [Theory]
        [InlineData("400000000000000")]
        [InlineData("40000000000000012")]
        [InlineData("4000-0000-0000-0001")]
        public void ProcessPayment_ConNumeroQueNoTieneDieciseisDigitos_Falla(string cardNumber)
        {
            var command = ValidPayment();
            command.CardNumber = cardNumber;

            new ProcessPaymentCommandValidator().Validate(command).IsValid.Should().BeFalse();
        }

        [Theory]
        [InlineData("12")]
        [InlineData("1234")]
        [InlineData("abc")]
        public void ProcessPayment_ConCvcMalFormado_Falla(string cvc)
        {
            var command = ValidPayment();
            command.Cvc = cvc;

            new ProcessPaymentCommandValidator().Validate(command).IsValid.Should().BeFalse();
        }

        [Theory]
        [InlineData("00")]
        [InlineData("13")]
        [InlineData("1")]
        [InlineData("ab")]
        public void ProcessPayment_ConMesDeExpiracionInvalido_Falla(string month)
        {
            var command = ValidPayment();
            command.MonthExpirationCard = month;

            new ProcessPaymentCommandValidator().Validate(command).IsValid.Should().BeFalse();
        }

        [Fact]
        public void ProcessPayment_ConAnioEnDosDigitos_Pasa()
        {
            var command = ValidPayment();
            command.YearExpirationCard = "29";

            new ProcessPaymentCommandValidator().Validate(command).IsValid.Should().BeTrue();
        }

        [Fact]
        public void ProcessPayment_SinAnioDeExpiracion_Falla()
        {
            var command = ValidPayment();
            command.YearExpirationCard = string.Empty;

            new ProcessPaymentCommandValidator().Validate(command).IsValid.Should().BeFalse();
        }

        [Fact]
        public void ProcessPayment_ConMontoCero_Falla()
        {
            var command = ValidPayment();
            command.TransactionAmount = 0m;

            new ProcessPaymentCommandValidator().Validate(command).IsValid.Should().BeFalse();
        }

        [Fact]
        public void ProcessPayment_SinComercio_Falla()
        {
            var command = ValidPayment();
            command.CommerceId = 0;

            new ProcessPaymentCommandValidator().Validate(command).IsValid.Should().BeFalse();
        }

        [Fact]
        public void GetCommerceTransactions_ConPageSizeSobreElMaximo_Falla()
            => new GetCommerceTransactionsQueryValidator()
                .Validate(new GetCommerceTransactionsQuery { CommerceId = 1, PageSize = 50 })
                .IsValid.Should().BeFalse();

        [Fact]
        public void GetCommerceTransactions_ConDatosValidos_Pasa()
            => new GetCommerceTransactionsQueryValidator()
                .Validate(new GetCommerceTransactionsQuery { CommerceId = 1 })
                .IsValid.Should().BeTrue();

        private static CreateUserCommand ValidUser() => new CreateUserCommand
        {
            FirstName = "Ana",
            LastName = "Pérez",
            Identification = "00112345678",
            Email = "ana@artemisbank.do",
            UserName = "aperez",
            Password = "Passw0rd!",
            ConfirmPassword = "Passw0rd!",
            Role = "Cliente"
        };

        [Fact]
        public void CreateUser_ConDatosValidos_Pasa()
            => new CreateUserCommandValidator().Validate(ValidUser()).IsValid.Should().BeTrue();

        [Fact]
        public void CreateUser_ConContraseniasQueNoCoinciden_Falla()
        {
            var command = ValidUser();
            command.ConfirmPassword = "Otra1234!";

            new CreateUserCommandValidator().Validate(command).IsValid.Should().BeFalse();
        }

        [Theory]
        [InlineData("123")]
        [InlineData("0011234567")]
        [InlineData("abcdefghijk")]
        public void CreateUser_ConCedulaMalFormada_Falla(string identification)
        {
            var command = ValidUser();
            command.Identification = identification;

            new CreateUserCommandValidator().Validate(command).IsValid.Should().BeFalse();
        }

        [Fact]
        public void CreateUser_ConCorreoInvalido_Falla()
        {
            var command = ValidUser();
            command.Email = "no-es-un-correo";

            new CreateUserCommandValidator().Validate(command).IsValid.Should().BeFalse();
        }

        [Fact]
        public void CreateUser_ConRolComercio_Falla()
        {
            var command = ValidUser();
            command.Role = "Comercio";

            new CreateUserCommandValidator().Validate(command).IsValid.Should().BeFalse();
        }

        [Fact]
        public void CreateUser_ConMontoInicialNegativo_Falla()
        {
            var command = ValidUser();
            command.InitialAmount = -1m;

            new CreateUserCommandValidator().Validate(command).IsValid.Should().BeFalse();
        }

        [Fact]
        public void UpdateUser_SinContrasenia_Pasa()
        {
            var command = new UpdateUserCommand
            {
                UserId = "u1",
                FirstName = "Ana",
                LastName = "Pérez",
                Identification = "00112345678",
                Email = "ana@artemisbank.do",
                UserName = "aperez"
            };

            new UpdateUserCommandValidator().Validate(command).IsValid.Should().BeTrue();
        }

        [Fact]
        public void UpdateUser_ConContraseniaCortaSiSeEnvia_Falla()
        {
            var command = new UpdateUserCommand
            {
                UserId = "u1",
                FirstName = "Ana",
                LastName = "Pérez",
                Identification = "00112345678",
                Email = "ana@artemisbank.do",
                UserName = "aperez",
                Password = "123",
                ConfirmPassword = "123"
            };

            new UpdateUserCommandValidator().Validate(command).IsValid.Should().BeFalse();
        }

        [Fact]
        public void SetUserStatus_SinUsuarioObjetivo_Falla()
            => new SetUserStatusCommandValidator()
                .Validate(new SetUserStatusCommand { TargetUserId = "" })
                .IsValid.Should().BeFalse();

        [Theory]
        [InlineData("Administrador")]
        [InlineData("Cajero")]
        [InlineData("Cliente")]
        [InlineData("Comercio")]
        [InlineData(null)]
        public void GetUsers_ConRolValido_Pasa(string? role)
            => new GetUsersQueryValidator()
                .Validate(new GetUsersQuery { Role = role })
                .IsValid.Should().BeTrue();

        [Fact]
        public void GetUsers_ConRolInexistente_Falla()
            => new GetUsersQueryValidator()
                .Validate(new GetUsersQuery { Role = "Supervisor" })
                .IsValid.Should().BeFalse();

        private static CreateCommerceCommand ValidCommerce() => new CreateCommerceCommand
        {
            Name = "Supermercado Hermes",
            Rnc = "130123456",
            Email = "comercio@artemisbank.do",
            PhoneNumber = "8095551234"
        };

        [Fact]
        public void CreateCommerce_ConDatosValidos_Pasa()
            => new CreateCommerceCommandValidator().Validate(ValidCommerce()).IsValid.Should().BeTrue();

        [Theory]
        [InlineData("12345")]
        [InlineData("130123456789")]
        [InlineData("RNC130123")]
        public void CreateCommerce_ConRncMalFormado_Falla(string rnc)
        {
            var command = ValidCommerce();
            command.Rnc = rnc;

            new CreateCommerceCommandValidator().Validate(command).IsValid.Should().BeFalse();
        }

        [Theory]
        [InlineData("809555123")]
        [InlineData("80955512345")]
        public void CreateCommerce_ConTelefonoMalFormado_Falla(string phone)
        {
            var command = ValidCommerce();
            command.PhoneNumber = phone;

            new CreateCommerceCommandValidator().Validate(command).IsValid.Should().BeFalse();
        }

        [Theory]
        [InlineData("activo")]
        [InlineData("inactivo")]
        [InlineData("todos")]
        [InlineData(null)]
        public void GetCommerces_ConEstadoValido_Pasa(string? status)
            => new GetCommercesQueryValidator()
                .Validate(new GetCommercesQuery { Status = status })
                .IsValid.Should().BeTrue();

        [Fact]
        public void GetCommerces_ConEstadoDesconocido_Falla()
            => new GetCommercesQueryValidator()
                .Validate(new GetCommercesQuery { Status = "suspendido" })
                .IsValid.Should().BeFalse();

        [Fact]
        public void SetCommerceStatus_ConIdInvalido_Falla()
            => new SetCommerceStatusCommandValidator()
                .Validate(new SetCommerceStatusCommand { Id = 0, IsActive = false })
                .IsValid.Should().BeFalse();
    }
}
