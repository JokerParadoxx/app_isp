using System.Data;
using AppIsp.Modelo;
using MySqlConnector;

namespace AppIsp.Controlador;

/// <summary>
/// Tickets de soporte. El comercial solo ve y edita los de tipo COMERCIAL.
/// El técnico, los de tipo TECNICO. El maestro ve ambos.
/// </summary>
public static class TicketControlador
{
    public static DataTable Leer(string texto, string estado, int? limite = null)
    {
        PermisosControlador.ExigirSesion();
        var busqueda = (texto ?? "").Trim();
        var filtroEstado = estado == "TODOS" ? "" : (estado ?? "");
        var tipo = TipoForzado();

        var sql = @"SELECT t.id_ticket, c.nombre AS cliente, t.tipo, t.categoria, t.prioridad, t.estado,
                           IFNULL(u.nombre, 'Sin asignar') AS asignado, t.fecha_creacion, t.fecha_cierre,
                           LEFT(t.descripcion, 80) AS resumen
                    FROM tickets t
                    JOIN clientes c ON c.id_cliente = t.id_cliente
                    LEFT JOIN usuarios u ON u.id_usuario = t.id_asignado
                    WHERE (@tipo = '' OR t.tipo = @tipo)
                      AND (@estado = '' OR t.estado = @estado)
                      AND (@q = '' OR c.nombre LIKE @like OR t.categoria LIKE @like OR t.descripcion LIKE @like)
                    ORDER BY t.fecha_creacion DESC";

        if (limite is > 0 and <= 100)
            sql += " LIMIT " + limite.Value;

        return ConexionMySql.Consultar(sql,
            ConexionMySql.P("@tipo", tipo),
            ConexionMySql.P("@estado", filtroEstado),
            ConexionMySql.P("@q", busqueda),
            ConexionMySql.P("@like", "%" + busqueda + "%"));
    }

    public static Ticket? LeerPorId(int id)
    {
        PermisosControlador.ExigirSesion();
        var ticket = ConexionMySql.LeerPrimero(
            "SELECT * FROM tickets WHERE id_ticket = @id",
            Mapear,
            ConexionMySql.P("@id", id));
        if (ticket == null) return null;
        if (!PuedeVerTipo(ticket.Tipo)) return null;
        return ticket;
    }

