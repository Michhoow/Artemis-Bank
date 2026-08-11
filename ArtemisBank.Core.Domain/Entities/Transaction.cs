using ArtemisBank.Core.Domain.Common;
using ArtemisBank.Core.Domain.Common.Enums;

namespace ArtemisBank.Core.Domain.Entities
{
    /// <summary>
    /// Movimiento del libro mayor de una cuenta de ahorro.
    /// Una operacion que mueve dinero entre dos cuentas genera DOS filas (registro cruzado)
    /// que comparten el mismo OperationReference: DEBITO en origen y CREDITO en destino.
    /// </summary>
    public class Transaction : AuditableEntity
    {
        /// <summary>Cuenta a la que pertenece esta linea del historial.</summary>
        public int SavingsAccountId { get; set; }

        /// <summary>Monto del movimiento. Siempre positivo; el signo lo determina el Type.</summary>
        public decimal Amount { get; set; }

        public TransactionType Type { get; set; }

        public TransactionStatus Status { get; set; } = TransactionStatus.Aprobada;

        public TransactionOperation Operation { get; set; }

        /// <summary>
        /// Fuente de la transaccion (texto mostrado al usuario):
        /// numero de cuenta origen, "DEPOSITO", ultimos 4 digitos de tarjeta o numero de prestamo.
        /// </summary>
        public string Origin { get; set; } = string.Empty;

        /// <summary>
        /// Destino de la transaccion (texto mostrado al usuario):
        /// numero de cuenta destino, "RETIRO", ultimos 4 digitos de tarjeta o numero de prestamo.
        /// </summary>
        public string Beneficiary { get; set; } = string.Empty;

        /// <summary>Motivo cuando Status = Rechazada. Nunca contiene datos sensibles.</summary>
        public string? RejectionReason { get; set; }

        /// <summary>
        /// Correlacion de la operacion de negocio. Las dos patas de una transferencia
        /// comparten el mismo valor; permite contar operaciones y no filas.
        /// </summary>
        public string OperationReference { get; set; } = string.Empty;

        /// <summary>Usuario autenticado responsable (cliente, cajero o administrador).</summary>
        public string? PerformedByUserId { get; set; }

        /// <summary>Rol del usuario responsable. Permite los indicadores del Home del cajero.</summary>
        public Roles? PerformedByRole { get; set; }

        // Navigation properties
        public SavingsAccount? SavingsAccount { get; set; }

        public bool IsPayment =>
            Operation == TransactionOperation.PagoTarjeta || Operation == TransactionOperation.PagoPrestamo;
    }
}
