using System.Data;
using AppIsp.Modelo;
using MySqlConnector;

namespace AppIsp.Controlador;

/// <summary>
/// Altas, bajas y cambios de plan. Cada cambio comercial queda en log_comercial
/// dentro de la misma transacción: si el log falla, el servicio tampoco se guarda.
/// El técnico solo puede leer.
/// </summary>
public static class ServicioControlador
{
    public static DataTable Leer(string texto)
    {
        PermisosControlador.ExigirSesion();
        var busqueda = (texto ?? "").Trim();
        return ConexionMySql.Consultar(
            @"SELECT s.id_servicio, c.nombre AS cliente, p.nombre AS plan, p.velocidad_mbps,
                     s.estado, s.fecha_alta, s.fecha_baja, s.comentario
              FROM servicios s
              JOIN clientes c ON c.id_cliente = s.id_cliente
              JOIN planes p ON p.id_plan = s.id_plan
              WHERE (@q = '' OR c.nombre LIKE @like OR p.nombre LIKE @like OR IFNULL(s.comentario, '') LIKE @like)
              ORDER BY s.id_servicio DESC",
            ConexionMySql.P("@q", busqueda),
            ConexionMySql.P("@like", "%" + busqueda + "%"));
    }

    public static Servicio? LeerPorId(int id)
    {
        PermisosControlador.ExigirSesion();
        return ConexionMySql.LeerPrimero(
            @"SELECT s.*, c.nombre AS nombre_cliente, p.nombre AS nombre_plan
              FROM servicios s
              JOIN clientes c ON c.id_cliente = s.id_cliente
              JOIN planes p ON p.id_plan = s.id_plan
              WHERE s.id_servicio = @id",
            Mapear,
            ConexionMySql.P("@id", id));
    }

    public static List<OpcionCombo> ListarOpciones()
    {
        PermisosControlador.ExigirSesion();
        var tabla = ConexionMySql.Consultar(
            @"SELECT s.id_servicio, CONCAT('#', s.id_servicio, ' ', c.nombre, ' — ', p.nombre) AS texto
              FROM servicios s
              JOIN clientes c ON c.id_cliente = s.id_cliente
              JOIN planes p ON p.id_plan = s.id_plan
              ORDER BY s.id_servicio DESC");
        return AOpciones(tabla, incluirVacio: true);
    }

    public static List<OpcionCombo> ListarPorCliente(int idCliente)
    {
        PermisosControlador.ExigirSesion();
        var tabla = ConexionMySql.Consultar(
            @"SELECT s.id_servicio, CONCAT(p.nombre, ' — ', s.estado) AS texto
              FROM servicios s
              JOIN planes p ON p.id_plan = s.id_plan
              WHERE s.id_cliente = @id
              ORDER BY s.id_servicio DESC",
            ConexionMySql.P("@id", idCliente));
        return AOpciones(tabla, incluirVacio: true);
    }

    public static Respuesta Crear(Servicio servicio)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.AltaServicio, "Su rol no puede dar de alta servicios.");
        if (bloqueo != null) return bloqueo;
        if (servicio.IdCliente <= 0 || servicio.IdPlan <= 0)
            return Respuesta.Fallo("Seleccione cliente y plan.");

        var error = Validador.Opcional(servicio.Comentario, "Comentario", 255);
        if (error != null) return Respuesta.Fallo(error);