    public static Respuesta Crear(Ticket ticket)
    {
        var bloqueo = PermisosControlador.BloquearSi(true, "");
        if (bloqueo != null) return bloqueo;
        ticket.Tipo = TipoAlGuardar(ticket.Tipo);
        var error = Validar(ticket);
        if (error != null) return Respuesta.Fallo(error);
        AjustarCierre(ticket);

        try
        {
            ConexionMySql.Insertar(
                @"INSERT INTO tickets (id_cliente, id_servicio, tipo, categoria, prioridad, estado, descripcion, fecha_cierre, id_asignado)
                  VALUES (@cliente, @servicio, @tipo, @categoria, @prioridad, @estado, @descripcion, @cierre, @asignado)",
                Parametros(ticket));
            return Respuesta.Ok("Ticket creado.");
        }
        catch (Exception ex)
        {
            return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex));
        }
    }

    public static Respuesta Actualizar(Ticket ticket)
    {
        var bloqueo = PermisosControlador.BloquearSi(true, "");
        if (bloqueo != null) return bloqueo;

        var actual = LeerPorId(ticket.IdTicket);
        if (actual == null) return Respuesta.Fallo("No se encontró el ticket o no corresponde a su rol.");
        ticket.Tipo = TipoAlGuardar(actual.Tipo);
        var error = Validar(ticket);
        if (error != null) return Respuesta.Fallo(error);
        AjustarCierre(ticket);

        try
        {
            var filas = ConexionMySql.Ejecutar(
                @"UPDATE tickets
                  SET id_cliente = @cliente, id_servicio = @servicio, tipo = @tipo, categoria = @categoria,
                      prioridad = @prioridad, estado = @estado, descripcion = @descripcion,
                      fecha_cierre = @cierre, id_asignado = @asignado
                  WHERE id_ticket = @id",
                Parametros(ticket));
            if (filas == 0) return Respuesta.Fallo("No se encontró el ticket.");
            return Respuesta.Ok("Ticket actualizado.");
        }
        catch (Exception ex)
        {
            return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex));
        }
    }

    public static Respuesta Eliminar(int id)
    {
        var actual = LeerPorId(id);
        if (actual == null) return Respuesta.Fallo("No se encontró el ticket o no corresponde a su rol.");
        try
        {
            ConexionMySql.Ejecutar("DELETE FROM tickets WHERE id_ticket = @id", ConexionMySql.P("@id", id));
            return Respuesta.Ok("Ticket eliminado.");
        }
        catch (Exception ex)
        {
            return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex));
        }
    }

    private static string TipoForzado()
    {
        if (PermisosControlador.EsRolComercial) return "COMERCIAL";
        if (PermisosControlador.EsRolTecnico) return "TECNICO";
        return "";
    }

    private static string TipoAlGuardar(string solicitado)
    {
        if (PermisosControlador.EsRolComercial) return "COMERCIAL";
        if (PermisosControlador.EsRolTecnico) return "TECNICO";
        return solicitado == "TECNICO" ? "TECNICO" : "COMERCIAL";
    }

    private static bool PuedeVerTipo(string tipo)
    {
        if (PermisosControlador.EsRolMaestro) return true;
        if (PermisosControlador.EsRolComercial) return tipo == "COMERCIAL";
        if (PermisosControlador.EsRolTecnico) return tipo == "TECNICO";
        return false;
    }

    private static void AjustarCierre(Ticket ticket)
    {
        if (ticket.Estado is "RESUELTO" or "CERRADO" or "CANCELADO")
            ticket.FechaCierre ??= DateTime.Now;
        else
            ticket.FechaCierre = null;
    }

    private static string? Validar(Ticket ticket)
    {
        if (ticket.IdCliente <= 0) return "Seleccione un cliente.";
        return Validador.Obligatorio(ticket.Categoria, "Categoría", 100)
            ?? Validador.Obligatorio(ticket.Descripcion, "Descripción", 4000)
            ?? Validador.EnLista(ticket.Tipo, Catalogos.TiposTicket, "Tipo")
            ?? Validador.EnLista(ticket.Prioridad, Catalogos.Prioridades, "Prioridad")
            ?? Validador.EnLista(ticket.Estado, Catalogos.EstadosTicket, "Estado");
    }

    private static MySqlParameter[] Parametros(Ticket ticket)
    {
        return new[]
        {
            ConexionMySql.P("@id", ticket.IdTicket),
            ConexionMySql.P("@cliente", ticket.IdCliente),
            ConexionMySql.P("@servicio", ticket.IdServicio is > 0 ? ticket.IdServicio : null),
            ConexionMySql.P("@tipo", ticket.Tipo),
            ConexionMySql.P("@categoria", ticket.Categoria.Trim()),
            ConexionMySql.P("@prioridad", ticket.Prioridad),
            ConexionMySql.P("@estado", ticket.Estado),
            ConexionMySql.P("@descripcion", ticket.Descripcion.Trim()),
            ConexionMySql.P("@cierre", ticket.FechaCierre),
            ConexionMySql.P("@asignado", ticket.IdAsignado is > 0 ? ticket.IdAsignado : null)
        };
    }

    private static Ticket Mapear(MySqlDataReader lector)
    {
        return new Ticket
        {
            IdTicket = LectorFila.Entero(lector, "id_ticket"),
            IdCliente = LectorFila.Entero(lector, "id_cliente"),
            IdServicio = LectorFila.EnteroNulo(lector, "id_servicio"),
            Tipo = LectorFila.Texto(lector, "tipo"),
            Categoria = LectorFila.Texto(lector, "categoria"),
            Prioridad = LectorFila.Texto(lector, "prioridad"),
            Estado = LectorFila.Texto(lector, "estado"),
            Descripcion = LectorFila.Texto(lector, "descripcion"),
            FechaCreacion = LectorFila.Fecha(lector, "fecha_creacion"),
            FechaCierre = LectorFila.FechaNula(lector, "fecha_cierre"),
            IdAsignado = LectorFila.EnteroNulo(lector, "id_asignado")
        };
    }
}
