namespace ArtemisBank.Core.Application.Common.Constants
{
    /// <summary>
    /// Mensajes literales exigidos por el documento funcional.
    /// Centralizarlos evita divergencias entre WebApp, WebApi y pruebas.
    /// </summary>
    public static class AppMessages
    {
        // ---------- Cuentas de ahorro (administrador) ----------
        public const string ClientNotFoundByIdentification = "No existe un cliente registrado con esta cédula.";
        public const string ClientWithoutAccounts = "Este cliente no tiene cuentas de ahorro registradas.";
        public const string MustSelectClient = "Debe seleccionar un cliente para continuar.";
        public const string OnlyActiveClients = "Solo se puede asignar cuentas de ahorro a clientes activos.";
        public const string ClientNeedsPrincipalAccount =
            "El cliente debe tener una cuenta de ahorro principal activa antes de asignarle una cuenta secundaria.";
        public const string NegativeInitialBalance = "El balance inicial no puede ser negativo.";
        public const string AccountDoesNotExist = "La cuenta seleccionada no existe.";
        public const string AccountAlreadyCancelled = "La cuenta seleccionada ya se encuentra cancelada.";
        public const string PrincipalCannotBeCancelled = "Las cuentas principales no pueden ser canceladas.";
        public const string NoPrincipalToReceiveFunds =
            "No es posible cancelar la cuenta porque el cliente no tiene una cuenta principal activa para recibir los fondos.";
        public const string CouldNotGenerateAccountNumber = "No fue posible generar un número de cuenta único.";

        // ---------- Cliente ----------
        public const string NoActiveFinancialProducts = "No posee productos financieros activos.";

        // ---------- Beneficiarios ----------
        public const string InvalidAccountNumber = "El número de cuenta ingresado no corresponde a una cuenta válida.";
        public const string CancelledAccountAsBeneficiary = "No puede agregar una cuenta cancelada como beneficiario.";
        public const string OwnAccountAsBeneficiary =
            "No puede agregar una cuenta propia como beneficiario. Utilice la opción Transferencia para mover fondos entre sus cuentas.";
        public const string BeneficiaryAlreadyRegistered = "Esta cuenta ya se encuentra registrada como beneficiario.";
        public const string BeneficiaryAdded = "Beneficiario agregado correctamente.";
        public const string BeneficiaryDeleted = "Beneficiario eliminado correctamente.";
        public const string NoBeneficiariesRegistered = "No tiene beneficiarios registrados.";
        public const string BeneficiaryAccountUnavailable = "La cuenta del beneficiario no se encuentra disponible.";
        public const string ConfirmDeleteBeneficiary = "¿Está seguro que desea eliminar este beneficiario?";

        // ---------- Transacciones ----------
        public const string InsufficientFundsExpress = "El monto ingresado excede el saldo disponible de la cuenta seleccionada.";
        public const string InsufficientFundsAccount = "El monto ingresado excede el saldo disponible de la cuenta.";
        public const string InsufficientFundsGeneric = "No dispone del monto requerido en la cuenta seleccionada.";
        public const string InsufficientFundsBeneficiary = "No dispone de fondos suficientes para realizar esta transacción.";
        public const string SameAccountNotAllowed = "La cuenta destino no puede ser la misma cuenta de origen.";
        public const string SameAccountTransferNotAllowed = "La cuenta de origen y la cuenta de destino no pueden ser la misma.";
        public const string CardWithoutDebt = "La tarjeta seleccionada no tiene deuda pendiente.";
        public const string LoanWithoutPendingInstallments = "El préstamo seleccionado no tiene cuotas pendientes de pago.";
        public const string ConfirmTransaction = "¿Está seguro de que desea realizar esta transacción?";
        public const string ConfirmTransfer = "¿Está seguro que desea realizar esta transferencia?";
        public const string ConfirmDeposit = "¿Está seguro que desea realizar este depósito?";
        public const string ConfirmWithdrawal = "¿Está seguro que desea realizar este retiro?";
        public const string ConfirmPayment = "¿Está seguro que desea realizar este pago?";

        // ---------- Transferencia entre cuentas propias ----------
        public const string NeedTwoActiveAccounts =
            "Debe tener al menos dos cuentas de ahorro activas para realizar una transferencia entre cuentas.";
        public const string TransferAmountGreaterThanZero = "El monto a transferir debe ser mayor que cero.";

        // ---------- Cajero ----------
        public const string DepositAmountGreaterThanZero = "El monto a depositar debe ser mayor que cero.";
        public const string WithdrawalAmountGreaterThanZero = "El monto a retirar debe ser mayor que cero.";
        public const string PaymentAmountGreaterThanZero = "El monto a pagar debe ser mayor que cero.";
        public const string TransactionAmountGreaterThanZero = "El monto de la transacción debe ser mayor que cero.";
        public const string InvalidOriginAccount = "El número de cuenta origen ingresado no corresponde a una cuenta válida.";
        public const string InvalidTargetAccount = "El número de cuenta destino ingresado no corresponde a una cuenta válida.";
        public const string InvalidCardNumber = "El número de tarjeta ingresado no corresponde a una tarjeta válida.";
        public const string InvalidLoanNumber = "El número de préstamo ingresado no corresponde a un préstamo válido.";
        public const string SameAccountCashier = "La cuenta origen y la cuenta destino no pueden ser la misma.";

        // ---------- Correos ----------
        public const string TransactionOkMailFailed =
            "La transacción fue realizada correctamente, pero no fue posible enviar una o más notificaciones por correo.";
        public const string TransferOkMailFailed =
            "La transferencia fue realizada correctamente, pero no fue posible enviar el correo de notificación.";
        public const string DepositOkMailFailed =
            "El depósito fue realizado correctamente, pero no fue posible enviar el correo de notificación.";
        public const string WithdrawalOkMailFailed =
            "El retiro fue realizado correctamente, pero no fue posible enviar el correo de notificación.";
        public const string PaymentOkMailFailed =
            "El pago fue realizado correctamente, pero no fue posible enviar el correo de notificación.";

        // ---------- Rechazos registrados en el historial ----------
        public const string RejectedInsufficientFunds = "Fondos insuficientes";
        public const string RejectedCancelledAccount = "Cuenta cancelada";
        public const string RejectedInvalidProduct = "Producto inválido";
    }
}
