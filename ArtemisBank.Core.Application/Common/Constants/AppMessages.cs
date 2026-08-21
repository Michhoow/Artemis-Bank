namespace ArtemisBank.Core.Application.Common.Constants
{
    public static class AppMessages
    {
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

        public const string NoActiveFinancialProducts = "No posee productos financieros activos.";

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

        public const string NeedTwoActiveAccounts =
            "Debe tener al menos dos cuentas de ahorro activas para realizar una transferencia entre cuentas.";
        public const string TransferAmountGreaterThanZero = "El monto a transferir debe ser mayor que cero.";

        public const string DepositAmountGreaterThanZero = "El monto a depositar debe ser mayor que cero.";
        public const string WithdrawalAmountGreaterThanZero = "El monto a retirar debe ser mayor que cero.";
        public const string PaymentAmountGreaterThanZero = "El monto a pagar debe ser mayor que cero.";
        public const string TransactionAmountGreaterThanZero = "El monto de la transacción debe ser mayor que cero.";
        public const string InvalidOriginAccount = "El número de cuenta origen ingresado no corresponde a una cuenta válida.";
        public const string InvalidTargetAccount = "El número de cuenta destino ingresado no corresponde a una cuenta válida.";
        public const string InvalidCardNumber = "El número de tarjeta ingresado no corresponde a una tarjeta válida.";
        public const string InvalidLoanNumber = "El número de préstamo ingresado no corresponde a un préstamo válido.";
        public const string SameAccountCashier = "La cuenta origen y la cuenta destino no pueden ser la misma.";

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

        public const string RejectedInsufficientFunds = "Fondos insuficientes";
        public const string RejectedCancelledAccount = "Cuenta cancelada";
        public const string RejectedInvalidProduct = "Producto inválido";

        public const string LoanClientRequired = "El cliente seleccionado es requerido.";
        public const string LoanClientNotFound = "El cliente seleccionado no existe.";
        public const string LoanOnlyActiveClients = "Solo se puede asignar préstamos a clientes activos.";
        public const string LoanClientAlreadyHasActiveLoan =
            "El cliente seleccionado ya tiene un préstamo activo.";
        public const string LoanClientNeedsPrincipalAccount =
            "El cliente debe tener una cuenta de ahorro principal activa para recibir el desembolso.";
        public const string LoanInvalidTerm = "El plazo seleccionado no es válido.";
        public const string LoanAmountGreaterThanZero = "El monto a prestar debe ser mayor que cero.";
        public const string LoanNegativeRate = "La tasa de interés anual no puede ser negativa.";
        public const string LoanNotFound = "El préstamo seleccionado no existe.";
        public const string LoanNotActive = "El préstamo seleccionado no se encuentra activo.";
        public const string LoanNoFutureInstallments =
            "No existen cuotas futuras pendientes para recalcular el préstamo.";
        public const string LoanCouldNotGenerateNumber = "No fue posible generar un número de préstamo único.";
        public const string LoanCurrentHighRisk =
            "Este cliente se considera de alto riesgo, ya que su deuda actual supera el promedio del sistema.";
        public const string LoanProjectedHighRisk =
            "Asignar este préstamo convertirá al cliente en un cliente de alto riesgo, " +
            "ya que su deuda superará el umbral promedio del sistema.";
        public const string LoanCreated = "Préstamo asignado correctamente.";
        public const string LoanRateUpdated = "La tasa de interés fue modificada correctamente.";
        public const string LoanConfirmAssignment = "¿Está seguro que desea asignar este préstamo?";

        public const string CardClientRequired = "El cliente seleccionado es requerido.";
        public const string CardOnlyActiveClients = "Solo se puede asignar tarjetas de crédito a clientes activos.";
        public const string CardLimitGreaterThanZero = "El límite de crédito debe ser mayor que cero.";
        public const string CardNotFound = "La tarjeta seleccionada no existe.";
        public const string CardNotActive = "La tarjeta seleccionada no se encuentra activa.";
        public const string CardExpired = "La tarjeta seleccionada se encuentra vencida.";
        public const string CardLimitBelowDebt =
            "El nuevo límite de crédito no puede ser menor que la deuda actual de la tarjeta.";
        public const string CardCannotCancelWithDebt =
            "No es posible cancelar la tarjeta porque tiene deuda pendiente.";
        public const string CardAlreadyCancelled = "La tarjeta seleccionada ya se encuentra cancelada.";
        public const string CardCouldNotGenerateNumber = "No fue posible generar un número de tarjeta único.";
        public const string CardCreated = "Tarjeta de crédito asignada correctamente.";
        public const string CardLimitUpdated = "El límite de crédito fue modificado correctamente.";
        public const string CardCancelled = "La tarjeta de crédito fue cancelada correctamente.";
        public const string CardConfirmCancel = "¿Está seguro que desea cancelar esta tarjeta de crédito?";

        public const string AdvanceAmountGreaterThanZero = "El monto del avance debe ser mayor que cero.";
        public const string AdvanceInsufficientCredit =
            "El monto solicitado excede el crédito disponible de la tarjeta seleccionada.";
        public const string AdvanceTargetAccountRequired = "Debe seleccionar la cuenta de destino del avance.";
        public const string AdvanceTargetAccountInvalid =
            "La cuenta de destino seleccionada no corresponde a una cuenta activa suya.";
        public const string AdvanceNoActiveCards = "No posee tarjetas de crédito activas.";
        public const string ConfirmCashAdvance = "¿Está seguro que desea realizar este avance de efectivo?";

        public const string CommerceNotFound = "El comercio seleccionado no existe.";
        public const string CommerceRncAlreadyExists = "El RNC indicado ya se encuentra registrado.";
        public const string CommerceEmailAlreadyExists = "El correo indicado ya se encuentra registrado.";
        public const string CommerceInactive = "El comercio se encuentra inactivo y no puede procesar pagos.";
        public const string CommerceAlreadyHasUser = "El comercio indicado ya tiene un usuario asociado.";
        public const string CommerceWithoutPrincipalAccount =
            "El comercio no tiene una cuenta de ahorro principal activa para recibir los fondos.";

        public const string InvalidCredentials = "Los datos de acceso son inválidos.";
        public const string AccountNotConfirmed =
            "Su cuenta se encuentra inactiva. Debe activar su cuenta antes de iniciar sesión.";
        public const string AccountInactive = "Su cuenta se encuentra inactiva.";

        public const string AccessDenied = "No posee permisos para acceder a esta sección.";

        public const string CommerceRoleWebAppNotAllowed =
            "El rol Comercio no tiene acceso a la aplicación web. Utilice la Web API con su token JWT.";
        public const string PasswordResetSent =
            "Si el correo existe en nuestro sistema, recibirá un enlace para restablecer su contraseña.";
        public const string PasswordResetSuccess =
            "Contraseña restablecida correctamente. Ya puede iniciar sesión.";
        public const string AccountActivated =
            "Cuenta activada exitosamente. Ya puede iniciar sesión.";
        public const string ActivationParamsInvalid =
            "El enlace de activación no es válido o ya fue utilizado.";

        public const string HermesInvalidCardNumber = "El número de tarjeta debe contener 16 dígitos.";
        public const string HermesInvalidCvc = "El código de seguridad indicado no es válido.";
        public const string HermesAmountGreaterThanZero = "El monto de la transacción debe ser mayor que cero.";
        public const string HermesInsufficientCredit =
            "La transacción fue rechazada por falta de crédito disponible en la tarjeta.";
        public const string HermesApproved = "Pago aprobado.";
        public const string HermesInvalidExpirationMonth =
            "El mes de expiración debe tener un valor válido entre 01 y 12.";
        public const string HermesInvalidExpirationYear = "El año de expiración indicado no es válido.";
        public const string HermesExpirationMismatch =
            "Los datos de expiración no corresponden a la tarjeta indicada.";
        public const string HermesCommerceNotAssociated =
            "El usuario autenticado no está asociado a ningún comercio.";

        public const string UserNameAlreadyExists = "El nombre de usuario indicado ya se encuentra registrado.";
        public const string UserEmailAlreadyExists = "El correo indicado ya se encuentra registrado.";
        public const string UserIdentificationAlreadyExists = "La cédula indicada ya se encuentra registrada.";
        public const string UserPasswordsDoNotMatch = "La contraseña y su confirmación no coinciden.";
        public const string UserCannotModifySelf = "No puede activar o inactivar su propio usuario.";
        public const string UserRoleCannotBeChanged = "El rol del usuario no puede ser modificado.";
        public const string UserNotFound = "El usuario seleccionado no existe.";
        public const string UserActivationEmailFailed =
            "No fue posible enviar el correo de activación. Intente nuevamente más tarde.";
        public const string UserCreated = "Usuario creado correctamente.";
        public const string UserUpdated = "Usuario actualizado correctamente.";
        public const string UserActivated = "Usuario activado correctamente.";
        public const string UserDeactivated = "Usuario inactivado correctamente.";
    }
}
