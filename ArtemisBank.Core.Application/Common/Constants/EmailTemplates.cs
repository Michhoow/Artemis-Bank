using ArtemisBank.Core.Application.Common.Models;

namespace ArtemisBank.Core.Application.Common.Constants
{
    /// <summary>
    /// Plantillas de los correos transaccionales.
    /// Nunca incluyen numero completo de cuenta ni de tarjeta: solo los ultimos 4 digitos.
    /// </summary>
    public static class EmailTemplates
    {
        private static string Wrap(string clientName, string body) =>
            $@"<div style=""font-family:Segoe UI,Arial,sans-serif;font-size:14px;color:#1f2937"">
  <p>Hola {clientName},</p>
  {body}
  <p style=""color:#6b7280;font-size:12px"">Si usted no reconoce esta operación, comuníquese con la entidad bancaria.</p>
</div>";

        private static string When(DateTime moment) =>
            $"{moment:dd/MM/yyyy} a las {moment:hh:mm:ss tt}";

        // ---------- Transaccion enviada (express / beneficiario / terceros) ----------
        public static string SentSubject(string targetLastFour) => $"Transacción realizada a la cuenta {targetLastFour}";

        public static string SentBody(string clientName, decimal amount, string sourceLastFour,
            string targetLastFour, DateTime moment) => Wrap(clientName, $@"
  <p>Se ha realizado una transacción desde su cuenta terminada en <b>{sourceLastFour}</b>.</p>
  <p>Monto transferido: <b>{Money.Format(amount)}</b><br/>
     Cuenta destino terminada en: <b>{targetLastFour}</b><br/>
     Fecha y hora: {When(moment)}</p>");

        // ---------- Transaccion recibida ----------
        public static string ReceivedSubject(string sourceLastFour) => $"Transacción enviada desde la cuenta {sourceLastFour}";

        public static string ReceivedBody(string clientName, decimal amount, string sourceLastFour,
            string targetLastFour, DateTime moment) => Wrap(clientName, $@"
  <p>Ha recibido una transacción en su cuenta terminada en <b>{targetLastFour}</b>.</p>
  <p>Monto recibido: <b>{Money.Format(amount)}</b><br/>
     Cuenta origen terminada en: <b>{sourceLastFour}</b><br/>
     Fecha y hora: {When(moment)}</p>");

        // ---------- Transferencia entre cuentas propias ----------
        public const string TransferSubject = "Transferencia entre cuentas realizada";

        public static string TransferBody(string clientName, decimal amount, string sourceLastFour,
            string targetLastFour, DateTime moment) => Wrap(clientName, $@"
  <p>Se ha realizado una transferencia entre sus cuentas de ahorro.</p>
  <p>Cuenta origen terminada en: <b>{sourceLastFour}</b><br/>
     Cuenta destino terminada en: <b>{targetLastFour}</b><br/>
     Monto transferido: <b>{Money.Format(amount)}</b><br/>
     Fecha y hora: {When(moment)}</p>");

        // ---------- Deposito ----------
        public static string DepositSubject(string lastFour) => $"Depósito realizado a su cuenta {lastFour}";

        public static string DepositBody(string clientName, decimal amount, string lastFour, DateTime moment)
            => Wrap(clientName, $@"
  <p>Se ha realizado un depósito a su cuenta terminada en <b>{lastFour}</b>.</p>
  <p>Monto depositado: <b>{Money.Format(amount)}</b><br/>
     Fecha y hora: {When(moment)}</p>");

        // ---------- Retiro ----------
        public static string WithdrawalSubject(string lastFour) => $"Retiro realizado desde su cuenta {lastFour}";

        public static string WithdrawalBody(string clientName, decimal amount, string lastFour, DateTime moment)
            => Wrap(clientName, $@"
  <p>Se ha realizado un retiro desde su cuenta terminada en <b>{lastFour}</b>.</p>
  <p>Monto retirado: <b>{Money.Format(amount)}</b><br/>
     Fecha y hora: {When(moment)}</p>");

        // ---------- Pago a tarjeta ----------
        public static string CardPaymentSubject(string cardLastFour) => $"Pago realizado a la tarjeta {cardLastFour}";

        public static string CardPaymentBody(string clientName, decimal amount, string accountLastFour,
            string cardLastFour, DateTime moment) => Wrap(clientName, $@"
  <p>Se ha realizado un pago a su tarjeta de crédito terminada en <b>{cardLastFour}</b>.</p>
  <p>Monto pagado: <b>{Money.Format(amount)}</b><br/>
     Cuenta origen terminada en: <b>{accountLastFour}</b><br/>
     Fecha y hora: {When(moment)}</p>");

        public static string CardPaymentDebitNoticeSubject(string accountLastFour)
            => $"Débito realizado desde su cuenta {accountLastFour}";

        public static string CardPaymentDebitNoticeBody(string clientName, decimal amount, string accountLastFour,
            string cardLastFour, DateTime moment) => Wrap(clientName, $@"
  <p>Se debitó dinero de su cuenta terminada en <b>{accountLastFour}</b> para pagar una tarjeta de crédito terminada en <b>{cardLastFour}</b>.</p>
  <p>Monto debitado: <b>{Money.Format(amount)}</b><br/>
     Fecha y hora: {When(moment)}</p>");

        // ---------- Pago a prestamo ----------
        public static string LoanPaymentSubject(string loanNumber) => $"Pago realizado al préstamo {loanNumber}";

        public static string LoanPaymentBody(string clientName, decimal amount, string loanNumber,
            string accountLastFour, DateTime moment) => Wrap(clientName, $@"
  <p>Se ha realizado un pago a su préstamo <b>{loanNumber}</b>.</p>
  <p>Monto pagado: <b>{Money.Format(amount)}</b><br/>
     Cuenta origen terminada en: <b>{accountLastFour}</b><br/>
     Fecha y hora: {When(moment)}</p>");

        // =================================================================================
        //  Productos de credito y pagos (Manuel)
        // =================================================================================

        // ---------- Aprobacion de prestamo ----------
        public const string LoanApprovedSubject = "Préstamo aprobado";

        public static string LoanApprovedBody(string clientName, decimal approvedCapital, int termInMonths,
            decimal annualRate, decimal monthlyInstallment, string loanNumber) => Wrap(clientName, $@"
  <p>Su préstamo ha sido aprobado correctamente.</p>
  <p>Número de préstamo: <b>{loanNumber}</b><br/>
     Monto aprobado: <b>{Money.Format(approvedCapital)}</b><br/>
     Plazo: <b>{termInMonths} meses</b><br/>
     Tasa de interés anual: <b>{annualRate:N2}%</b><br/>
     Cuota mensual: <b>{Money.Format(monthlyInstallment)}</b></p>
  <p>El monto aprobado ha sido depositado en su cuenta de ahorro principal.</p>");

        // ---------- Modificacion de tasa de prestamo ----------
        public const string LoanRateUpdatedSubject = "Actualización de tasa de interés de su préstamo";

        public static string LoanRateUpdatedBody(string clientName, string loanNumber, decimal newRate,
            decimal newMonthlyInstallment) => Wrap(clientName, $@"
  <p>La tasa de interés de su préstamo <b>{loanNumber}</b> ha sido actualizada.</p>
  <p>Nueva tasa de interés anual: <b>{newRate:N2}%</b><br/>
     Nueva cuota mensual de las cuotas futuras: <b>{Money.Format(newMonthlyInstallment)}</b></p>");

        // ---------- Asignacion de tarjeta ----------
        public const string CardAssignedSubject = "Nueva tarjeta de crédito asignada";

        public static string CardAssignedBody(string clientName, string cardLastFour, decimal creditLimit,
            string expiration, DateTime assignedAt) => Wrap(clientName, $@"
  <p>Se ha asignado una nueva tarjeta de crédito a su cuenta.</p>
  <p>Tarjeta terminada en: <b>{cardLastFour}</b><br/>
     Límite aprobado: <b>{Money.Format(creditLimit)}</b><br/>
     Fecha de expiración: <b>{expiration}</b><br/>
     Fecha de asignación: {When(assignedAt)}</p>
  <p style=""color:#6b7280;font-size:12px"">Por seguridad, no comparta la información de su tarjeta con terceros.</p>");

        // ---------- Modificacion de limite de tarjeta ----------
        public const string CardLimitUpdatedSubject = "Modificación de límite de tarjeta";

        public static string CardLimitUpdatedBody(string clientName, string cardLastFour, decimal newLimit,
            DateTime moment) => Wrap(clientName, $@"
  <p>El límite de su tarjeta de crédito terminada en <b>{cardLastFour}</b> ha sido actualizado.</p>
  <p>Nuevo límite aprobado: <b>{Money.Format(newLimit)}</b><br/>
     Fecha de modificación: {When(moment)}</p>");

        // ---------- Avance de efectivo ----------
        public static string CashAdvanceSubject(string cardLastFour) => $"Avance de efectivo desde la tarjeta {cardLastFour}";

        public static string CashAdvanceBody(string clientName, decimal advanceAmount, decimal interest,
            decimal totalCharged, string cardLastFour, string accountLastFour, DateTime moment)
            => Wrap(clientName, $@"
  <p>Se ha realizado un avance de efectivo desde su tarjeta terminada en <b>{cardLastFour}</b>.</p>
  <p>Monto recibido en su cuenta terminada en <b>{accountLastFour}</b>: <b>{Money.Format(advanceAmount)}</b><br/>
     Interés aplicado (6.25%): <b>{Money.Format(interest)}</b><br/>
     Total cargado a la tarjeta: <b>{Money.Format(totalCharged)}</b><br/>
     Fecha y hora: {When(moment)}</p>");

        // ---------- Hermes Pay: consumo del cliente ----------
        public static string HermesConsumptionSubject(string cardLastFour) => $"Consumo realizado con la tarjeta {cardLastFour}";

        public static string HermesConsumptionBody(string clientName, decimal amount, string cardLastFour,
            string commerceName, DateTime moment) => Wrap(clientName, $@"
  <p>Se ha realizado un consumo con su tarjeta terminada en <b>{cardLastFour}</b>.</p>
  <p>Comercio: <b>{commerceName}</b><br/>
     Monto: <b>{Money.Format(amount)}</b><br/>
     Fecha y hora: {When(moment)}</p>");

        // ---------- Hermes Pay: pago recibido por el comercio ----------
        public static string HermesPaymentReceivedSubject(string cardLastFour) => $"Pago recibido a través de tarjeta {cardLastFour}";

        public static string HermesPaymentReceivedBody(string commerceName, decimal amount, string cardLastFour,
            DateTime moment) => Wrap(commerceName, $@"
  <p>Ha recibido un nuevo pago mediante Hermes Pay.</p>
  <p>Tarjeta terminada en: <b>{cardLastFour}</b><br/>
     Monto recibido: <b>{Money.Format(amount)}</b><br/>
     Fecha y hora: {When(moment)}</p>
  <p style=""color:#6b7280;font-size:12px"">Este mensaje sirve como constancia del pago recibido.</p>");
    }
}
