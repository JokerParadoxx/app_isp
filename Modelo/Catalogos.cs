namespace AppIsp.Modelo;

/// <summary>
/// Valores permitidos por los ENUM de la base. Si se escribe otro texto,
/// MySQL lo rechaza, así que los combos se llenan solo con esta lista.
/// </summary>
public static class Catalogos
{
    public static readonly string[] EstadosCliente = { "ACTIVO", "SUSPENDIDO", "BAJA" };
    public static readonly string[] EstadosServicio = { "PENDIENTE_INSTALACION", "ACTIVO", "SUSPENDIDO", "BAJA" };
    public static readonly string[] EstadosRouter = { "ALMACEN", "ASIGNADO", "EN_FALLA", "RETIRADO" };
    public static readonly string[] EstadosInstalacion = { "PENDIENTE", "EN_PROCESO", "COMPLETADA", "CANCELADA" };
    public static readonly string[] BloquesAgenda =
    {
        "09:00-10:00", "10:00-11:00", "11:00-12:00", "12:00-13:00",
        "13:00-14:00", "14:00-15:00", "15:00-16:00", "16:00-17:00",
        "17:00-18:00", "18:00-19:00", "19:00-20:00"
    };
    public static readonly string[] EstadosFactura = { "PENDIENTE", "PAGADA", "VENCIDA", "ANULADA" };
    public static readonly string[] TiposTicket = { "TECNICO", "COMERCIAL" };
    public static readonly string[] Prioridades = { "BAJA", "MEDIA", "ALTA", "CRITICA" };
    public static readonly string[] EstadosTicket = { "ABIERTO", "EN_PROCESO", "RESUELTO", "CERRADO", "CANCELADO" };
    public static readonly string[] TiposCambioComercial = { "ALTA", "BAJA", "CAMBIO_PLAN", "SUSPENSION", "REACTIVACION" };
    public static readonly string[] AccionesProvision = { "REBOOT", "CAMBIO_IP", "PUSH_CONFIG" };

    /// <summary>
    /// Pasa el valor de la base a una frase más fácil de leer.
    /// Los bloques de agenda son rangos de una hora, de 09:00 a 20:00.
    /// Mañana, tarde y noche quedan solo por si alguna fila antigua aún los tiene.
    /// </summary>
    public static string TextoAmigable(string? valor)
    {
        if (string.IsNullOrEmpty(valor))
            return "";

        return valor switch
        {
            "MANANA" => "Mañana",
            "PENDIENTE_INSTALACION" => "Pendiente de instalación",
            "EN_PROCESO" => "En proceso",
            "EN_FALLA" => "En falla",
            "CAMBIO_PLAN" => "Cambio de plan",
            "PUSH_CONFIG" => "Enviar configuración",
            "CAMBIO_IP" => "Cambio de IP",
            "REBOOT" => "Reinicio",
            "ABIERTA" => "Abierta",
            "DESACTIVADO" => "Desactivado",
            "ESTATICA" => "Estática",
            "PERMITIR" => "Permitir solo estas MAC",
            "BLOQUEAR" => "Bloquear estas MAC",
            "AMBOS" => "TCP y UDP",
            "EN_LINEA" => "En línea",
            "SIN_RESPUESTA" => "Sin respuesta",
            "DEGRADADA" => "Degradada",
            "SLAAC" => "Automática (SLAAC)",
            "CRITICA" => "Crítica",
            _ => valor.Replace('_', ' ')
        };
    }
}
