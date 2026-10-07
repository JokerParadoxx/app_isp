using System.Data;
using System.Net;
using System.Net.Sockets;
using AppIsp.Modelo;
using MySqlConnector;

namespace AppIsp.Controlador;

/// <summary>Equipos CPE. El técnico lee y actualiza. Crear y eliminar queda para el maestro.</summary>
public static class RouterControlador
{
    public static DataTable Leer(string texto)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.OperarRouters, "Su rol no puede ver los routers.");
        if (bloqueo != null) throw new InvalidOperationException(bloqueo.Mensaje);

        var busqueda = (texto ?? "").Trim();
        return ConexionMySql.Consultar(
            @"SELECT r.id_router, r.serie, r.modelo, r.mac_address, r.ip_gestion, r.estado,
                     IFNULL(c.nombre, '') AS cliente, r.ubicacion
              FROM routers r
              LEFT JOIN servicios s ON s.id_servicio = r.id_servicio
              LEFT JOIN clientes c ON c.id_cliente = s.id_cliente
              WHERE (@q = '' OR r.serie LIKE @like OR r.mac_address LIKE @like OR IFNULL(r.ip_gestion, '') LIKE @like
                     OR IFNULL(c.nombre, '') LIKE @like)
              ORDER BY r.id_router DESC",
            ConexionMySql.P("@q", busqueda),
            ConexionMySql.P("@like", "%" + busqueda + "%"));
    }

    public static Router? LeerPorId(int id)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.OperarRouters, "Su rol no puede ver los routers.");
        if (bloqueo != null) throw new InvalidOperationException(bloqueo.Mensaje);
        return ConexionMySql.LeerPrimero(
            "SELECT * FROM routers WHERE id_router = @id",
            Mapear,
            ConexionMySql.P("@id", id));
    }

    public static List<OpcionCombo> ListarOpciones()
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.OperarRouters, "Su rol no puede ver los routers.");
        if (bloqueo != null) throw new InvalidOperationException(bloqueo.Mensaje);

        var tabla = ConexionMySql.Consultar(
            @"SELECT id_router, CONCAT(serie, ' · ', modelo, ' · ', IFNULL(ip_gestion, 'sin IP')) AS texto
              FROM routers ORDER BY serie");
        var lista = new List<OpcionCombo>();
        foreach (DataRow fila in tabla.Rows)
        {
            lista.Add(new OpcionCombo
            {
                Id = Convert.ToInt32(fila["id_router"]),
                Texto = Convert.ToString(fila["texto"]) ?? ""
            });
        }
        return lista;
    }

    public static Respuesta Crear(Router router)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.CrearRouters, "Solo el maestro puede crear routers.");
        if (bloqueo != null) return bloqueo;
        var error = Validar(router);
        if (error != null) return Respuesta.Fallo(error);

        try
        {
            ConexionMySql.Insertar(
                @"INSERT INTO routers (serie, modelo, mac_address, ip_gestion, estado, id_servicio, ubicacion)
                  VALUES (@serie, @modelo, @mac, @ip, @estado, @servicio, @ubicacion)",
                Parametros(router));
            return Respuesta.Ok("Router creado.");
        }
        catch (Exception ex)
        {
            return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex));
        }
    }

    public static Respuesta Actualizar(Router router)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.OperarRouters, "Su rol no puede actualizar routers.");
        if (bloqueo != null) return bloqueo;
        var error = Validar(router);
        if (error != null) return Respuesta.Fallo(error);

        try
        {
            var filas = ConexionMySql.Ejecutar(
                @"UPDATE routers
                  SET serie = @serie, modelo = @modelo, mac_address = @mac, ip_gestion = @ip,
                      estado = @estado, id_servicio = @servicio, ubicacion = @ubicacion
                  WHERE id_router = @id",
                Parametros(router));
            if (filas == 0) return Respuesta.Fallo("No se encontró el router.");
            return Respuesta.Ok("Router actualizado.");
        }
        catch (Exception ex)
        {
            return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex));
        }
    }

    public static Respuesta Eliminar(int id)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.CrearRouters, "Solo el maestro puede eliminar routers.");
        if (bloqueo != null) return bloqueo;
        try
        {
            var filas = ConexionMySql.Ejecutar("DELETE FROM routers WHERE id_router = @id", ConexionMySql.P("@id", id));
            if (filas == 0) return Respuesta.Fallo("No se encontró el router.");
            return Respuesta.Ok("Router eliminado.");
        }
        catch (Exception ex)
        {
            return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex));
        }
    }

    private static string? Validar(Router router)
    {
        var error = Validador.Obligatorio(router.Serie, "Serie", 100)
            ?? Validador.Obligatorio(router.Modelo, "Modelo", 100)
            ?? Validador.Obligatorio(router.MacAddress, "MAC", 50)
            ?? Validador.Opcional(router.IpGestion, "IP de gestión", 50)
            ?? Validador.Opcional(router.Ubicacion, "Ubicación", 255)
            ?? Validador.EnLista(router.Estado, Catalogos.EstadosRouter, "Estado");
        if (error != null) return error;

        if (!string.IsNullOrWhiteSpace(router.IpGestion)
            && (!IPAddress.TryParse(router.IpGestion.Trim(), out var ip) || ip.AddressFamily != AddressFamily.InterNetwork))
            return "La IP de gestión debe ser una IPv4, por ejemplo 192.168.1.10.";

        return null;
    }

    private static MySqlParameter[] Parametros(Router router)
    {
        return new[]
        {
            ConexionMySql.P("@id", router.IdRouter),
            ConexionMySql.P("@serie", router.Serie.Trim()),
            ConexionMySql.P("@modelo", router.Modelo.Trim()),
            ConexionMySql.P("@mac", router.MacAddress.Trim()),
            ConexionMySql.P("@ip", Validador.Limpio(router.IpGestion)),
            ConexionMySql.P("@estado", router.Estado),
            ConexionMySql.P("@servicio", router.IdServicio is > 0 ? router.IdServicio : null),
            ConexionMySql.P("@ubicacion", Validador.Limpio(router.Ubicacion))
        };
    }

    private static Router Mapear(MySqlDataReader lector)
    {
        return new Router
        {
            IdRouter = LectorFila.Entero(lector, "id_router"),
            Serie = LectorFila.Texto(lector, "serie"),
            Modelo = LectorFila.Texto(lector, "modelo"),
            MacAddress = LectorFila.Texto(lector, "mac_address"),
            IpGestion = LectorFila.TextoNulo(lector, "ip_gestion"),
            Estado = LectorFila.Texto(lector, "estado"),
            IdServicio = LectorFila.EnteroNulo(lector, "id_servicio"),
            Ubicacion = LectorFila.TextoNulo(lector, "ubicacion")
        };
    }
}
