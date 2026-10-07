using System.Data;
using AppIsp.Modelo;
using MySqlConnector;

namespace AppIsp.Controlador;

/// <summary>CRUD de planes. Solo el maestro los administra. El resto solo los consulta para vender un servicio.</summary>
public static class PlanControlador
{
    public static DataTable Leer(string texto)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.AdministrarPlanes, "Solo el maestro administra los planes.");
        if (bloqueo != null) throw new InvalidOperationException(bloqueo.Mensaje);

        var busqueda = (texto ?? "").Trim();
        return ConexionMySql.Consultar(
            @"SELECT id_plan, nombre, descripcion, velocidad_mbps, precio_mensual,
                     IF(activo = 1, 'Sí', 'No') AS activo
              FROM planes
              WHERE (@q = '' OR nombre LIKE @like OR IFNULL(descripcion, '') LIKE @like)
              ORDER BY velocidad_mbps",
            ConexionMySql.P("@q", busqueda),
            ConexionMySql.P("@like", "%" + busqueda + "%"));
    }

    public static Plan? LeerPorId(int id)
    {
        PermisosControlador.ExigirSesion();
        return ConexionMySql.LeerPrimero(
            "SELECT * FROM planes WHERE id_plan = @id",
            Mapear,
            ConexionMySql.P("@id", id));
    }

    public static List<OpcionCombo> ListarOpciones(bool soloActivos)
    {
        PermisosControlador.ExigirSesion();
        var tabla = ConexionMySql.Consultar(
            @"SELECT id_plan, nombre, velocidad_mbps, precio_mensual
              FROM planes
              WHERE (@solo = 0 OR activo = 1)
              ORDER BY velocidad_mbps",
            ConexionMySql.P("@solo", soloActivos ? 1 : 0));

        var lista = new List<OpcionCombo>();
        foreach (DataRow fila in tabla.Rows)
        {
            var precio = Convert.ToDecimal(fila["precio_mensual"]);
            lista.Add(new OpcionCombo
            {
                Id = Convert.ToInt32(fila["id_plan"]),
                Texto = fila["nombre"] + " · " + fila["velocidad_mbps"] + " Mbps · " + precio.ToString("C0")
            });
        }
        return lista;
    }

    public static Respuesta Crear(Plan plan)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.AdministrarPlanes, "Solo el maestro puede crear planes.");
        if (bloqueo != null) return bloqueo;
        var error = Validar(plan);
        if (error != null) return Respuesta.Fallo(error);

        try
        {
            ConexionMySql.Insertar(
                @"INSERT INTO planes (nombre, descripcion, velocidad_mbps, precio_mensual, activo)
                  VALUES (@nombre, @descripcion, @velocidad, @precio, @activo)",
                ConexionMySql.P("@nombre", plan.Nombre.Trim()),
                ConexionMySql.P("@descripcion", Validador.Limpio(plan.Descripcion)),
                ConexionMySql.P("@velocidad", plan.VelocidadMbps),
                ConexionMySql.P("@precio", plan.PrecioMensual),
                ConexionMySql.P("@activo", plan.Activo ? 1 : 0));
            return Respuesta.Ok("Plan creado.");
        }
        catch (Exception ex)
        {
            return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex));
        }
    }

    public static Respuesta Actualizar(Plan plan)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.AdministrarPlanes, "Solo el maestro puede editar planes.");
        if (bloqueo != null) return bloqueo;
        var error = Validar(plan);
        if (error != null) return Respuesta.Fallo(error);

        try
        {
            var filas = ConexionMySql.Ejecutar(
                @"UPDATE planes
                  SET nombre = @nombre, descripcion = @descripcion, velocidad_mbps = @velocidad,
                      precio_mensual = @precio, activo = @activo
                  WHERE id_plan = @id",
                ConexionMySql.P("@nombre", plan.Nombre.Trim()),
                ConexionMySql.P("@descripcion", Validador.Limpio(plan.Descripcion)),
                ConexionMySql.P("@velocidad", plan.VelocidadMbps),
                ConexionMySql.P("@precio", plan.PrecioMensual),
                ConexionMySql.P("@activo", plan.Activo ? 1 : 0),
                ConexionMySql.P("@id", plan.IdPlan));
            if (filas == 0) return Respuesta.Fallo("No se encontró el plan.");
            return Respuesta.Ok("Plan actualizado.");
        }
        catch (Exception ex)
        {
            return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex));
        }
    }

    public static Respuesta Eliminar(int id)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.AdministrarPlanes, "Solo el maestro puede eliminar planes.");
        if (bloqueo != null) return bloqueo;
        try
        {
            var filas = ConexionMySql.Ejecutar("DELETE FROM planes WHERE id_plan = @id", ConexionMySql.P("@id", id));
            if (filas == 0) return Respuesta.Fallo("No se encontró el plan.");
            return Respuesta.Ok("Plan eliminado.");
        }
        catch (Exception ex)
        {
            return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex));
        }
    }

    private static string? Validar(Plan plan)
    {
        var error = Validador.Obligatorio(plan.Nombre, "Nombre", 100)
            ?? Validador.Opcional(plan.Descripcion, "Descripción", 255);
        if (error != null) return error;
        if (plan.VelocidadMbps <= 0) return "La velocidad debe ser mayor que cero.";
        if (plan.PrecioMensual < 0) return "El precio no puede ser negativo.";
        return null;
    }

    private static Plan Mapear(MySqlDataReader lector)
    {
        return new Plan
        {
            IdPlan = LectorFila.Entero(lector, "id_plan"),
            Nombre = LectorFila.Texto(lector, "nombre"),
            Descripcion = LectorFila.TextoNulo(lector, "descripcion"),
            VelocidadMbps = LectorFila.Entero(lector, "velocidad_mbps"),
            PrecioMensual = LectorFila.Dinero(lector, "precio_mensual"),
            Activo = LectorFila.Booleano(lector, "activo")
        };
    }
}
