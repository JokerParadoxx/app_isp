using System.Data;
using AppIsp.Modelo;
using MySqlConnector;

namespace AppIsp.Controlador;

/// <summary>Bloques de agenda. Comercial y técnico consultan. Solo el maestro los carga.</summary>
public static class AgendaControlador
{
    public static DataTable Leer(string texto)
    {
        PermisosControlador.ExigirSesion();
        AsegurarBloques();
        var busqueda = (texto ?? "").Trim();
        return ConexionMySql.Consultar(
            @"SELECT d.id_disponibilidad, u.nombre AS usuario, d.fecha, d.bloque_horario,
                     IF(d.disponible = 1, 'Sí', 'No') AS disponible
              FROM disponibilidad_agenda d
              JOIN usuarios u ON u.id_usuario = d.id_usuario
              WHERE (@q = '' OR u.nombre LIKE @like)
              ORDER BY d.fecha, d.bloque_horario",
            ConexionMySql.P("@q", busqueda),
            ConexionMySql.P("@like", "%" + busqueda + "%"));
    }

    public static DisponibilidadAgenda? LeerPorId(int id)
    {
        PermisosControlador.ExigirSesion();
        return ConexionMySql.LeerPrimero(
            "SELECT * FROM disponibilidad_agenda WHERE id_disponibilidad = @id",
            Mapear,
            ConexionMySql.P("@id", id));
    }

    public static Respuesta Crear(DisponibilidadAgenda item)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.AgendarDisponibilidad, "Solo el maestro carga la disponibilidad.");
        if (bloqueo != null) return bloqueo;
        AsegurarBloques();
        var error = Validar(item) ?? FechaValida(item.Fecha, null);
        if (error != null) return Respuesta.Fallo(error);
        if (Existe(item, 0))
            return Respuesta.Fallo("Ese usuario ya tiene ese bloque en la fecha indicada.");

        try
        {
            ConexionMySql.Insertar(
                @"INSERT INTO disponibilidad_agenda (id_usuario, fecha, bloque_horario, disponible)
                  VALUES (@usuario, @fecha, @bloque, @disponible)",
                Parametros(item));
            return Respuesta.Ok("Bloque de agenda creado.");
        }
        catch (Exception ex)
        {
            return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex));
        }
    }

    public static Respuesta Actualizar(DisponibilidadAgenda item)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.AgendarDisponibilidad, "Solo el maestro edita la agenda.");
        if (bloqueo != null) return bloqueo;
        AsegurarBloques();
        var guardado = LeerPorId(item.IdDisponibilidad);
        var error = Validar(item) ?? FechaValida(item.Fecha, guardado?.Fecha);
        if (error != null) return Respuesta.Fallo(error);
        if (Existe(item, item.IdDisponibilidad))
            return Respuesta.Fallo("Ese usuario ya tiene ese bloque en la fecha indicada.");

        try
        {
            var filas = ConexionMySql.Ejecutar(
                @"UPDATE disponibilidad_agenda
                  SET id_usuario = @usuario, fecha = @fecha, bloque_horario = @bloque, disponible = @disponible
                  WHERE id_disponibilidad = @id",
                Parametros(item));
            if (filas == 0) return Respuesta.Fallo("No se encontró el bloque.");
            return Respuesta.Ok("Bloque actualizado.");
        }
        catch (Exception ex)
        {
            return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex));
        }
    }

    public static Respuesta Eliminar(int id)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.AgendarDisponibilidad, "Solo el maestro elimina bloques de agenda.");
        if (bloqueo != null) return bloqueo;
        try
        {
            var filas = ConexionMySql.Ejecutar(
                "DELETE FROM disponibilidad_agenda WHERE id_disponibilidad = @id",
                ConexionMySql.P("@id", id));
            if (filas == 0) return Respuesta.Fallo("No se encontró el bloque.");
            return Respuesta.Ok("Bloque eliminado.");
        }
        catch (Exception ex)
        {
            return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex));
        }
    }

    private static bool Existe(DisponibilidadAgenda item, int idIgnorado)
    {
        var cantidad = Convert.ToInt32(ConexionMySql.Escalar(
            @"SELECT COUNT(*) FROM disponibilidad_agenda
              WHERE id_usuario = @usuario AND fecha = @fecha AND bloque_horario = @bloque
                AND id_disponibilidad <> @id",
            ConexionMySql.P("@usuario", item.IdUsuario),
            ConexionMySql.P("@fecha", item.Fecha.Date),
            ConexionMySql.P("@bloque", item.BloqueHorario),
            ConexionMySql.P("@id", idIgnorado)));
        return cantidad > 0;
    }

    private static bool _bloquesListos;

    /// <summary>
    /// La columna nació como mañana/tarde/noche. Pasa a texto para guardar
    /// cada hora de 09:00 a 20:00 sin rehacer la base a mano.
    /// </summary>
    private static void AsegurarBloques()
    {
        if (_bloquesListos) return;
        var tipo = Convert.ToString(ConexionMySql.Escalar(
            @"SELECT DATA_TYPE FROM information_schema.COLUMNS
              WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'disponibilidad_agenda'
                AND COLUMN_NAME = 'bloque_horario'"));
        if (!string.Equals(tipo, "varchar", StringComparison.OrdinalIgnoreCase))
        {
            ConexionMySql.Ejecutar(
                "ALTER TABLE disponibilidad_agenda MODIFY bloque_horario VARCHAR(20) NOT NULL");
        }
        _bloquesListos = true;
    }

    private static string? FechaValida(DateTime fecha, DateTime? fechaGuardada)
    {
        if (fecha.Date >= DateTime.Today) return null;
        if (fechaGuardada.HasValue && fecha.Date == fechaGuardada.Value.Date) return null;
        return "No se puede elegir un día anterior a hoy. Márquelo en el calendario.";
    }

    private static string? Validar(DisponibilidadAgenda item)
    {
        if (item.IdUsuario <= 0) return "Seleccione un usuario.";
        return Validador.EnLista(item.BloqueHorario, Catalogos.BloquesAgenda, "Bloque");
    }

    private static MySqlParameter[] Parametros(DisponibilidadAgenda item)
    {
        return new[]
        {
            ConexionMySql.P("@id", item.IdDisponibilidad),
            ConexionMySql.P("@usuario", item.IdUsuario),
            ConexionMySql.P("@fecha", item.Fecha.Date),
            ConexionMySql.P("@bloque", item.BloqueHorario),
            ConexionMySql.P("@disponible", item.Disponible ? 1 : 0)
        };
    }

    private static DisponibilidadAgenda Mapear(MySqlDataReader lector)
    {
        return new DisponibilidadAgenda
        {
            IdDisponibilidad = LectorFila.Entero(lector, "id_disponibilidad"),
            IdUsuario = LectorFila.Entero(lector, "id_usuario"),
            Fecha = LectorFila.Fecha(lector, "fecha"),
            BloqueHorario = LectorFila.Texto(lector, "bloque_horario"),
            Disponible = LectorFila.Booleano(lector, "disponible")
        };
    }
}
