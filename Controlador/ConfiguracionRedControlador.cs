using System.Data;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using AppIsp.Modelo;

namespace AppIsp.Controlador;

/// <summary>
/// Configuración del router y mediciones de la conexión de gestión.
/// Solo el técnico y el maestro pueden usarlo.
/// </summary>
public static class ConfiguracionRedControlador
{
    public static readonly string[] Cifrados = { "WPA2", "WPA3", "WPA2/WPA3", "WPA", "ABIERTA" };
    public static readonly string[] Protocolos = { "TCP", "UDP", "AMBOS" };
    public static readonly string[] FiltrosMac = { "DESACTIVADO", "PERMITIR", "BLOQUEAR" };
    public static readonly string[] ModosIpv4 = { "DHCP", "ESTATICA" };
    public static readonly string[] ModosIpv6 = { "DESACTIVADO", "SLAAC", "DHCP", "ESTATICA" };
    public static readonly string[] Bandas = { "2.4", "5", "LAN" };

    public static void AsegurarTablas()
    {
        ConexionMySql.Ejecutar(
            @"CREATE TABLE IF NOT EXISTS router_config (
                id_router INT PRIMARY KEY,
                wifi24_ssid VARCHAR(32) NOT NULL DEFAULT '',
                wifi24_clave VARCHAR(64) NOT NULL DEFAULT '',
                wifi24_cifrado VARCHAR(20) NOT NULL DEFAULT 'WPA2',
                wifi24_activa TINYINT(1) NOT NULL DEFAULT 1,
                wifi5_ssid VARCHAR(32) NOT NULL DEFAULT '',
                wifi5_clave VARCHAR(64) NOT NULL DEFAULT '',
                wifi5_cifrado VARCHAR(20) NOT NULL DEFAULT 'WPA2',
                wifi5_activa TINYINT(1) NOT NULL DEFAULT 1,
                dmz_activa TINYINT(1) NOT NULL DEFAULT 0,
                dmz_ip VARCHAR(45) NULL,
                firewall_activo TINYINT(1) NOT NULL DEFAULT 1,
                spi_activo TINYINT(1) NOT NULL DEFAULT 1,
                bloqueo_wan_ping TINYINT(1) NOT NULL DEFAULT 1,
                upnp_activo TINYINT(1) NOT NULL DEFAULT 0,
                acceso_remoto TINYINT(1) NOT NULL DEFAULT 0,
                puerto_admin INT NOT NULL DEFAULT 80,
                filtro_mac VARCHAR(20) NOT NULL DEFAULT 'DESACTIVADO',
                ipv4_modo VARCHAR(20) NOT NULL DEFAULT 'DHCP',
                ipv4_ip VARCHAR(45) NULL,
                ipv4_mascara VARCHAR(45) NULL,
                ipv4_puerta VARCHAR(45) NULL,
                ipv4_dhcp_inicio VARCHAR(45) NULL,
                ipv4_dhcp_fin VARCHAR(45) NULL,
                ipv6_activa TINYINT(1) NOT NULL DEFAULT 0,
                ipv6_modo VARCHAR(20) NOT NULL DEFAULT 'DESACTIVADO',
                ipv6_ip VARCHAR(45) NULL,
                ipv6_prefijo VARCHAR(10) NULL,
                ipv6_puerta VARCHAR(45) NULL,
                dns_primario VARCHAR(45) NULL,
                dns_secundario VARCHAR(45) NULL,
                dns6_primario VARCHAR(45) NULL,
                dns6_secundario VARCHAR(45) NULL,
                actualizado_en DATETIME NULL,
                FOREIGN KEY (id_router) REFERENCES routers(id_router))");
        ConexionMySql.Ejecutar(
            @"CREATE TABLE IF NOT EXISTS router_puerto (
                id_puerto INT AUTO_INCREMENT PRIMARY KEY,
                id_router INT NOT NULL,
                nombre VARCHAR(80) NOT NULL,
                protocolo VARCHAR(10) NOT NULL,
                puerto_externo INT NOT NULL,
                puerto_interno INT NOT NULL,
                ip_destino VARCHAR(45) NOT NULL,
                activo TINYINT(1) NOT NULL DEFAULT 1,
                FOREIGN KEY (id_router) REFERENCES routers(id_router))");
        ConexionMySql.Ejecutar(
            @"CREATE TABLE IF NOT EXISTS router_mac (
                id_mac INT AUTO_INCREMENT PRIMARY KEY,
                id_router INT NOT NULL,
                mac VARCHAR(17) NOT NULL,
                descripcion VARCHAR(80) NULL,
                FOREIGN KEY (id_router) REFERENCES routers(id_router))");
        ConexionMySql.Ejecutar(
            @"CREATE TABLE IF NOT EXISTS router_dispositivo (
                id_dispositivo INT AUTO_INCREMENT PRIMARY KEY,
                id_router INT NOT NULL,
                nombre VARCHAR(80) NOT NULL,
                mac VARCHAR(17) NOT NULL,
                ip VARCHAR(45) NULL,
                banda VARCHAR(10) NOT NULL DEFAULT 'LAN',
                conectado TINYINT(1) NOT NULL DEFAULT 1,
                FOREIGN KEY (id_router) REFERENCES routers(id_router))");
        ConexionMySql.Ejecutar(
            @"CREATE TABLE IF NOT EXISTS router_reserva (
                id_reserva INT AUTO_INCREMENT PRIMARY KEY,
                id_router INT NOT NULL,
                nombre VARCHAR(80) NOT NULL,
                mac VARCHAR(17) NOT NULL,
                ip VARCHAR(45) NOT NULL,
                FOREIGN KEY (id_router) REFERENCES routers(id_router))");
        ConexionMySql.Ejecutar(
            @"CREATE TABLE IF NOT EXISTS router_medicion (
                id_medicion INT AUTO_INCREMENT PRIMARY KEY,
                id_router INT NOT NULL,
                fecha DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                estado VARCHAR(20) NOT NULL,
                latencia_ms INT NOT NULL DEFAULT 0,
                perdida_pct INT NOT NULL DEFAULT 100,
                intentos INT NOT NULL DEFAULT 4,
                exitosos INT NOT NULL DEFAULT 0,
                detalle VARCHAR(255) NULL,
                FOREIGN KEY (id_router) REFERENCES routers(id_router))");
    }

