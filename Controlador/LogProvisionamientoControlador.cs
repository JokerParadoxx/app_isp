using System.Data;
using AppIsp.Modelo;
using MySqlConnector;

namespace AppIsp.Controlador;

/// <summary>
/// Auditoría técnica. Crear lo usa el aprovisionamiento.
/// Actualizar no se permite: un log no se reescribe. El maestro puede eliminar.
/// </summary>
public static class LogProvisionamientoControlador
{
    public static DataTable Leer(int? idRouter = null)
    {
        var puede = idRouter == null
            ? PermisosControlador.VerAuditoria
            : PermisosControlador.Provisionar;
        var bloqueo = PermisosControlador.BloquearSi(puede, "Su rol no puede ver este log.");
        if (bloqueo != null) throw new InvalidOperationException(bloqueo.Mensaje);

        return ConexionMySql.Consultar(
            @"SELECT l.id_log, r.serie, u.nombre AS usuario, l.accion, l.detalle, l.fecha_accion
              FROM log_provisionamiento l
              JOIN routers r ON r.id_router = l.id_router
              JOIN usuarios u ON u.id_usuario = l.id_usuario
              WHERE (@router = 0 OR l.id_router = @router)
              ORDER BY l.id_log DESC",
            ConexionMySql.P("@router", idRouter ?? 0));
    }

    public static LogProvisionamiento? LeerPorId(int id)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.VerAuditoria || PermisosControlador.Provisionar, "Su rol no puede ver este log.");
        if (bloqueo != null) throw new InvalidOperationException(bloqueo.Mensaje);
        return ConexionMySql.LeerPrimero(
            "SELECT * FROM log_provisionamiento WHERE id_log = @id",
            Mapear,
            ConexionMySql.P("@id", id));
    }

    public static Respuesta Crear(LogProvisionamiento log)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.Provisionar, "Su rol no registra aprovisionamiento.");
        if (bloqueo != null) return bloqueo;
        if (log.IdRouter <= 0) return Respuesta.Fallo("Seleccione un router.");
        var error = Validador.Obligatorio(log.Accion, "Acción", 100);
        if (error != null) return Respuesta.Fallo(error);

        try
        {
            ConexionMySql.Insertar(
                @"INSERT INTO log_provisionamiento (id_router, id_usuario, accion, detalle)
                  VALUES (@router, @usuario, @accion, @detalle)",
                ConexionMySql.P("@router", log.IdRouter),
                ConexionMySql.P("@usuario", SesionActual.IdUsuario),
                ConexionMySql.P("@accion", log.Accion.Trim()),
                ConexionMySql.P("@detalle", Validador.Limpio(log.Detalle)));
            return Respuesta.Ok("Registro de aprovisionamiento creado.");
        }
        catch (Exception ex)
        {
            return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex));
        }
    }

    public static Respuesta Actualizar(LogProvisionamiento log)
    {
        return Respuesta.Fallo("Los registros de aprovisionamiento no se modifican.");
    }

    public static Respuesta Eliminar(int id)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.EsRolMaestro, "Solo el maestro puede borrar logs.");
        if (bloqueo != null) return bloqueo;
        try
        {
            var filas = ConexionMySql.Ejecutar("DELETE FROM log_provisionamiento WHERE id_log = @id", ConexionMySql.P("@id", id));
            if (filas == 0) return Respuesta.Fallo("No se encontró el registro.");
            return Respuesta.Ok("Registro eliminado.");
        }
        catch (Exception ex)
        {
            return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex));
        }
    }

    private static LogProvisionamiento Mapear(MySqlDataReader lector)
    {
        return new LogProvisionamiento
        {
            IdLog = LectorFila.Entero(lector, "id_log"),
            IdRouter = LectorFila.Entero(lector, "id_router"),
            IdUsuario = LectorFila.Entero(lector, "id_usuario"),
            Accion = LectorFila.Texto(lector, "accion"),
            Detalle = LectorFila.TextoNulo(lector, "detalle"),
            FechaAccion = LectorFila.Fecha(lector, "fecha_accion")
        };
    }
}