        try
        {
            var estadoCliente = Convert.ToString(ConexionMySql.Escalar(
                "SELECT estado FROM clientes WHERE id_cliente = @id",
                ConexionMySql.P("@id", servicio.IdCliente)));
            if (estadoCliente == null) return Respuesta.Fallo("El cliente no existe.");
            if (estadoCliente == "BAJA") return Respuesta.Fallo("El cliente está de baja. Cambie su estado antes de contratar.");

            if (!PlanActivo(servicio.IdPlan))
                return Respuesta.Fallo("Ese plan no existe o no está activo.");

            var nombrePlan = NombrePlan(servicio.IdPlan);
            using var conexion = ConexionMySql.Abrir();
            using var transaccion = conexion.BeginTransaction();

            var id = ConexionMySql.Insertar(conexion, transaccion,
                @"INSERT INTO servicios (id_cliente, id_plan, estado, comentario)
                  VALUES (@cliente, @plan, 'PENDIENTE_INSTALACION', @comentario)",
                ConexionMySql.P("@cliente", servicio.IdCliente),
                ConexionMySql.P("@plan", servicio.IdPlan),
                ConexionMySql.P("@comentario", Validador.Limpio(servicio.Comentario)));

            RegistrarLog(conexion, transaccion, id, "ALTA", "Alta de servicio. Plan: " + nombrePlan);
            transaccion.Commit();
            return Respuesta.Ok("Servicio creado. Quedó pendiente de instalación.");
        }
        catch (Exception ex)
        {
            return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex));
        }
    }

    public static Respuesta Actualizar(Servicio servicio)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.AltaServicio, "Su rol no puede cambiar el plan.");
        if (bloqueo != null) return bloqueo;

        var error = Validador.Opcional(servicio.Comentario, "Comentario", 255);
        if (error != null) return Respuesta.Fallo(error);
        if (servicio.IdPlan <= 0) return Respuesta.Fallo("Seleccione un plan.");

        try
        {
            var actual = LeerPorId(servicio.IdServicio);
            if (actual == null) return Respuesta.Fallo("No se encontró el servicio.");
            if (actual.Estado == "BAJA") return Respuesta.Fallo("El servicio está de baja.");
            if (!PlanActivo(servicio.IdPlan) && servicio.IdPlan != actual.IdPlan)
                return Respuesta.Fallo("Ese plan no está activo.");

            using var conexion = ConexionMySql.Abrir();
            using var transaccion = conexion.BeginTransaction();

            ConexionMySql.Ejecutar(conexion, transaccion,
                @"UPDATE servicios SET id_plan = @plan, comentario = @comentario WHERE id_servicio = @id",
                ConexionMySql.P("@plan", servicio.IdPlan),
                ConexionMySql.P("@comentario", Validador.Limpio(servicio.Comentario)),
                ConexionMySql.P("@id", servicio.IdServicio));

            if (actual.IdPlan != servicio.IdPlan)
            {
                RegistrarLog(conexion, transaccion, servicio.IdServicio, "CAMBIO_PLAN",
                    "Plan anterior: " + actual.NombrePlan + ". Plan nuevo: " + NombrePlan(servicio.IdPlan) + ".");
            }

            transaccion.Commit();
            return Respuesta.Ok(actual.IdPlan == servicio.IdPlan
                ? "Comentario actualizado."
                : "Cambio de plan registrado.");
        }
        catch (Exception ex)
        {
            return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex));
        }
    }

    public static Respuesta DarDeBaja(int id, string? comentario)
    {
        return CambiarEstado(id, "BAJA", "BAJA", comentario, soloDesde: null);
    }

    public static Respuesta Suspender(int id)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.EsRolMaestro, "Solo el maestro puede suspender desde esta acción.");
        if (bloqueo != null) return bloqueo;
        return CambiarEstado(id, "SUSPENDIDO", "SUSPENSION", null, soloDesde: "ACTIVO");
    }

    public static Respuesta Reactivar(int id)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.EsRolMaestro, "Solo el maestro puede reactivar.");
        if (bloqueo != null) return bloqueo;
        return CambiarEstado(id, "ACTIVO", "REACTIVACION", null, soloDesde: "SUSPENDIDO");
    }

    public static Respuesta Eliminar(int id)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.EsRolMaestro, "Solo el maestro puede borrar el servicio. El comercial debe darlo de baja.");
        if (bloqueo != null) return bloqueo;
        try
        {
            var filas = ConexionMySql.Ejecutar("DELETE FROM servicios WHERE id_servicio = @id", ConexionMySql.P("@id", id));
            if (filas == 0) return Respuesta.Fallo("No se encontró el servicio.");
            return Respuesta.Ok("Servicio eliminado.");
        }
        catch (Exception ex)
        {
            return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex));
        }
    }

    private static Respuesta CambiarEstado(int id, string estadoNuevo, string tipoLog, string? comentario, string? soloDesde)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.AltaServicio, "Su rol no puede modificar servicios.");
        if (bloqueo != null) return bloqueo;

        try
        {
            var actual = LeerPorId(id);
            if (actual == null) return Respuesta.Fallo("No se encontró el servicio.");
            if (actual.Estado == "BAJA") return Respuesta.Fallo("El servicio ya está de baja.");
            if (soloDesde != null && actual.Estado != soloDesde)
                return Respuesta.Fallo("Esta acción solo aplica si el servicio está en estado " + Catalogos.TextoAmigable(soloDesde) + ".");

            using var conexion = ConexionMySql.Abrir();
            using var transaccion = conexion.BeginTransaction();

            if (estadoNuevo == "BAJA")
            {
                ConexionMySql.Ejecutar(conexion, transaccion,
                    @"UPDATE servicios SET estado = 'BAJA', fecha_baja = NOW(), comentario = @comentario WHERE id_servicio = @id",
                    ConexionMySql.P("@comentario", Validador.Limpio(comentario) ?? actual.Comentario),
                    ConexionMySql.P("@id", id));
            }
            else
            {
                ConexionMySql.Ejecutar(conexion, transaccion,
                    "UPDATE servicios SET estado = @estado WHERE id_servicio = @id",
                    ConexionMySql.P("@estado", estadoNuevo),
                    ConexionMySql.P("@id", id));
            }

            RegistrarLog(conexion, transaccion, id, tipoLog, Validador.Limpio(comentario) ?? Catalogos.TextoAmigable(tipoLog));
            transaccion.Commit();
            return Respuesta.Ok("Cambio registrado: " + Catalogos.TextoAmigable(tipoLog) + ".");
        }
        catch (Exception ex)
        {
            return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex));
        }
    }

    private static void RegistrarLog(MySqlConnection conexion, MySqlTransaction transaccion, int idServicio, string tipo, string? detalle)
    {
        ConexionMySql.Ejecutar(conexion, transaccion,
            @"INSERT INTO log_comercial (id_servicio, id_usuario, tipo_cambio, detalle)
              VALUES (@servicio, @usuario, @tipo, @detalle)",
            ConexionMySql.P("@servicio", idServicio),
            ConexionMySql.P("@usuario", SesionActual.IdUsuario),
            ConexionMySql.P("@tipo", tipo),
            ConexionMySql.P("@detalle", detalle));
    }

    private static bool PlanActivo(int idPlan)
    {
        var activo = ConexionMySql.Escalar("SELECT activo FROM planes WHERE id_plan = @id", ConexionMySql.P("@id", idPlan));
        return activo != null && Convert.ToInt32(activo) == 1;
    }

    private static string NombrePlan(int idPlan)
    {
        return Convert.ToString(ConexionMySql.Escalar(
            "SELECT nombre FROM planes WHERE id_plan = @id", ConexionMySql.P("@id", idPlan))) ?? "";
    }

    private static List<OpcionCombo> AOpciones(DataTable tabla, bool incluirVacio)
    {
        var lista = new List<OpcionCombo>();
        if (incluirVacio)
            lista.Add(new OpcionCombo { Id = 0, Texto = "(Sin servicio)" });
        foreach (DataRow fila in tabla.Rows)
        {
            lista.Add(new OpcionCombo
            {
                Id = Convert.ToInt32(fila["id_servicio"]),
                Texto = Convert.ToString(fila["texto"]) ?? ""
            });
        }
        return lista;
    }

    private static Servicio Mapear(MySqlDataReader lector)
    {
        return new Servicio
        {
            IdServicio = LectorFila.Entero(lector, "id_servicio"),
            IdCliente = LectorFila.Entero(lector, "id_cliente"),
            IdPlan = LectorFila.Entero(lector, "id_plan"),
            Estado = LectorFila.Texto(lector, "estado"),
            FechaAlta = LectorFila.Fecha(lector, "fecha_alta"),
            FechaBaja = LectorFila.FechaNula(lector, "fecha_baja"),
            Comentario = LectorFila.TextoNulo(lector, "comentario"),
            NombreCliente = LectorFila.Texto(lector, "nombre_cliente"),
            NombrePlan = LectorFila.Texto(lector, "nombre_plan")
        };
    }
}
