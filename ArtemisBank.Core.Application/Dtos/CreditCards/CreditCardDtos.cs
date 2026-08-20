namespace ArtemisBank.Core.Application.Dtos.CreditCards
{
    /// <summary>Fila del listado de tarjetas (WebApp/API). Nunca incluye numero completo ni CVC.</summary>
    public class CreditCardListItemDto
    {
        public int Id { get; set; }
        /// <summary>"************1234" — solo enmascarado.</summary>
        public string MaskedCardNumber { get; set; } = string.Empty;
        public string LastFourDigits { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientFullName { get; set; } = string.Empty;
        public decimal CreditLimit { get; set; }
        public decimal AvailableCredit { get; set; }
        public decimal CurrentDebt { get; set; }
        /// <summary>MM/AA.</summary>
        public string ExpirationDate { get; set; } = string.Empty;
        /// <summary>"Activa" | "Cancelada".</summary>
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>Consumo tal como se expone en el detalle de la tarjeta.</summary>
    public class ConsumptionDto
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        /// <summary>Nombre del comercio o "AVANCE".</summary>
        public string CommerceName { get; set; } = string.Empty;
        /// <summary>"APROBADO" | "RECHAZADO".</summary>
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>Detalle de una tarjeta con sus consumos (GET /api/credit-card/{id}).</summary>
    public class CreditCardDetailDto
    {
        public int Id { get; set; }
        public string MaskedCardNumber { get; set; } = string.Empty;
        public string LastFourDigits { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientFullName { get; set; } = string.Empty;
        public decimal CreditLimit { get; set; }
        public decimal AvailableCredit { get; set; }
        public decimal CurrentDebt { get; set; }
        public string ExpirationDate { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<ConsumptionDto> Consumptions { get; set; } = new();
    }

    /// <summary>Datos de asignacion de una tarjeta.</summary>
    public class AssignCreditCardDto
    {
        public string ClientId { get; set; } = string.Empty;
        public decimal CreditLimit { get; set; }
        public string? AssignedByUserId { get; set; }
    }

    /// <summary>Filtro del listado de tarjetas.</summary>
    public class CreditCardFilterDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        /// <summary>activa | cancelada | todas.</summary>
        public string? Status { get; set; }
        public string? Identification { get; set; }
    }

    /// <summary>Datos de un avance de efectivo (WebApp cliente).</summary>
    public class CashAdvanceDto
    {
        public string CardNumber { get; set; } = string.Empty;
        public string TargetAccountNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? ClientId { get; set; }
    }

    /// <summary>Datos de un pago Hermes Pay.</summary>
    public class ProcessPaymentDto
    {
        public int CommerceId { get; set; }
        public string CardNumber { get; set; } = string.Empty;
        public string MonthExpirationCard { get; set; } = string.Empty;
        public string YearExpirationCard { get; set; } = string.Empty;
        public string Cvc { get; set; } = string.Empty;
        public decimal TransactionAmount { get; set; }
        /// <summary>Actor autenticado (rol Administrador o Comercio) resuelto por el controlador.</summary>
        public string? AuthenticatedUserId { get; set; }
        public bool IsCommerceRole { get; set; }
    }

    /// <summary>Fila de transaccion de comercio (GET /pay/get-transactions).</summary>
    public class CommerceTransactionDto
    {
        public string Id { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public decimal Amount { get; set; }
        public string CardLastFourDigits { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
