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

        // =================================================================================
        //  Productos de credito y pagos (Manuel)
        // =================================================================================

        // ---------- Prestamos ----------
        public const string ClientAlreadyHasActiveLoan = "Este cliente ya tiene un préstamo activo asignado.";
        public const string InvalidLoanTerm = "El plazo seleccionado no es válido.";
        public const string LoanAmountGreaterThanZero = "El monto a prestar debe ser mayor que cero.";
        public const string NegativeInterestRate = "La tasa de interés anual no puede ser negativa.";
        public const string ClientHasNoLoans = "Este cliente no tiene préstamos registrados.";
        public const string LoanDoesNotExist = "El préstamo seleccionado no existe.";
        public const string OnlyActiveLoanRateEditable = "Solo se puede modificar la tasa de interés de préstamos activos.";
        public const string NoFutureInstallmentsToRecalculate = "No existen cuotas futuras pendientes para recalcular.";
        public const string ClientNeedsPrincipalForDisbursement =
            "El cliente no tiene una cuenta de ahorro principal activa para recibir el desembolso del préstamo.";
        public const string CurrentHighRisk =
            "Este cliente se considera de alto riesgo, ya que su deuda actual supera el promedio del sistema.";
        public const string ProjectedHighRisk =
            "Asignar este préstamo convertirá al cliente en un cliente de alto riesgo, ya que su deuda superará el umbral promedio del sistema.";
        public const string LoanCreatedMailFailed =
            "El préstamo fue creado correctamente, pero no fue posible enviar el correo de notificación.";
        public const string CouldNotGenerateLoanNumber = "No fue posible generar un número de préstamo único.";

        // ---------- Tarjetas de credito ----------
        public const string CreditLimitGreaterThanZero = "El límite de crédito debe ser mayor que cero.";
        public const string OnlyActiveClientsForCard = "Solo se puede asignar tarjetas de crédito a clientes activos.";
        public const string ClientHasNoCards = "Este cliente no tiene tarjetas de crédito registradas.";
        public const string CardDoesNotExist = "La tarjeta seleccionada no existe.";
        public const string CannotModifyCancelledCard = "No se puede modificar una tarjeta cancelada.";
        public const string NewLimitBelowDebt = "El límite de la tarjeta no puede ser inferior al monto adeudado actualmente.";
        public const string CardHasPendingDebt = "Para cancelar esta tarjeta, el cliente debe saldar la totalidad de la deuda pendiente.";
        public const string CardCreatedMailFailed =
            "La tarjeta fue creada correctamente, pero no fue posible enviar el correo de notificación.";
        public const string CardLimitUpdatedMailFailed =
            "El límite fue actualizado correctamente, pero no fue posible enviar el correo de notificación.";
        public const string CouldNotGenerateCardNumber = "No fue posible generar un número de tarjeta único.";

        // ---------- Avance de efectivo ----------
        public const string CardNotActive = "La tarjeta seleccionada no se encuentra activa.";
        public const string CardExpired = "La tarjeta seleccionada se encuentra vencida.";
        public const string AdvanceAccountNotActive = "La cuenta de ahorro seleccionada no se encuentra activa.";
        public const string AdvanceAmountGreaterThanZero = "El monto del avance debe ser mayor que cero.";
        public const string AdvanceExceedsAvailableCredit =
            "El avance solicitado excede el crédito disponible de la tarjeta seleccionada.";

        // ---------- Hermes Pay ----------
        public const string PaymentExceedsAvailableCredit =
            "El monto de la transacción excede el crédito disponible de la tarjeta.";
        public const string InvalidCardData =
            "Los datos de la tarjeta ingresados no corresponden a una tarjeta válida.";
        public const string CommerceNotFound = "El comercio indicado no existe.";
        public const string CommerceInactive = "El comercio indicado se encuentra inactivo.";
        public const string CommerceHasNoUser = "El comercio no tiene un usuario asociado.";
        public const string CommerceUserHasNoPrincipal =
            "El comercio no tiene una cuenta de ahorro principal activa para recibir el pago.";
        public const string CommerceUserWithoutCommerce = "El usuario de comercio no tiene un comercio asociado.";
    }
}
