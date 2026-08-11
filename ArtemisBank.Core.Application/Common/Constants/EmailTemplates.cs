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
    }
}
