using System.Data;
using AppIsp.Modelo;

namespace AppIsp.Controlador;

/// <summary>
/// Simula reinicio, cambio de IP y envío de configuración.
/// No se contacta un equipo real: la acción queda escrita en log_provisionamiento.
/// </summary>
public static class ProvisionamientoControlador
{
    public sealed class Ficha
    {
        public int IdRouter { get; set; }
        public string Mac { get; set; } = "";
        public string Serie { get; set; } = "";
        public string Modelo { get; set; } = "";
        public string? IpGestion { get; set; }
        public string Estado { get; set; } = "";
        public string? Ubicacion { get; set; }
        public int IdCliente { get; set; }
        public string Cliente { get; set; } = "";
        public string Identificacion { get; set; } = "";
        public string Plan { get; set; } = "";
        public string Perfil { get; set; } = "";
        public int VelocidadMbps { get; set; }
        public decimal PrecioMensual { get; set; }
        public string EstadoServicio { get; set; } = "";
    }

    public static DataTable LeerEquipos()
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.Provisionar, "Su rol no puede aprovisionar equipos.");
        if (bloqueo != null) throw new InvalidOperationException(bloqueo.Mensaje);

        return ConexionMySql.Consultar(
            @"SELECT r.id_router, r.mac_address, IFNULL(c.id_cliente, 0) AS id_cliente,
                     IFNULL(c.nombre, 'Sin cliente') AS cliente,
                     IFNULL(p.nombre, 'Sin plan') AS plan,
                     IFNULL(p.velocidad_mbps, 0) AS velocidad_mbps,
                     r.modelo, IFNULL(r.ip_gestion, '') AS ip_gestion, r.estado
              FROM routers r
              LEFT JOIN servicios s ON s.id_servicio = r.id_servicio
              LEFT JOIN clientes c ON c.id_cliente = s.id_cliente
              LEFT JOIN planes p ON p.id_plan = s.id_plan
              ORDER BY r.mac_address");
    }

    public static Ficha? LeerFicha(int idRouter)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.Provisionar, "Su rol no puede aprovisionar equipos.");
        if (bloqueo != null) throw new InvalidOperationException(bloqueo.Mensaje);

        return ConexionMySql.LeerPrimero(
            @"SELECT r.id_router, r.mac_address, r.serie, r.modelo, r.ip_gestion, r.estado, r.ubicacion,
                     IFNULL(c.id_cliente, 0) AS id_cliente, IFNULL(c.nombre, '') AS cliente,
                     IFNULL(c.identificacion, '') AS identificacion,
                     IFNULL(p.nombre, '') AS plan, IFNULL(p.descripcion, '') AS perfil,
                     IFNULL(p.velocidad_mbps, 0) AS velocidad_mbps,
                     IFNULL(p.precio_mensual, 0) AS precio_mensual,
                     IFNULL(s.estado, '') AS estado_servicio
              FROM routers r
              LEFT JOIN servicios s ON s.id_servicio = r.id_servicio
              LEFT JOIN clientes c ON c.id_cliente = s.id_cliente
              LEFT JOIN planes p ON p.id_plan = s.id_plan
              WHERE r.id_router = @id",
            lector => new Ficha
            {
                IdRouter = LectorFila.Entero(lector, "id_router"),
                Mac = LectorFila.Texto(lector, "mac_address"),
                Serie = LectorFila.Texto(lector, "serie"),
                Modelo = LectorFila.Texto(lector, "modelo"),
                IpGestion = LectorFila.TextoNulo(lector, "ip_gestion"),
                Estado = LectorFila.Texto(lector, "estado"),
                Ubicacion = LectorFila.TextoNulo(lector, "ubicacion"),
                IdCliente = LectorFila.Entero(lector, "id_cliente"),
                Cliente = LectorFila.Texto(lector, "cliente"),
                Identificacion = LectorFila.Texto(lector, "identificacion"),
                Plan = LectorFila.Texto(lector, "plan"),
                Perfil = LectorFila.Texto(lector, "perfil"),
                VelocidadMbps = LectorFila.Entero(lector, "velocidad_mbps"),
                PrecioMensual = LectorFila.Dinero(lector, "precio_mensual"),
                EstadoServicio = LectorFila.Texto(lector, "estado_servicio")
            },
            ConexionMySql.P("@id", idRouter));
    }

    public static Respuesta Reiniciar(int idRouter)
    {
        return Ejecutar(idRouter, "REBOOT", null, null,
            "Simulación: se envió REBOOT. Un equipo real respondería en 30 a 60 segundos.");
    }

    public static Respuesta CambiarIp(int idRouter, string nuevaIp)
    {
        if (string.IsNullOrWhiteSpace(nuevaIp))
            return Respuesta.Fallo("Escriba la nueva IP.");
        return Ejecutar(idRouter, "CAMBIO_IP", nuevaIp.Trim(), null,
            "Simulación: la IP de gestión pasó a " + nuevaIp.Trim() + ".");
    }

    public static Respuesta EnviarConfiguracion(int idRouter, string configuracion)
    {
        if (string.IsNullOrWhiteSpace(configuracion))
            return Respuesta.Fallo("Escriba la configuración que quiere enviar.");
        if (configuracion.Trim().Length > 4000)
            return Respuesta.Fallo("La configuración no puede superar 4000 caracteres.");
        return Ejecutar(idRouter, "PUSH_CONFIG", null, configuracion.Trim(),
            "Simulación: la configuración quedó registrada. No se envió a un router real.");
    }

    private static Respuesta Ejecutar(int idRouter, string accion, string? nuevaIp, string? detalleExtra, string mensajeOk)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.Provisionar, "Su rol no puede aprovisionar equipos.");
        if (bloqueo != null) return bloqueo;

        try
        {
            var router = RouterControlador.LeerPorId(idRouter);
            if (router == null) return Respuesta.Fallo("No se encontró el router.");
            if (router.Estado == "RETIRADO")
                return Respuesta.Fallo("El router está retirado. No se le envían comandos.");

            if (accion == "CAMBIO_IP")
            {
                var prueba = new Router
                {
                    Serie = router.Serie,
                    Modelo = router.Modelo,
                    MacAddress = router.MacAddress,
                    Estado = router.Estado,
                    IpGestion = nuevaIp
                };
                // Reutiliza la validación de IP del controlador de routers.
                if (nuevaIp != null && !System.Net.IPAddress.TryParse(nuevaIp, out _))
                    return Respuesta.Fallo("La IP debe ser una IPv4, por ejemplo 192.168.1.10.");
                _ = prueba;
            }

            using var conexion = ConexionMySql.Abrir();
            using var transaccion = conexion.BeginTransaction();

            if (accion == "CAMBIO_IP")
            {
                ConexionMySql.Ejecutar(conexion, transaccion,
                    "UPDATE routers SET ip_gestion = @ip WHERE id_router = @id",
                    ConexionMySql.P("@ip", nuevaIp),
                    ConexionMySql.P("@id", idRouter));
            }

            var detalle = mensajeOk;
            if (!string.IsNullOrWhiteSpace(detalleExtra))
                detalle += "\n" + detalleExtra;

            ConexionMySql.Ejecutar(conexion, transaccion,
                @"INSERT INTO log_provisionamiento (id_router, id_usuario, accion, detalle)
                  VALUES (@router, @usuario, @accion, @detalle)",
                ConexionMySql.P("@router", idRouter),
                ConexionMySql.P("@usuario", SesionActual.IdUsuario),
                ConexionMySql.P("@accion", accion),
                ConexionMySql.P("@detalle", detalle));

            transaccion.Commit();
            return Respuesta.Ok(mensajeOk);
        }
        catch (Exception ex)
        {
            return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex));
        }
    }
}
