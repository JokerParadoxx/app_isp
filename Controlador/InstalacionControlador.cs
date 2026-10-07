using System.Data;
using AppIsp.Modelo;
using MySqlConnector;

namespace AppIsp.Controlador;

/// <summary>
/// Visitas de instalación. Comercial las agenda. El técnico solo actualiza el estado,
/// la fecha real y el comentario: el controlador conserva el resto de la fila.
/// </summary>
public static class InstalacionControlador
{
    public static DataTable Leer(string texto)
    {
        PermisosControlador.ExigirSesion();
        var busqueda = (texto ?? "").Trim();
        return ConexionMySql.Consultar(
            @"SELECT i.id_instalacion, c.nombre AS cliente, p.nombre AS plan,
                     IFNULL(t.nombre, 'Sin técnico') AS tecnico,
                     i.fecha_programada, i.fecha_real, i.estado, i.comentario
              FROM instalaciones i
              JOIN servicios s ON s.id_servicio = i.id_servicio
              JOIN clientes c ON c.id_cliente = s.id_cliente
              JOIN planes p ON p.id_plan = s.id_plan
              LEFT JOIN usuarios t ON t.id_usuario = i.id_tecnico
              WHERE (@q = '' OR c.nombre LIKE @like OR IFNULL(i.comentario, '') LIKE @like)
              ORDER BY i.fecha_programada DESC",
            ConexionMySql.P("@q", busqueda),
            ConexionMySql.P("@like", "%" + busqueda + "%"));
    }

    public static Instalacion? LeerPorId(int id)
    {
        PermisosControlador.ExigirSesion();
        return ConexionMySql.LeerPrimero(
            "SELECT * FROM instalaciones WHERE id_instalacion = @id",
            Mapear,
            ConexionMySql.P("@id", id));
    }

    public static Respuesta Crear(Instalacion instalacion)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.CrearInstalacion, "Su rol no puede agendar instalaciones.");
        if (bloqueo != null) return bloqueo;
        var error = Validar(instalacion) ?? FechaProgramadaValida(instalacion.FechaProgramada, null);
        if (error != null) return Respuesta.Fallo(error);

        try
        {
            ConexionMySql.Insertar(
                @"INSERT INTO instalaciones (id_servicio, id_tecnico, fecha_programada, fecha_real, estado, comentario)
                  VALUES (@servicio, @tecnico, @programada, @real, @estado, @comentario)",
                Parametros(instalacion));
            return Respuesta.Ok("Instalación agendada.");
        }
        catch (Exception ex)
        {
            return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex));
        }
    }

    public static Respuesta Actualizar(Instalacion instalacion)
    {
        var puede = PermisosControlador.CrearInstalacion || PermisosControlador.SoloEstadoInstalacion;
        var bloqueo = PermisosControlador.BloquearSi(puede, "Su rol no puede modificar instalaciones.");
        if (bloqueo != null) return bloqueo;

        try
        {
            if (PermisosControlador.SoloEstadoInstalacion)
            {
                var actual = LeerPorId(instalacion.IdInstalacion);
                if (actual == null) return Respuesta.Fallo("No se encontró la instalación.");
                instalacion.IdServicio = actual.IdServicio;
                instalacion.IdTecnico = actual.IdTecnico;
                instalacion.FechaProgramada = actual.FechaProgramada;
            }

            CompletarFechaReal(instalacion);
            var guardada = PermisosControlador.SoloEstadoInstalacion ? null : LeerPorId(instalacion.IdInstalacion);
            var error = Validar(instalacion) ?? FechaProgramadaValida(instalacion.FechaProgramada, guardada?.FechaProgramada);
            if (error != null) return Respuesta.Fallo(error);

            var filas = ConexionMySql.Ejecutar(
                @"UPDATE instalaciones
                  SET id_servicio = @servicio, id_tecnico = @tecnico, fecha_programada = @programada,
                      fecha_real = @real, estado = @estado, comentario = @comentario
                  WHERE id_instalacion = @id",
                Parametros(instalacion));
            if (filas == 0) return Respuesta.Fallo("No se encontró la instalación.");
            return Respuesta.Ok("Instalación actualizada.");
        }
        catch (Exception ex)
        {
            return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex));
        }
    }

    public static Respuesta Eliminar(int id)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.EliminarInstalacion, "Solo el maestro puede eliminar una instalación.");
        if (bloqueo != null) return bloqueo;
        try
        {
            var filas = ConexionMySql.Ejecutar("DELETE FROM instalaciones WHERE id_instalacion = @id", ConexionMySql.P("@id", id));
            if (filas == 0) return Respuesta.Fallo("No se encontró la instalación.");
            return Respuesta.Ok("Instalación eliminada.");
        }
        catch (Exception ex)
        {
            return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex));
        }
    }

    private static void CompletarFechaReal(Instalacion instalacion)
    {
        if (instalacion.Estado == "COMPLETADA" && instalacion.FechaReal == null)
            instalacion.FechaReal = DateTime.Now;
    }

    private static string? FechaProgramadaValida(DateTime fecha, DateTime? fechaGuardada)
    {
        if (fecha.Date >= DateTime.Today) return null;
        if (fechaGuardada.HasValue && fecha.Date == fechaGuardada.Value.Date) return null;
        return "No se puede agendar en un día anterior a hoy. Elija el día en el calendario.";
    }

    private static string? Validar(Instalacion instalacion)
    {
        if (instalacion.IdServicio <= 0) return "Seleccione el servicio que se va a instalar.";
        return Validador.EnLista(instalacion.Estado, Catalogos.EstadosInstalacion, "Estado")
            ?? Validador.Opcional(instalacion.Comentario, "Comentario", 255);
    }

    private static MySqlParameter[] Parametros(Instalacion instalacion)
    {
        return new[]
        {
            ConexionMySql.P("@id", instalacion.IdInstalacion),
            ConexionMySql.P("@servicio", instalacion.IdServicio),
            ConexionMySql.P("@tecnico", instalacion.IdTecnico is > 0 ? instalacion.IdTecnico : null),
            ConexionMySql.P("@programada", instalacion.FechaProgramada),
            ConexionMySql.P("@real", instalacion.FechaReal),
            ConexionMySql.P("@estado", instalacion.Estado),
            ConexionMySql.P("@comentario", Validador.Limpio(instalacion.Comentario))
        };
    }

    private static Instalacion Mapear(MySqlDataReader lector)
    {
        return new Instalacion
        {
            IdInstalacion = LectorFila.Entero(lector, "id_instalacion"),
            IdServicio = LectorFila.Entero(lector, "id_servicio"),
            IdTecnico = LectorFila.EnteroNulo(lector, "id_tecnico"),
            FechaProgramada = LectorFila.Fecha(lector, "fecha_programada"),
            FechaReal = LectorFila.FechaNula(lector, "fecha_real"),
            Estado = LectorFila.Texto(lector, "estado"),
            Comentario = LectorFila.TextoNulo(lector, "comentario")
        };
    }
}
