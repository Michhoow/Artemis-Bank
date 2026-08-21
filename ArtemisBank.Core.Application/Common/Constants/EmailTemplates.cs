using ArtemisBank.Core.Application.Common.Models;

namespace ArtemisBank.Core.Application.Common.Constants
{
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

        public static string SentSubject(string targetLastFour) => $"Transacción realizada a la cuenta {targetLastFour}";

        public static string SentBody(string clientName, decimal amount, string sourceLastFour,
            string targetLastFour, DateTime moment) => Wrap(clientName, $@"
  <p>Se ha realizado una transacción desde su cuenta terminada en <b>{sourceLastFour}</b>.</p>
  <p>Monto transferido: <b>{Money.Format(amount)}</b><br/>
     Cuenta destino terminada en: <b>{targetLastFour}</b><br/>
     Fecha y hora: {When(moment)}</p>");

        public static string ReceivedSubject(string sourceLastFour) => $"Transacción enviada desde la cuenta {sourceLastFour}";

        public static string ReceivedBody(string clientName, decimal amount, string sourceLastFour,
            string targetLastFour, DateTime moment) => Wrap(clientName, $@"
  <p>Ha recibido una transacción en su cuenta terminada en <b>{targetLastFour}</b>.</p>
  <p>Monto recibido: <b>{Money.Format(amount)}</b><br/>
     Cuenta origen terminada en: <b>{sourceLastFour}</b><br/>
     Fecha y hora: {When(moment)}</p>");

        public const string TransferSubject = "Transferencia entre cuentas realizada";

        public static string TransferBody(string clientName, decimal amount, string sourceLastFour,
            string targetLastFour, DateTime moment) => Wrap(clientName, $@"
  <p>Se ha realizado una transferencia entre sus cuentas de ahorro.</p>
  <p>Cuenta origen terminada en: <b>{sourceLastFour}</b><br/>
     Cuenta destino terminada en: <b>{targetLastFour}</b><br/>
     Monto transferido: <b>{Money.Format(amount)}</b><br/>
     Fecha y hora: {When(moment)}</p>");

        public static string DepositSubject(string lastFour) => $"Depósito realizado a su cuenta {lastFour}";

        public static string DepositBody(string clientName, decimal amount, string lastFour, DateTime moment)
            => Wrap(clientName, $@"
  <p>Se ha realizado un depósito a su cuenta terminada en <b>{lastFour}</b>.</p>
  <p>Monto depositado: <b>{Money.Format(amount)}</b><br/>
     Fecha y hora: {When(moment)}</p>");

        public static string WithdrawalSubject(string lastFour) => $"Retiro realizado desde su cuenta {lastFour}";

        public static string WithdrawalBody(string clientName, decimal amount, string lastFour, DateTime moment)
            => Wrap(clientName, $@"
  <p>Se ha realizado un retiro desde su cuenta terminada en <b>{lastFour}</b>.</p>
  <p>Monto retirado: <b>{Money.Format(amount)}</b><br/>
     Fecha y hora: {When(moment)}</p>");

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

        public static string LoanPaymentSubject(string loanNumber) => $"Pago realizado al préstamo {loanNumber}";

        public static string LoanPaymentBody(string clientName, decimal amount, string loanNumber,
            string accountLastFour, DateTime moment) => Wrap(clientName, $@"
  <p>Se ha realizado un pago a su préstamo <b>{loanNumber}</b>.</p>
  <p>Monto pagado: <b>{Money.Format(amount)}</b><br/>
     Cuenta origen terminada en: <b>{accountLastFour}</b><br/>
     Fecha y hora: {When(moment)}</p>");

        public static string LoanApprovedSubject(string loanNumber) => $"Préstamo {loanNumber} aprobado";

        public static string LoanApprovedBody(string clientName, string loanNumber, decimal capital,
            decimal monthlyInstallment, int termInMonths, decimal annualRate, string accountLastFour,
            DateTime moment) => Wrap(clientName, $@"
  <p>Su solicitud de préstamo ha sido aprobada.</p>
  <p>Número de préstamo: <b>{loanNumber}</b><br/>
     Monto aprobado: <b>{Money.Format(capital)}</b><br/>
     Cuota mensual: <b>{Money.Format(monthlyInstallment)}</b><br/>
     Plazo: <b>{termInMonths} meses</b><br/>
     Tasa de interés anual: <b>{annualRate:N2} %</b><br/>
     Desembolsado a su cuenta terminada en: <b>{accountLastFour}</b><br/>
     Fecha y hora: {When(moment)}</p>");

        public static string LoanRateChangedSubject(string loanNumber)
            => $"Cambio de tasa en su préstamo {loanNumber}";

        public static string LoanRateChangedBody(string clientName, string loanNumber, decimal newRate,
            decimal newMonthlyInstallment, int recalculatedInstallments, DateTime moment)
            => Wrap(clientName, $@"
  <p>Se ha modificado la tasa de interés de su préstamo <b>{loanNumber}</b>.</p>
  <p>Nueva tasa de interés anual: <b>{newRate:N2} %</b><br/>
     Nueva cuota mensual: <b>{Money.Format(newMonthlyInstallment)}</b><br/>
     Cuotas futuras recalculadas: <b>{recalculatedInstallments}</b><br/>
     Fecha y hora: {When(moment)}</p>
  <p>Las cuotas ya pagadas, parcialmente pagadas o vencidas no fueron modificadas.</p>");

        public static string CardAssignedSubject(string cardLastFour)
            => $"Tarjeta de crédito {cardLastFour} asignada";

        public static string CardAssignedBody(string clientName, string cardLastFour, decimal creditLimit,
            string expiration, DateTime moment) => Wrap(clientName, $@"
  <p>Se le ha asignado una nueva tarjeta de crédito.</p>
  <p>Tarjeta: <b>**** **** **** {cardLastFour}</b><br/>
     Límite de crédito: <b>{Money.Format(creditLimit)}</b><br/>
     Fecha de expiración: <b>{expiration}</b><br/>
     Fecha y hora: {When(moment)}</p>
  <p>Por su seguridad, el código de seguridad no se incluye en este mensaje.</p>");

        public static string CardLimitChangedSubject(string cardLastFour)
            => $"Cambio de límite en su tarjeta {cardLastFour}";

        public static string CardLimitChangedBody(string clientName, string cardLastFour,
            decimal newLimit, DateTime moment) => Wrap(clientName, $@"
  <p>Se ha modificado el límite de crédito de su tarjeta terminada en <b>{cardLastFour}</b>.</p>
  <p>Nuevo límite de crédito: <b>{Money.Format(newLimit)}</b><br/>
     Fecha y hora: {When(moment)}</p>");

        public static string CardCancelledSubject(string cardLastFour)
            => $"Tarjeta de crédito {cardLastFour} cancelada";

        public static string CardCancelledBody(string clientName, string cardLastFour, DateTime moment)
            => Wrap(clientName, $@"
  <p>Su tarjeta de crédito terminada en <b>{cardLastFour}</b> ha sido cancelada.</p>
  <p>Fecha y hora: {When(moment)}</p>");

        public static string CashAdvanceSubject(string cardLastFour)
            => $"Avance de efectivo desde su tarjeta {cardLastFour}";

        public static string CashAdvanceBody(string clientName, decimal amount, decimal interest,
            decimal totalCharged, string cardLastFour, string accountLastFour, DateTime moment)
            => Wrap(clientName, $@"
  <p>Se ha realizado un avance de efectivo desde su tarjeta terminada en <b>{cardLastFour}</b>.</p>
  <p>Monto acreditado a su cuenta: <b>{Money.Format(amount)}</b><br/>
     Interés aplicado (6.25 %): <b>{Money.Format(interest)}</b><br/>
     Total cargado a la tarjeta: <b>{Money.Format(totalCharged)}</b><br/>
     Cuenta destino terminada en: <b>{accountLastFour}</b><br/>
     Fecha y hora: {When(moment)}</p>");

        public static string CommercePaymentClientSubject(string commerceName)
            => $"Consumo aprobado en {commerceName}";

        public static string CommercePaymentClientBody(string clientName, decimal amount,
            string commerceName, string cardLastFour, DateTime moment) => Wrap(clientName, $@"
  <p>Se ha registrado un consumo con su tarjeta terminada en <b>{cardLastFour}</b>.</p>
  <p>Comercio: <b>{commerceName}</b><br/>
     Monto consumido: <b>{Money.Format(amount)}</b><br/>
     Fecha y hora: {When(moment)}</p>");

        public static string CommercePaymentReceivedSubject(string accountLastFour)
            => $"Pago acreditado a su cuenta {accountLastFour}";

        public static string CommercePaymentReceivedBody(string commerceName, decimal amount,
            string accountLastFour, string cardLastFour, DateTime moment) => Wrap(commerceName, $@"
  <p>Se ha acreditado un pago a su cuenta terminada en <b>{accountLastFour}</b>.</p>
  <p>Monto acreditado: <b>{Money.Format(amount)}</b><br/>
     Tarjeta terminada en: <b>{cardLastFour}</b><br/>
     Fecha y hora: {When(moment)}</p>");

        public const string ActivationSubject = "Activación de cuenta";

        public static string ActivationLinkBody(string userName, string activationLink)
            => Wrap(userName, $@"
  <p>Su cuenta ha sido creada correctamente en Artemis Banking.</p>
  <p>Para activar su usuario, haga clic en el siguiente enlace:<br/>
     <a href=""{activationLink}"">Activar mi cuenta</a></p>
  <p>Si usted no esperaba la creación de esta cuenta, ignore este mensaje.</p>");

        public const string ActivationTokenSubject = "Token de activación de cuenta";

        public static string ActivationTokenBody(string userName, string token)
            => Wrap(userName, $@"
  <p>Su cuenta ha sido creada correctamente en Artemis Banking.</p>
  <p>Utilice el siguiente token para activar su cuenta desde el endpoint correspondiente:</p>
  <p style=""font-family:Consolas,monospace;word-break:break-all""><b>{token}</b></p>
  <p>Si usted no esperaba la creación de esta cuenta, ignore este mensaje.</p>");

    }
}
