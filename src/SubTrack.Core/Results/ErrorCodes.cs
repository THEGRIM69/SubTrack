namespace SubTrack.Core.Results;

/// <summary>
/// F0-15 — Catálogo de códigos de error estables.
/// No se renombran ni se reutilizan para otro significado.
/// </summary>
public static class ErrorCodes
{
    // Validación
    public const string NombreRequerido = "NOMBRE_REQUERIDO";
    public const string NombreDemasiadoLargo = "NOMBRE_DEMASIADO_LARGO";
    public const string ImporteInvalido = "IMPORTE_INVALIDO";
    public const string FechaRequerida = "FECHA_REQUERIDA";
    public const string FechaFuturaNoPermitida = "FECHA_FUTURA_NO_PERMITIDA";
    public const string RangoExcedido = "RANGO_EXCEDIDO";
    public const string MotivoRequerido = "MOTIVO_REQUERIDO";
    public const string DiaAnclaInvalido = "DIA_ANCLA_INVALIDO";
    public const string DiasDeIngresoInvalidos = "DIAS_DE_INGRESO_INVALIDOS";
    public const string CategoriaRequerida = "CATEGORIA_REQUERIDA";
    public const string EstadoInicialInvalido = "ESTADO_INICIAL_INVALIDO";
    public const string OperacionInvalida = "OPERACION_INVALIDA";

    // Suscripciones
    public const string SuscripcionNoEncontrada = "SUSCRIPCION_NO_ENCONTRADA";
    public const string SuscripcionInactiva = "SUSCRIPCION_INACTIVA";
    public const string SuscripcionYaInactiva = "SUSCRIPCION_YA_INACTIVA";
    public const string FechaEfectivaAnterior = "FECHA_EFECTIVA_ANTERIOR";
    public const string PeriodoCubiertoPorPago = "PERIODO_CUBIERTO_POR_PAGO";

    // Cobros programados
    public const string CobroNoEncontrado = "COBRO_NO_ENCONTRADO";
    public const string CobroYaPagado = "COBRO_YA_PAGADO";
    public const string CobroCancelado = "COBRO_CANCELADO";
    public const string CobroImporteNoCoincide = "COBRO_IMPORTE_NO_COINCIDE";
    public const string CobroConPagos = "COBRO_CON_PAGOS";
    public const string CobroDuplicado = "COBRO_DUPLICADO";
    public const string AjusteNoPermitido = "AJUSTE_NO_PERMITIDO";

    // Pagos
    public const string PagoNoEncontrado = "PAGO_NO_ENCONTRADO";
    public const string ComprobanteDuplicado = "COMPROBANTE_DUPLICADO";
    public const string OperacionYaProcesada = "OPERACION_YA_PROCESADA";

    // Gastos
    public const string GastoNoEncontrado = "GASTO_NO_ENCONTRADO";
    public const string GastoYaConfirmado = "GASTO_YA_CONFIRMADO";
    public const string GastoYaAnulado = "GASTO_YA_ANULADO";

    // Ingresos
    public const string IngresoNoEncontrado = "INGRESO_NO_ENCONTRADO";
    public const string IngresoYaConfirmado = "INGRESO_YA_CONFIRMADO";
    public const string IngresoYaAnulado = "INGRESO_YA_ANULADO";
    public const string PlantillaNoEncontrada = "PLANTILLA_NO_ENCONTRADA";

    // Categorías
    public const string CategoriaNoEncontrada = "CATEGORIA_NO_ENCONTRADA";
    public const string CategoriaInactiva = "CATEGORIA_INACTIVA";
    public const string CategoriaConGastos = "CATEGORIA_CON_GASTOS";
    public const string CategoriaDuplicada = "CATEGORIA_DUPLICADA";

    // Saldo inicial
    public const string SaldoInicialNoConfigurado = "SALDO_INICIAL_NO_CONFIGURADO";
    public const string SaldoInicialYaConfigurado = "SALDO_INICIAL_YA_CONFIGURADO";

    // Persistencia
    public const string BaseOcupada = "BASE_OCUPADA";
    public const string BaseCorrupta = "BASE_CORRUPTA";
    public const string BaseNoDisponible = "BASE_NO_DISPONIBLE";
    public const string ErrorInterno = "ERROR_INTERNO";
}
