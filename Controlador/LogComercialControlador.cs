using System.Data;
using AppIsp.Modelo;
using MySqlConnector;

namespace AppIsp.Controlador;

/// <summary>
/// Auditoría comercial. Las altas y bajas las escribe ServicioControlador.
/// Este controlador sirve para consultarlas y, si hace falta, borrarlas (solo maestro).
/// </summary>
public static class LogComercialControlador
{
    public static DataTable Leer()
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.VerAuditoria, "Solo el maestro ve la auditoría comercial.");
        if (bloqueo != null) throw new InvalidOperationException(bloqueo.Mensaje);

        return ConexionMySql.Consultar(
            @"SELECT l.id_log, c.nombre AS cliente, p.nombre AS plan, u.nombre AS usuario,
                     l.tipo_cambio, l.detalle, l.fecha_cambio
              FROM log_comercial l
              JOIN servicios s ON s.id_servicio = l.id_servicio
              JOIN clientes c ON c.id_cliente = s.id_cliente
              JOIN planes p ON p.id_plan = s.id_plan
              JOIN usuarios u ON u.id_usuario = l.id_usuario
              ORDER BY l.id_log DESC");
    }

    public static LogComercial? LeerPorId(int id)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.VerAuditoria, "Solo el maestro ve la auditoría comercial.");
        if (bloqueo != null) throw new InvalidOperationException(bloqueo.Mensaje);
        return ConexionMySql.LeerPrimero(
            "SELECT * FROM log_comercial WHERE id_log = @id",
            Mapear,
            ConexionMySql.P("@id", id));
    }

    public static Respuesta Crear(LogComercial log)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.AltaServicio, "Su rol no registra cambios comerciales.");
        if (bloqueo != null) return bloqueo;
        if (log.IdServicio <= 0) return Respuesta.Fallo("Falta el servicio.");
        var error = Validador.EnLista(log.TipoCambio, Catalogos.TiposCambioComercial, "Tipo de cambio");
        if (error != null) return Respuesta.Fallo(error);

        try
        {
            ConexionMySql.Insertar(
                @"INSERT INTO log_comercial (id_servicio, id_usuario, tipo_cambio, detalle)
                  VALUES (@servicio, @usuario, @tipo, @detalle)",
                ConexionMySql.P("@servicio", log.IdServicio),
                ConexionMySql.P("@usuario", SesionActual.IdUsuario),
                ConexionMySql.P("@tipo", log.TipoCambio),
                ConexionMySql.P("@detalle", Validador.Limpio(log.Detalle)));
            return Respuesta.Ok("Cambio comercial registrado.");
        }
        catch (Exception ex)
        {
            return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex));
        }
    }

    public static Respuesta Actualizar(LogComercial log)
    {
        return Respuesta.Fallo("Los registros comerciales no se modifican.");
    }

    public static Respuesta Eliminar(int id)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.EsRolMaestro, "Solo el maestro puede borrar la auditoría.");
        if (bloqueo != null) return bloqueo;
        try
        {
            var filas = ConexionMySql.Ejecutar("DELETE FROM log_comercial WHERE id_log = @id", ConexionMySql.P("@id", id));
            if (filas == 0) return Respuesta.Fallo("No se encontró el registro.");
            return Respuesta.Ok("Registro eliminado.");
        }
        catch (Exception ex)
        {
            return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex));
        }
    }

    private static LogComercial Mapear(MySqlDataReader lector)
    {
        return new LogComercial
        {
            IdLog = LectorFila.Entero(lector, "id_log"),
            IdServicio = LectorFila.Entero(lector, "id_servicio"),
            IdUsuario = LectorFila.Entero(lector, "id_usuario"),
            TipoCambio = LectorFila.Texto(lector, "tipo_cambio"),
            Detalle = LectorFila.TextoNulo(lector, "detalle"),
            FechaCambio = LectorFila.Fecha(lector, "fecha_cambio")
        };
    }
}
