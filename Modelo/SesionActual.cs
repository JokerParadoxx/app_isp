namespace AppIsp.Modelo;

/// <summary>
/// Recuerda quién entró. Es estático porque en esta aplicación de escritorio
/// hay un solo usuario a la vez. Al cerrar sesión se limpia.
/// </summary>
public static class SesionActual
{
    public static bool Activa { get; private set; }
    public static int IdUsuario { get; private set; }
    public static string Nombre { get; private set; } = "";
    public static string Email { get; private set; } = "";
    public static int IdRol { get; private set; }

    /// <summary>Nombre del rol tal como está en la tabla roles: COMERCIAL, TECNICO o MAESTRO.</summary>
    public static string Rol { get; private set; } = "";

    public static string RolVisible => Rol switch
    {
        "COMERCIAL" => "Comercial",
        "TECNICO" => "Técnico",
        "MAESTRO" => "Maestro",
        _ => Rol
    };

    public static void Iniciar(int idUsuario, string nombre, string email, int idRol, string rol)
    {
        IdUsuario = idUsuario;
        Nombre = nombre;
        Email = email;
        IdRol = idRol;
        Rol = rol;
        Activa = true;
    }

    public static void CambiarId(int idUsuario) => IdUsuario = idUsuario;

    public static void Cerrar()
    {
        Activa = false;
        IdUsuario = 0;
        Nombre = "";
        Email = "";
        IdRol = 0;
        Rol = "";
    }
}
