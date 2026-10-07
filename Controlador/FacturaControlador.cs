using System.Data;
using System.Text.RegularExpressions;
using AppIsp.Modelo;
using MySqlConnector;

namespace AppIsp.Controlador;

/// <summary>
/// Facturas y su detalle. Solo el maestro. El total no se escribe a mano:
/// se recalcula sumando las líneas.
/// </summary>
public static class FacturaControlador
{
    private static readonly Regex PeriodoValido = new(@"^\d{4}-(0[1-9]|1[0-2])$", RegexOptions.Compiled);

    public static DataTable Leer(string texto)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.Facturar, "Solo el maestro ve la facturación.");
        if (bloqueo != null) throw new InvalidOperationException(bloqueo.Mensaje);

        var busqueda = (texto ?? "").Trim();
        return ConexionMySql.Consultar(
            @"SELECT f.id_factura, c.nombre AS cliente, f.periodo, f.fecha_emision, f.fecha_venc,
                     f.monto_total, f.estado
              FROM facturas f
              JOIN clientes c ON c.id_cliente = f.id_cliente
              WHERE (@q = '' OR c.nombre LIKE @like OR f.periodo LIKE @like)
              ORDER BY f.id_factura DESC",
            ConexionMySql.P("@q", busqueda),
            ConexionMySql.P("@like", "%" + busqueda + "%"));
    }

    public static Factura? LeerPorId(int id)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.Facturar, "Solo el maestro ve la facturación.");
        if (bloqueo != null) throw new InvalidOperationException(bloqueo.Mensaje);
        return ConexionMySql.LeerPrimero(
            "SELECT * FROM facturas WHERE id_factura = @id",
            Mapear,
            ConexionMySql.P("@id", id));
    }

    public static DataTable LeerDetalle(int idFactura)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.Facturar, "Solo el maestro ve la facturación.");
        if (bloqueo != null) throw new InvalidOperationException(bloqueo.Mensaje);
        return ConexionMySql.Consultar(
            @"SELECT id_detalle, descripcion, cantidad, precio_unitario, subtotal
              FROM factura_detalle WHERE id_factura = @id ORDER BY id_detalle",
            ConexionMySql.P("@id", idFactura));
    }

    public static Respuesta Crear(Factura factura)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.Facturar, "Solo el maestro puede facturar.");
        if (bloqueo != null) return bloqueo;
        var error = ValidarCabecera(factura);
        if (error != null) return Respuesta.Fallo(error);

        try
        {
            var id = ConexionMySql.Insertar(
                @"INSERT INTO facturas (id_cliente, periodo, fecha_emision, fecha_venc, monto_total, estado)
                  VALUES (@cliente, @periodo, @emision, @venc, 0, 'PENDIENTE')",
                ConexionMySql.P("@cliente", factura.IdCliente),
                ConexionMySql.P("@periodo", factura.Periodo.Trim()),
                ConexionMySql.P("@emision", factura.FechaEmision.Date),
                ConexionMySql.P("@venc", factura.FechaVencimiento.Date));
            return Respuesta.Ok("Factura " + id + " creada. Agregue el detalle.");
        }
        catch (Exception ex)
        {
            return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex));
        }
    }

    public static Respuesta Actualizar(Factura factura)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.Facturar, "Solo el maestro puede modificar facturas.");
        if (bloqueo != null) return bloqueo;
        var error = Validador.EnLista(factura.Estado, Catalogos.EstadosFactura, "Estado");
        if (error != null) return Respuesta.Fallo(error);
        if (factura.FechaVencimiento.Date < factura.FechaEmision.Date)
            return Respuesta.Fallo("El vencimiento no puede ser anterior a la emisión.");

        try
        {
            var filas = ConexionMySql.Ejecutar(
                @"UPDATE facturas SET fecha_emision = @emision, fecha_venc = @venc, estado = @estado
                  WHERE id_factura = @id",
                ConexionMySql.P("@emision", factura.FechaEmision.Date),
                ConexionMySql.P("@venc", factura.FechaVencimiento.Date),
                ConexionMySql.P("@estado", factura.Estado),
                ConexionMySql.P("@id", factura.IdFactura));
            if (filas == 0) return Respuesta.Fallo("No se encontró la factura.");
            return Respuesta.Ok("Factura actualizada.");
        }
        catch (Exception ex)
        {
            return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex));
        }
    }

    public static Respuesta Eliminar(int id)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.Facturar, "Solo el maestro puede eliminar facturas.");
        if (bloqueo != null) return bloqueo;
        try
        {
            using var conexion = ConexionMySql.Abrir();
            using var transaccion = conexion.BeginTransaction();
            ConexionMySql.Ejecutar(conexion, transaccion,
                "DELETE FROM factura_detalle WHERE id_factura = @id", ConexionMySql.P("@id", id));
            var filas = ConexionMySql.Ejecutar(conexion, transaccion,
                "DELETE FROM facturas WHERE id_factura = @id", ConexionMySql.P("@id", id));
            transaccion.Commit();
            if (filas == 0) return Respuesta.Fallo("No se encontró la factura.");
            return Respuesta.Ok("Factura eliminada.");
        }
        catch (Exception ex)
        {
            return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex));
        }
    }

    public static Respuesta AgregarDetalle(FacturaDetalle linea)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.Facturar, "Solo el maestro edita el detalle.");
        if (bloqueo != null) return bloqueo;
        if (linea.IdFactura <= 0) return Respuesta.Fallo("Seleccione una factura.");
        var error = Validador.Obligatorio(linea.Descripcion, "Descripción", 255);
        if (error != null) return Respuesta.Fallo(error);
        if (linea.Cantidad <= 0) return Respuesta.Fallo("La cantidad debe ser mayor que cero.");
        if (linea.PrecioUnitario < 0) return Respuesta.Fallo("El precio no puede ser negativo.");

        var factura = LeerPorId(linea.IdFactura);
        if (factura == null) return Respuesta.Fallo("No se encontró la factura.");
        if (factura.Estado != "PENDIENTE")
            return Respuesta.Fallo("Solo se agregan líneas a una factura pendiente.");

        try
        {
            linea.Subtotal = linea.Cantidad * linea.PrecioUnitario;
            using var conexion = ConexionMySql.Abrir();
            using var transaccion = conexion.BeginTransaction();
            ConexionMySql.Insertar(conexion, transaccion,
                @"INSERT INTO factura_detalle (id_factura, id_servicio, descripcion, cantidad, precio_unitario, subtotal)
                  VALUES (@factura, @servicio, @descripcion, @cantidad, @precio, @subtotal)",
                ConexionMySql.P("@factura", linea.IdFactura),
                ConexionMySql.P("@servicio", linea.IdServicio is > 0 ? linea.IdServicio : null),
                ConexionMySql.P("@descripcion", linea.Descripcion.Trim()),
                ConexionMySql.P("@cantidad", linea.Cantidad),
                ConexionMySql.P("@precio", linea.PrecioUnitario),
                ConexionMySql.P("@subtotal", linea.Subtotal));
            Recalcular(conexion, transaccion, linea.IdFactura);
            transaccion.Commit();
            return Respuesta.Ok("Línea agregada.");
        }
        catch (Exception ex)
        {
            return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex));
        }
    }

    public static Respuesta QuitarDetalle(int idDetalle)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.Facturar, "Solo el maestro edita el detalle.");
        if (bloqueo != null) return bloqueo;
        try
        {
            var idFactura = ConexionMySql.Escalar(
                "SELECT id_factura FROM factura_detalle WHERE id_detalle = @id",
                ConexionMySql.P("@id", idDetalle));
            if (idFactura == null) return Respuesta.Fallo("No se encontró la línea.");

            var factura = LeerPorId(Convert.ToInt32(idFactura));
            if (factura != null && factura.Estado != "PENDIENTE")
                return Respuesta.Fallo("Solo se quitan líneas de una factura pendiente.");

            using var conexion = ConexionMySql.Abrir();
            using var transaccion = conexion.BeginTransaction();
            ConexionMySql.Ejecutar(conexion, transaccion,
                "DELETE FROM factura_detalle WHERE id_detalle = @id", ConexionMySql.P("@id", idDetalle));
            Recalcular(conexion, transaccion, Convert.ToInt32(idFactura));
            transaccion.Commit();
            return Respuesta.Ok("Línea eliminada.");
        }
        catch (Exception ex)
        {
            return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex));
        }
    }

    public static FacturaDetalle? SugerirDesdeServicio(int idServicio)
    {
        PermisosControlador.ExigirSesion();
        return ConexionMySql.LeerPrimero(
            @"SELECT p.nombre, p.velocidad_mbps, p.precio_mensual
              FROM servicios s
              JOIN planes p ON p.id_plan = s.id_plan
              WHERE s.id_servicio = @id",
            lector => new FacturaDetalle
            {
                IdServicio = idServicio,
                Descripcion = "Plan " + LectorFila.Texto(lector, "nombre") + " " + LectorFila.Entero(lector, "velocidad_mbps") + " Mbps",
                Cantidad = 1,
                PrecioUnitario = LectorFila.Dinero(lector, "precio_mensual")
            },
            ConexionMySql.P("@id", idServicio));
    }

    private static void Recalcular(MySqlConnection conexion, MySqlTransaction transaccion, int idFactura)
    {
        ConexionMySql.Ejecutar(conexion, transaccion,
            @"UPDATE facturas
              SET monto_total = (
                  SELECT IFNULL(SUM(subtotal), 0) FROM factura_detalle WHERE id_factura = @id)
              WHERE id_factura = @id",
            ConexionMySql.P("@id", idFactura));
    }

    private static string? ValidarCabecera(Factura factura)
    {
        if (factura.IdCliente <= 0) return "Seleccione un cliente.";
        if (!PeriodoValido.IsMatch(factura.Periodo.Trim()))
            return "El período debe tener formato AAAA-MM, por ejemplo 2026-09.";
        if (factura.FechaVencimiento.Date < factura.FechaEmision.Date)
            return "El vencimiento no puede ser anterior a la emisión.";
        return null;
    }

    private static Factura Mapear(MySqlDataReader lector)
    {
        return new Factura
        {
            IdFactura = LectorFila.Entero(lector, "id_factura"),
            IdCliente = LectorFila.Entero(lector, "id_cliente"),
            Periodo = LectorFila.Texto(lector, "periodo"),
            FechaEmision = LectorFila.Fecha(lector, "fecha_emision"),
            FechaVencimiento = LectorFila.Fecha(lector, "fecha_venc"),
            MontoTotal = LectorFila.Dinero(lector, "monto_total"),
            Estado = LectorFila.Texto(lector, "estado")
        };
    }
}