    public static ConfiguracionRouter Leer(int idRouter)
    {
        Exigir();
        AsegurarTablas();
        var fila = ConexionMySql.LeerPrimero(
            "SELECT * FROM router_config WHERE id_router = @id",
            Mapear,
            ConexionMySql.P("@id", idRouter));
        return fila ?? new ConfiguracionRouter { IdRouter = idRouter };
    }

    public static Respuesta Guardar(ConfiguracionRouter config)
    {
        var bloqueo = ExigirRespuesta();
        if (bloqueo != null) return bloqueo;
        if (config.IdRouter <= 0) return Respuesta.Fallo("Seleccione un router.");

        var error = ValidarConfig(config);
        if (error != null) return Respuesta.Fallo(error);

        try
        {
            AsegurarTablas();
            ConexionMySql.Ejecutar(
                @"INSERT INTO router_config (
                    id_router, wifi24_ssid, wifi24_clave, wifi24_cifrado, wifi24_activa,
                    wifi5_ssid, wifi5_clave, wifi5_cifrado, wifi5_activa,
                    dmz_activa, dmz_ip, firewall_activo, spi_activo, bloqueo_wan_ping, upnp_activo,
                    acceso_remoto, puerto_admin, filtro_mac,
                    ipv4_modo, ipv4_ip, ipv4_mascara, ipv4_puerta, ipv4_dhcp_inicio, ipv4_dhcp_fin,
                    ipv6_activa, ipv6_modo, ipv6_ip, ipv6_prefijo, ipv6_puerta,
                    dns_primario, dns_secundario, dns6_primario, dns6_secundario, actualizado_en)
                  VALUES (
                    @id, @w24s, @w24c, @w24f, @w24a, @w5s, @w5c, @w5f, @w5a,
                    @dmz, @dmzip, @fw, @spi, @ping, @upnp, @remoto, @padmin, @mac,
                    @m4, @ip4, @mask, @gw4, @ini, @fin,
                    @v6a, @m6, @ip6, @pre, @gw6,
                    @d1, @d2, @d61, @d62, NOW())
                  ON DUPLICATE KEY UPDATE
                    wifi24_ssid = VALUES(wifi24_ssid), wifi24_clave = VALUES(wifi24_clave),
                    wifi24_cifrado = VALUES(wifi24_cifrado), wifi24_activa = VALUES(wifi24_activa),
                    wifi5_ssid = VALUES(wifi5_ssid), wifi5_clave = VALUES(wifi5_clave),
                    wifi5_cifrado = VALUES(wifi5_cifrado), wifi5_activa = VALUES(wifi5_activa),
                    dmz_activa = VALUES(dmz_activa), dmz_ip = VALUES(dmz_ip),
                    firewall_activo = VALUES(firewall_activo), spi_activo = VALUES(spi_activo),
                    bloqueo_wan_ping = VALUES(bloqueo_wan_ping), upnp_activo = VALUES(upnp_activo),
                    acceso_remoto = VALUES(acceso_remoto), puerto_admin = VALUES(puerto_admin),
                    filtro_mac = VALUES(filtro_mac),
                    ipv4_modo = VALUES(ipv4_modo), ipv4_ip = VALUES(ipv4_ip), ipv4_mascara = VALUES(ipv4_mascara),
                    ipv4_puerta = VALUES(ipv4_puerta), ipv4_dhcp_inicio = VALUES(ipv4_dhcp_inicio),
                    ipv4_dhcp_fin = VALUES(ipv4_dhcp_fin),
                    ipv6_activa = VALUES(ipv6_activa), ipv6_modo = VALUES(ipv6_modo),
                    ipv6_ip = VALUES(ipv6_ip), ipv6_prefijo = VALUES(ipv6_prefijo), ipv6_puerta = VALUES(ipv6_puerta),
                    dns_primario = VALUES(dns_primario), dns_secundario = VALUES(dns_secundario),
                    dns6_primario = VALUES(dns6_primario), dns6_secundario = VALUES(dns6_secundario),
                    actualizado_en = NOW()",
                ConexionMySql.P("@id", config.IdRouter),
                ConexionMySql.P("@w24s", config.Wifi24Ssid.Trim()),
                ConexionMySql.P("@w24c", config.Wifi24Clave),
                ConexionMySql.P("@w24f", config.Wifi24Cifrado),
                ConexionMySql.P("@w24a", config.Wifi24Activa ? 1 : 0),
                ConexionMySql.P("@w5s", config.Wifi5Ssid.Trim()),
                ConexionMySql.P("@w5c", config.Wifi5Clave),
                ConexionMySql.P("@w5f", config.Wifi5Cifrado),
                ConexionMySql.P("@w5a", config.Wifi5Activa ? 1 : 0),
                ConexionMySql.P("@dmz", config.DmzActiva ? 1 : 0),
                ConexionMySql.P("@dmzip", Vacio(config.DmzIp)),
                ConexionMySql.P("@fw", config.FirewallActivo ? 1 : 0),
                ConexionMySql.P("@spi", config.SpiActivo ? 1 : 0),
                ConexionMySql.P("@ping", config.BloqueoWanPing ? 1 : 0),
                ConexionMySql.P("@upnp", config.UpnpActivo ? 1 : 0),
                ConexionMySql.P("@remoto", config.AccesoRemoto ? 1 : 0),
                ConexionMySql.P("@padmin", config.PuertoAdmin),
                ConexionMySql.P("@mac", config.FiltroMac),
                ConexionMySql.P("@m4", config.Ipv4Modo),
                ConexionMySql.P("@ip4", Vacio(config.Ipv4Ip)),
                ConexionMySql.P("@mask", Vacio(config.Ipv4Mascara)),
                ConexionMySql.P("@gw4", Vacio(config.Ipv4Puerta)),
                ConexionMySql.P("@ini", Vacio(config.Ipv4DhcpInicio)),
                ConexionMySql.P("@fin", Vacio(config.Ipv4DhcpFin)),
                ConexionMySql.P("@v6a", config.Ipv6Activa ? 1 : 0),
                ConexionMySql.P("@m6", config.Ipv6Modo),
                ConexionMySql.P("@ip6", Vacio(config.Ipv6Ip)),
                ConexionMySql.P("@pre", Vacio(config.Ipv6Prefijo)),
                ConexionMySql.P("@gw6", Vacio(config.Ipv6Puerta)),
                ConexionMySql.P("@d1", Vacio(config.DnsPrimario)),
                ConexionMySql.P("@d2", Vacio(config.DnsSecundario)),
                ConexionMySql.P("@d61", Vacio(config.Dns6Primario)),
                ConexionMySql.P("@d62", Vacio(config.Dns6Secundario)));
            return Respuesta.Ok("Configuración del router guardada.");
        }
        catch (Exception ex)
        {
            return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex));
        }
    }

    public static DataTable LeerPuertos(int idRouter) => Listar(
        @"SELECT id_puerto, nombre, protocolo, puerto_externo, puerto_interno, ip_destino,
                 IF(activo = 1, 'Sí', 'No') AS activo
          FROM router_puerto WHERE id_router = @id ORDER BY puerto_externo", idRouter);

    public static Respuesta GuardarPuerto(int id, int idRouter, string nombre, string protocolo, int externo, int interno, string ip, bool activo)
    {
        var bloqueo = ExigirRespuesta();
        if (bloqueo != null) return bloqueo;
        if (idRouter <= 0) return Respuesta.Fallo("Seleccione un router.");
        var error = Validador.Obligatorio(nombre, "Nombre del puerto", 80)
            ?? IpObligatoria(ip, "IP de destino")
            ?? Puerto(externo, "Puerto externo")
            ?? Puerto(interno, "Puerto interno");
        if (error != null) return Respuesta.Fallo(error);
        try
        {
            AsegurarTablas();
            if (id == 0)
            {
                ConexionMySql.Ejecutar(
                    @"INSERT INTO router_puerto (id_router, nombre, protocolo, puerto_externo, puerto_interno, ip_destino, activo)
                      VALUES (@router, @nombre, @proto, @ext, @int, @ip, @activo)",
                    ConexionMySql.P("@router", idRouter),
                    ConexionMySql.P("@nombre", nombre.Trim()),
                    ConexionMySql.P("@proto", protocolo),
                    ConexionMySql.P("@ext", externo),
                    ConexionMySql.P("@int", interno),
                    ConexionMySql.P("@ip", ip.Trim()),
                    ConexionMySql.P("@activo", activo ? 1 : 0));
            }
            else
            {
                ConexionMySql.Ejecutar(
                    @"UPDATE router_puerto
                      SET nombre = @nombre, protocolo = @proto, puerto_externo = @ext,
                          puerto_interno = @int, ip_destino = @ip, activo = @activo
                      WHERE id_puerto = @id AND id_router = @router",
                    ConexionMySql.P("@nombre", nombre.Trim()),
                    ConexionMySql.P("@proto", protocolo),
                    ConexionMySql.P("@ext", externo),
                    ConexionMySql.P("@int", interno),
                    ConexionMySql.P("@ip", ip.Trim()),
                    ConexionMySql.P("@activo", activo ? 1 : 0),
                    ConexionMySql.P("@id", id),
                    ConexionMySql.P("@router", idRouter));
            }
            return Respuesta.Ok("Regla de puerto guardada.");
        }
        catch (Exception ex) { return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex)); }
    }

    public static Respuesta EliminarPuerto(int id, int idRouter) =>
        Borrar("DELETE FROM router_puerto WHERE id_puerto = @id AND id_router = @router", id, idRouter, "Regla de puerto eliminada.");

    public static DataTable LeerMac(int idRouter) => Listar(
        "SELECT id_mac, mac, IFNULL(descripcion, '') AS descripcion FROM router_mac WHERE id_router = @id ORDER BY mac", idRouter);

    public static Respuesta GuardarMac(int id, int idRouter, string mac, string? descripcion)
    {
        var bloqueo = ExigirRespuesta();
        if (bloqueo != null) return bloqueo;
        if (idRouter <= 0) return Respuesta.Fallo("Seleccione un router.");
        var error = MacObligatoria(mac) ?? Validador.Opcional(descripcion, "Descripción", 80);
        if (error != null) return Respuesta.Fallo(error);
        try
        {
            AsegurarTablas();
            if (id == 0)
            {
                ConexionMySql.Ejecutar(
                    "INSERT INTO router_mac (id_router, mac, descripcion) VALUES (@router, @mac, @desc)",
                    ConexionMySql.P("@router", idRouter),
                    ConexionMySql.P("@mac", NormalizarMac(mac)),
                    ConexionMySql.P("@desc", Vacio(descripcion)));
            }
            else
            {
                ConexionMySql.Ejecutar(
                    @"UPDATE router_mac SET mac = @mac, descripcion = @desc
                      WHERE id_mac = @id AND id_router = @router",
                    ConexionMySql.P("@mac", NormalizarMac(mac)),
                    ConexionMySql.P("@desc", Vacio(descripcion)),
                    ConexionMySql.P("@id", id),
                    ConexionMySql.P("@router", idRouter));
            }
            return Respuesta.Ok("Dirección MAC guardada.");
        }
        catch (Exception ex) { return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex)); }
    }

    public static Respuesta EliminarMac(int id, int idRouter) =>
        Borrar("DELETE FROM router_mac WHERE id_mac = @id AND id_router = @router", id, idRouter, "Dirección MAC eliminada.");

    public static DataTable LeerDispositivos(int idRouter) => Listar(
        @"SELECT id_dispositivo, nombre, mac, IFNULL(ip, '') AS ip, banda,
                 IF(conectado = 1, 'Sí', 'No') AS conectado
          FROM router_dispositivo WHERE id_router = @id ORDER BY nombre", idRouter);

    public static Respuesta GuardarDispositivo(int id, int idRouter, string nombre, string mac, string? ip, string banda, bool conectado)
    {
        var bloqueo = ExigirRespuesta();
        if (bloqueo != null) return bloqueo;
        if (idRouter <= 0) return Respuesta.Fallo("Seleccione un router.");
        var error = Validador.Obligatorio(nombre, "Nombre", 80) ?? MacObligatoria(mac) ?? IpOpcional(ip, "IP");
        if (error != null) return Respuesta.Fallo(error);
        try
        {
            AsegurarTablas();
            if (id == 0)
            {
                ConexionMySql.Ejecutar(
                    @"INSERT INTO router_dispositivo (id_router, nombre, mac, ip, banda, conectado)
                      VALUES (@router, @nombre, @mac, @ip, @banda, @con)",
                    ConexionMySql.P("@router", idRouter),
                    ConexionMySql.P("@nombre", nombre.Trim()),
                    ConexionMySql.P("@mac", NormalizarMac(mac)),
                    ConexionMySql.P("@ip", Vacio(ip)),
                    ConexionMySql.P("@banda", banda),
                    ConexionMySql.P("@con", conectado ? 1 : 0));
            }
            else
            {
                ConexionMySql.Ejecutar(
                    @"UPDATE router_dispositivo
                      SET nombre = @nombre, mac = @mac, ip = @ip, banda = @banda, conectado = @con
                      WHERE id_dispositivo = @id AND id_router = @router",
                    ConexionMySql.P("@nombre", nombre.Trim()),
                    ConexionMySql.P("@mac", NormalizarMac(mac)),
                    ConexionMySql.P("@ip", Vacio(ip)),
                    ConexionMySql.P("@banda", banda),
                    ConexionMySql.P("@con", conectado ? 1 : 0),
                    ConexionMySql.P("@id", id),
                    ConexionMySql.P("@router", idRouter));
            }
            return Respuesta.Ok("Dispositivo guardado.");
        }
        catch (Exception ex) { return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex)); }
    }

    public static Respuesta EliminarDispositivo(int id, int idRouter) =>
        Borrar("DELETE FROM router_dispositivo WHERE id_dispositivo = @id AND id_router = @router", id, idRouter, "Dispositivo eliminado.");

    public static DataTable LeerReservas(int idRouter) => Listar(
        "SELECT id_reserva, nombre, mac, ip FROM router_reserva WHERE id_router = @id ORDER BY ip", idRouter);

    public static Respuesta GuardarReserva(int id, int idRouter, string nombre, string mac, string ip)
    {
        var bloqueo = ExigirRespuesta();
        if (bloqueo != null) return bloqueo;
        if (idRouter <= 0) return Respuesta.Fallo("Seleccione un router.");
        var error = Validador.Obligatorio(nombre, "Nombre", 80) ?? MacObligatoria(mac) ?? IpObligatoria(ip, "IP reservada");
        if (error != null) return Respuesta.Fallo(error);
        try
        {
            AsegurarTablas();
            if (id == 0)
            {
                ConexionMySql.Ejecutar(
                    "INSERT INTO router_reserva (id_router, nombre, mac, ip) VALUES (@router, @nombre, @mac, @ip)",
                    ConexionMySql.P("@router", idRouter),
                    ConexionMySql.P("@nombre", nombre.Trim()),
                    ConexionMySql.P("@mac", NormalizarMac(mac)),
                    ConexionMySql.P("@ip", ip.Trim()));
            }
            else
            {
                ConexionMySql.Ejecutar(
                    @"UPDATE router_reserva SET nombre = @nombre, mac = @mac, ip = @ip
                      WHERE id_reserva = @id AND id_router = @router",
                    ConexionMySql.P("@nombre", nombre.Trim()),
                    ConexionMySql.P("@mac", NormalizarMac(mac)),
                    ConexionMySql.P("@ip", ip.Trim()),
                    ConexionMySql.P("@id", id),
                    ConexionMySql.P("@router", idRouter));
            }
            return Respuesta.Ok("Reserva de IP guardada.");
        }
        catch (Exception ex) { return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex)); }
    }

    public static Respuesta EliminarReserva(int id, int idRouter) =>
        Borrar("DELETE FROM router_reserva WHERE id_reserva = @id AND id_router = @router", id, idRouter, "Reserva eliminada.");

    public static DataTable LeerMediciones(int idRouter) => Listar(
        @"SELECT fecha, estado, latencia_ms, perdida_pct, exitosos, intentos, IFNULL(detalle, '') AS detalle
          FROM router_medicion WHERE id_router = @id ORDER BY fecha DESC LIMIT 40", idRouter);

    public static (string estado, int latencia, int perdida, int muestras, int conectados) Resumen(int idRouter)
    {
        Exigir();
        AsegurarTablas();
        var ultima = ConexionMySql.Consultar(
            @"SELECT estado, latencia_ms, perdida_pct
              FROM router_medicion WHERE id_router = @id ORDER BY fecha DESC LIMIT 1",
            ConexionMySql.P("@id", idRouter));
        var muestras = Convert.ToInt32(ConexionMySql.Escalar(
            "SELECT COUNT(*) FROM router_medicion WHERE id_router = @id", ConexionMySql.P("@id", idRouter)));
        var conectados = Convert.ToInt32(ConexionMySql.Escalar(
            "SELECT COUNT(*) FROM router_dispositivo WHERE id_router = @id AND conectado = 1",
            ConexionMySql.P("@id", idRouter)));
        if (ultima.Rows.Count == 0)
            return ("SIN MEDICIÓN", 0, 0, muestras, conectados);
        var fila = ultima.Rows[0];
        return (
            Convert.ToString(fila["estado"]) ?? "",
            Convert.ToInt32(fila["latencia_ms"]),
            Convert.ToInt32(fila["perdida_pct"]),
            muestras,
            conectados);
    }

    public static Respuesta Medir(int idRouter)
    {
        var bloqueo = ExigirRespuesta();
        if (bloqueo != null) return bloqueo;
        if (idRouter <= 0) return Respuesta.Fallo("Seleccione un router.");

        var router = RouterControlador.LeerPorId(idRouter);
        if (router == null) return Respuesta.Fallo("No se encontró el router.");
        if (string.IsNullOrWhiteSpace(router.IpGestion))
            return Respuesta.Fallo("El router no tiene IP de gestión. Cárguela en Routers.");

        const int intentos = 4;
        var exitosos = 0;
        var suma = 0L;
        string detalle;
        try
        {
            using var ping = new Ping();
            for (var i = 0; i < intentos; i++)
            {
                var respuesta = ping.Send(router.IpGestion.Trim(), 1000);
                if (respuesta.Status == IPStatus.Success)
                {
                    exitosos++;
                    suma += respuesta.RoundtripTime;
                }
            }
            detalle = exitosos + " de " + intentos + " respuestas desde " + router.IpGestion.Trim();
        }
        catch (Exception ex)
        {
            detalle = ex.Message;
        }

        var perdida = (int)Math.Round((intentos - exitosos) * 100.0 / intentos);
        var latencia = exitosos == 0 ? 0 : (int)(suma / exitosos);
        var estado = exitosos == 0 ? "SIN_RESPUESTA" : perdida >= 25 ? "DEGRADADA" : "EN_LINEA";

        try
        {
            AsegurarTablas();
            ConexionMySql.Ejecutar(
                @"INSERT INTO router_medicion (id_router, estado, latencia_ms, perdida_pct, intentos, exitosos, detalle)
                  VALUES (@id, @estado, @latencia, @perdida, @intentos, @exitosos, @detalle)",
                ConexionMySql.P("@id", idRouter),
                ConexionMySql.P("@estado", estado),
                ConexionMySql.P("@latencia", latencia),
                ConexionMySql.P("@perdida", perdida),
                ConexionMySql.P("@intentos", intentos),
                ConexionMySql.P("@exitosos", exitosos),
                ConexionMySql.P("@detalle", detalle.Length > 255 ? detalle[..255] : detalle));
            return Respuesta.Ok(Catalogos.TextoAmigable(estado) + ". Latencia " + latencia + " ms, pérdida " + perdida + "%.");
        }
        catch (Exception ex)
        {
            return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex));
        }
    }

    private static DataTable Listar(string sql, int idRouter)
    {
        Exigir();
        AsegurarTablas();
        return ConexionMySql.Consultar(sql, ConexionMySql.P("@id", idRouter));
    }

    private static Respuesta Borrar(string sql, int id, int idRouter, string ok)
    {
        var bloqueo = ExigirRespuesta();
        if (bloqueo != null) return bloqueo;
        if (id <= 0) return Respuesta.Fallo("Seleccione un registro.");
        try
        {
            var filas = ConexionMySql.Ejecutar(sql, ConexionMySql.P("@id", id), ConexionMySql.P("@router", idRouter));
            return filas == 0 ? Respuesta.Fallo("No se encontró el registro.") : Respuesta.Ok(ok);
        }
        catch (Exception ex) { return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex)); }
    }

    private static void Exigir()
    {
        var bloqueo = ExigirRespuesta();
        if (bloqueo != null) throw new InvalidOperationException(bloqueo.Mensaje);
    }

    private static Respuesta? ExigirRespuesta() =>
        PermisosControlador.BloquearSi(PermisosControlador.AreaTecnica, "Solo el técnico o el maestro puede ver esta configuración.");

    private static string? ValidarConfig(ConfiguracionRouter config)
    {
        var error = ValidarWifi("2.4 GHz", config.Wifi24Activa, config.Wifi24Ssid, config.Wifi24Clave, config.Wifi24Cifrado)
            ?? ValidarWifi("5 GHz", config.Wifi5Activa, config.Wifi5Ssid, config.Wifi5Clave, config.Wifi5Cifrado);
        if (error != null) return error;
        if (config.DmzActiva)
        {
            error = IpObligatoria(config.DmzIp, "IP de DMZ");
            if (error != null) return error;
        }
        if (config.PuertoAdmin < 1 || config.PuertoAdmin > 65535)
            return "El puerto de administración debe estar entre 1 y 65535.";
        if (config.Ipv4Modo == "ESTATICA")
        {
            error = IpObligatoria(config.Ipv4Ip, "Dirección IPv4")
                ?? IpObligatoria(config.Ipv4Mascara, "Máscara")
                ?? IpObligatoria(config.Ipv4Puerta, "Puerta de enlace");
            if (error != null) return error;
        }
        error = IpOpcional(config.Ipv4DhcpInicio, "Inicio DHCP")
            ?? IpOpcional(config.Ipv4DhcpFin, "Fin DHCP")
            ?? IpOpcional(config.DnsPrimario, "DNS primario")
            ?? IpOpcional(config.DnsSecundario, "DNS secundario");
        if (error != null) return error;
        if (config.Ipv6Activa && config.Ipv6Modo == "ESTATICA")
        {
            if (string.IsNullOrWhiteSpace(config.Ipv6Ip))
                return "Indique la dirección IPv6.";
        }
        return null;
    }

    private static string? ValidarWifi(string banda, bool activa, string ssid, string clave, string cifrado)
    {
        if (!activa) return null;
        var error = Validador.Obligatorio(ssid, "Nombre Wi-Fi " + banda, 32);
        if (error != null) return error;
        if (cifrado == "ABIERTA") return null;
        if (string.IsNullOrWhiteSpace(clave) || clave.Length < 8 || clave.Length > 63)
            return "La contraseña Wi-Fi " + banda + " debe tener entre 8 y 63 caracteres.";
        return null;
    }

    private static string? Puerto(int valor, string campo) =>
        valor < 1 || valor > 65535 ? "El campo \"" + campo + "\" debe estar entre 1 y 65535." : null;

    private static string? IpObligatoria(string? valor, string campo)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return "El campo \"" + campo + "\" es obligatorio.";
        return IpValida(valor.Trim()) ? null : "El campo \"" + campo + "\" no es una IP válida.";
    }

    private static string? IpOpcional(string? valor, string campo)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        return IpValida(valor.Trim()) ? null : "El campo \"" + campo + "\" no es una IP válida.";
    }

    private static bool IpValida(string valor) =>
        Regex.IsMatch(valor, @"^(\d{1,3}\.){3}\d{1,3}$") || valor.Contains(':');

    private static string? MacObligatoria(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return "La dirección MAC es obligatoria.";
        var limpia = valor.Trim().Replace("-", ":").ToUpperInvariant();
        return Regex.IsMatch(limpia, @"^([0-9A-F]{2}:){5}[0-9A-F]{2}$")
            ? null
            : "La MAC debe tener el formato AA:BB:CC:DD:EE:FF.";
    }

    private static string NormalizarMac(string valor) =>
        valor.Trim().Replace("-", ":").ToUpperInvariant();

    private static object? Vacio(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static ConfiguracionRouter Mapear(MySqlConnector.MySqlDataReader lector)
    {
        return new ConfiguracionRouter
        {
            IdRouter = LectorFila.Entero(lector, "id_router"),
            Wifi24Ssid = LectorFila.Texto(lector, "wifi24_ssid"),
            Wifi24Clave = LectorFila.Texto(lector, "wifi24_clave"),
            Wifi24Cifrado = LectorFila.Texto(lector, "wifi24_cifrado"),
            Wifi24Activa = LectorFila.Booleano(lector, "wifi24_activa"),
            Wifi5Ssid = LectorFila.Texto(lector, "wifi5_ssid"),
            Wifi5Clave = LectorFila.Texto(lector, "wifi5_clave"),
            Wifi5Cifrado = LectorFila.Texto(lector, "wifi5_cifrado"),
            Wifi5Activa = LectorFila.Booleano(lector, "wifi5_activa"),
            DmzActiva = LectorFila.Booleano(lector, "dmz_activa"),
            DmzIp = LectorFila.Texto(lector, "dmz_ip"),
            FirewallActivo = LectorFila.Booleano(lector, "firewall_activo"),
            SpiActivo = LectorFila.Booleano(lector, "spi_activo"),
            BloqueoWanPing = LectorFila.Booleano(lector, "bloqueo_wan_ping"),
            UpnpActivo = LectorFila.Booleano(lector, "upnp_activo"),
            AccesoRemoto = LectorFila.Booleano(lector, "acceso_remoto"),
            PuertoAdmin = LectorFila.Entero(lector, "puerto_admin"),
            FiltroMac = LectorFila.Texto(lector, "filtro_mac"),
            Ipv4Modo = LectorFila.Texto(lector, "ipv4_modo"),
            Ipv4Ip = LectorFila.Texto(lector, "ipv4_ip"),
            Ipv4Mascara = LectorFila.Texto(lector, "ipv4_mascara"),
            Ipv4Puerta = LectorFila.Texto(lector, "ipv4_puerta"),
            Ipv4DhcpInicio = LectorFila.Texto(lector, "ipv4_dhcp_inicio"),
            Ipv4DhcpFin = LectorFila.Texto(lector, "ipv4_dhcp_fin"),
            Ipv6Activa = LectorFila.Booleano(lector, "ipv6_activa"),
            Ipv6Modo = LectorFila.Texto(lector, "ipv6_modo"),
            Ipv6Ip = LectorFila.Texto(lector, "ipv6_ip"),
            Ipv6Prefijo = LectorFila.Texto(lector, "ipv6_prefijo"),
            Ipv6Puerta = LectorFila.Texto(lector, "ipv6_puerta"),
            DnsPrimario = LectorFila.Texto(lector, "dns_primario"),
            DnsSecundario = LectorFila.Texto(lector, "dns_secundario"),
            Dns6Primario = LectorFila.Texto(lector, "dns6_primario"),
            Dns6Secundario = LectorFila.Texto(lector, "dns6_secundario")
        };
    }
}
