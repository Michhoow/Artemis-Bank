namespace ArtemisBank.Core.Domain.Common.Enums
{
    public enum TransactionOperation
    {
        BalanceInicial = 1,
        Deposito = 2,
        Retiro = 3,
        TransferenciaEntreCuentas = 4,
        TransaccionExpress = 5,
        TransaccionBeneficiario = 6,
        TransaccionTerceros = 7,
        PagoTarjeta = 8,
        PagoPrestamo = 9,
        AvanceEfectivo = 10,
        DesembolsoPrestamo = 11,
        CancelacionCuenta = 12,
        PagoComercio = 13
    }
}
