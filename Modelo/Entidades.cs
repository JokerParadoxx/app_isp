namespace AppIsp.Modelo;

// Cada clase representa una fila de una tabla del diagrama isp_db.
// Los nombres de las propiedades siguen a las columnas, pero en C#
// se usa PascalCase para que se lean bien.

/// <summary>Tabla roles. Los tres nombres fijos son COMERCIAL, TECNICO y MAESTRO.</summary>
public sealed class Rol
{
    public int IdRol { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
}

/// <summary>Tabla usuarios. Quienes entran a la aplicación, no los abonados.</summary>
public sealed class Usuario
{
    public int IdUsuario { get; set; }
    public string Nombre { get; set; } = "";
    public string Email { get; set; } = "";

    /// <summary>Hash PBKDF2 guardado en password_hash. La vista no lo muestra.</summary>
    public string PasswordHash { get; set; } = "";

    /// <summary>Clave en texto plano solo mientras se crea o se cambia. No viene de la base.</summary>
    public string? PasswordNueva { get; set; }

    public int IdRol { get; set; }
    public string RolNombre { get; set; } = "";
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
}

/// <summary>Tabla planes. El producto de internet que se ofrece.</summary>
public sealed class Plan
{
    public int IdPlan { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public int VelocidadMbps { get; set; }
    public decimal PrecioMensual { get; set; }
    public bool Activo { get; set; } = true;
}

/// <summary>Tabla clientes. El abonado del ISP.</summary>
public sealed class Cliente
{
    public int IdCliente { get; set; }
    public string Nombre { get; set; } = "";
    public string Identificacion { get; set; } = "";
    public string Direccion { get; set; } = "";
    public string? Comuna { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string Estado { get; set; } = "ACTIVO";
    public DateTime FechaAlta { get; set; } = DateTime.Today;
    public DateTime? FechaBaja { get; set; }
}

/// <summary>Tabla servicios. Un plan contratado por un cliente.</summary>
public sealed class Servicio
{
    public int IdServicio { get; set; }
    public int IdCliente { get; set; }
    public int IdPlan { get; set; }
    public string Estado { get; set; } = "PENDIENTE_INSTALACION";
    public DateTime FechaAlta { get; set; }
    public DateTime? FechaBaja { get; set; }
    public string? Comentario { get; set; }
    public string NombreCliente { get; set; } = "";
    public string NombrePlan { get; set; } = "";
}

/// <summary>Tabla routers. El equipo CPE que se aprovisiona.</summary>
public sealed class Router
{
    public int IdRouter { get; set; }
    public string Serie { get; set; } = "";
    public string Modelo { get; set; } = "";
    public string MacAddress { get; set; } = "";
    public string? IpGestion { get; set; }
    public string Estado { get; set; } = "ALMACEN";
    public int? IdServicio { get; set; }
    public string? Ubicacion { get; set; }
}

/// <summary>Configuración remota guardada para un router.</summary>
public sealed class ConfiguracionRouter
{
    public int IdRouter { get; set; }
    public string Wifi24Ssid { get; set; } = "";
    public string Wifi24Clave { get; set; } = "";
    public string Wifi24Cifrado { get; set; } = "WPA2";
    public bool Wifi24Activa { get; set; } = true;
    public string Wifi5Ssid { get; set; } = "";
    public string Wifi5Clave { get; set; } = "";
    public string Wifi5Cifrado { get; set; } = "WPA2";
    public bool Wifi5Activa { get; set; } = true;
    public bool DmzActiva { get; set; }
    public string? DmzIp { get; set; }
    public bool FirewallActivo { get; set; } = true;
    public bool SpiActivo { get; set; } = true;
    public bool BloqueoWanPing { get; set; } = true;
    public bool UpnpActivo { get; set; }
    public bool AccesoRemoto { get; set; }
    public int PuertoAdmin { get; set; } = 80;
    public string FiltroMac { get; set; } = "DESACTIVADO";
    public string Ipv4Modo { get; set; } = "DHCP";
    public string? Ipv4Ip { get; set; }
    public string? Ipv4Mascara { get; set; }
    public string? Ipv4Puerta { get; set; }
    public string? Ipv4DhcpInicio { get; set; }
    public string? Ipv4DhcpFin { get; set; }
    public bool Ipv6Activa { get; set; }
    public string Ipv6Modo { get; set; } = "DESACTIVADO";
    public string? Ipv6Ip { get; set; }
    public string? Ipv6Prefijo { get; set; }
    public string? Ipv6Puerta { get; set; }
    public string? DnsPrimario { get; set; }
    public string? DnsSecundario { get; set; }
    public string? Dns6Primario { get; set; }
    public string? Dns6Secundario { get; set; }
}

/// <summary>Tabla instalaciones. La visita para dejar el servicio funcionando.</summary>
public sealed class Instalacion
{
    public int IdInstalacion { get; set; }
    public int IdServicio { get; set; }
    public int? IdTecnico { get; set; }
    public DateTime FechaProgramada { get; set; } = DateTime.Now;
    public DateTime? FechaReal { get; set; }
    public string Estado { get; set; } = "PENDIENTE";
    public string? Comentario { get; set; }
}

/// <summary>Tabla disponibilidad_agenda. Bloques en los que un usuario puede salir a terreno.</summary>
public sealed class DisponibilidadAgenda
{
    public int IdDisponibilidad { get; set; }
    public int IdUsuario { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Today;
    public string BloqueHorario { get; set; } = "09:00-10:00";
    public bool Disponible { get; set; } = true;
}

/// <summary>Tabla facturas. La cabecera del cobro de un período.</summary>
public sealed class Factura
{
    public int IdFactura { get; set; }
    public int IdCliente { get; set; }
    public string Periodo { get; set; } = "";
    public DateTime FechaEmision { get; set; } = DateTime.Today;
    public DateTime FechaVencimiento { get; set; } = DateTime.Today.AddDays(10);
    public decimal MontoTotal { get; set; }
    public string Estado { get; set; } = "PENDIENTE";
}

/// <summary>Tabla factura_detalle. Una línea de la factura.</summary>
public sealed class FacturaDetalle
{
    public int IdDetalle { get; set; }
    public int IdFactura { get; set; }
    public int? IdServicio { get; set; }
    public string Descripcion { get; set; } = "";
    public int Cantidad { get; set; } = 1;
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}

/// <summary>Tabla tickets. Puede ser de tipo COMERCIAL o TECNICO.</summary>
public sealed class Ticket
{
    public int IdTicket { get; set; }
    public int IdCliente { get; set; }
    public int? IdServicio { get; set; }
    public string Tipo { get; set; } = "COMERCIAL";
    public string Categoria { get; set; } = "";
    public string Prioridad { get; set; } = "MEDIA";
    public string Estado { get; set; } = "ABIERTO";
    public string Descripcion { get; set; } = "";
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaCierre { get; set; }
    public int? IdAsignado { get; set; }
}

/// <summary>Tabla log_provisionamiento. Historial de acciones sobre un router.</summary>
public sealed class LogProvisionamiento
{
    public int IdLog { get; set; }
    public int IdRouter { get; set; }
    public int IdUsuario { get; set; }
    public string Accion { get; set; } = "";
    public string? Detalle { get; set; }
    public DateTime FechaAccion { get; set; }
}

/// <summary>Tabla log_comercial. Historial de altas, bajas y cambios de plan.</summary>
public sealed class LogComercial
{
    public int IdLog { get; set; }
    public int IdServicio { get; set; }
    public int IdUsuario { get; set; }
    public string TipoCambio { get; set; } = "";
    public string? Detalle { get; set; }
    public DateTime FechaCambio { get; set; }
}
