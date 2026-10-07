using AppIsp.Modelo;

namespace AppIsp.Controlador;

/// <summary>
/// Decide qué puede hacer cada rol. Las vistas ocultan botones con estas
/// propiedades, y los controladores vuelven a preguntar antes de tocar la base.
/// El maestro entra a las dos áreas: comercial y técnica.
/// </summary>
public static class PermisosControlador
{
    public static bool EsRolComercial => SesionActual.Rol == "COMERCIAL";
    public static bool EsRolTecnico => SesionActual.Rol == "TECNICO";
    public static bool EsRolMaestro => SesionActual.Rol == "MAESTRO";

    public static bool AreaComercial => EsRolComercial || EsRolMaestro;
    public static bool AreaTecnica => EsRolTecnico || EsRolMaestro;

    public static bool EditarClientes => AreaComercial;
    public static bool EliminarClientes => EsRolMaestro;
    public static bool AltaServicio => AreaComercial;
    public static bool AdministrarPlanes => EsRolMaestro;
    public static bool Facturar => EsRolMaestro;
    public static bool AdministrarUsuarios => EsRolMaestro;
    public static bool VerAuditoria => EsRolMaestro;
    public static bool OperarRouters => AreaTecnica;
    public static bool CrearRouters => EsRolMaestro;
    public static bool Provisionar => AreaTecnica;
    public static bool AgendarDisponibilidad => EsRolMaestro;
    public static bool CrearInstalacion => AreaComercial;
    public static bool EliminarInstalacion => EsRolMaestro;

    /// <summary>El técnico solo cambia el estado de la visita, no la reprograma.</summary>
    public static bool SoloEstadoInstalacion => EsRolTecnico;

    public static bool VerModulo(string clave)
    {
        if (!SesionActual.Activa)
            return false;

        return clave switch
        {
            "inicio" => true,
            "clientes" => AreaComercial,
            "servicios" => AreaComercial || AreaTecnica,
            "instalaciones" => AreaComercial || AreaTecnica,
            "tickets" => AreaComercial || AreaTecnica,
            "agenda" => AreaComercial || AreaTecnica,
            "routers" => AreaTecnica,
            "provision" => AreaTecnica,
            "remota" => AreaTecnica,
            "conexion" => AreaTecnica,
            "facturas" => EsRolMaestro,
            "planes" => EsRolMaestro,
            "usuarios" => EsRolMaestro,
            "auditoria" => EsRolMaestro,
            _ => false
        };
    }

    public static void ExigirSesion()
    {
        if (!SesionActual.Activa)
            throw new InvalidOperationException("Debe iniciar sesión.");
    }

    /// <summary>Devuelve un fallo si el rol no puede seguir. Null significa que sí puede.</summary>
    public static Respuesta? BloquearSi(bool permitido, string mensaje)
    {
        if (!SesionActual.Activa)
            return Respuesta.Fallo("Debe iniciar sesión.");
        if (!permitido)
            return Respuesta.Fallo(mensaje);
        return null;
    }
}
