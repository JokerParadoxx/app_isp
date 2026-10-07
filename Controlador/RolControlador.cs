using System.Data;
using AppIsp.Modelo;
using MySqlConnector;

namespace AppIsp.Controlador;

/// <summary>
/// Los roles COMERCIAL, TECNICO y MAESTRO vienen del script.
/// No se crean ni se borran desde la app, porque los permisos comparan esos nombres.
/// El maestro sí puede ajustar la descripción.
/// </summary>
public static class RolControlador
{
    public static DataTable Leer()
    {
        PermisosControlador.ExigirSesion();
        return ConexionMySql.Consultar(
            "SELECT id_role, nombre, descripcion FROM roles ORDER BY id_role");
    }

    public static Rol? LeerPorId(int id)
    {
        PermisosControlador.ExigirSesion();
        return ConexionMySql.LeerPrimero(
            "SELECT id_role, nombre, descripcion FROM roles WHERE id_role = @id",
            Mapear,
            ConexionMySql.P("@id", id));
    }

    public static List<OpcionCombo> ListarOpciones()
    {
        PermisosControlador.ExigirSesion();
        var tabla = ConexionMySql.Consultar("SELECT id_role, nombre FROM roles ORDER BY id_role");
        var lista = new List<OpcionCombo>();
        foreach (DataRow fila in tabla.Rows)
        {
            lista.Add(new OpcionCombo
            {
                Id = Convert.ToInt32(fila["id_role"]),
                Texto = NombreVisible(Convert.ToString(fila["nombre"]) ?? "")
            });
        }
        return lista;
    }

    public static Respuesta Crear(Rol rol)
    {
        return Respuesta.Fallo("Los roles se cargan con el script de la base. No se agregan roles nuevos.");
    }

    public static Respuesta Actualizar(Rol rol)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.AdministrarUsuarios, "Solo el maestro puede editar roles.");
        if (bloqueo != null) return bloqueo;

        var error = Validador.Opcional(rol.Descripcion, "Descripción", 255);
        if (error != null) return Respuesta.Fallo(error);

        try
        {
            var filas = ConexionMySql.Ejecutar(
                "UPDATE roles SET descripcion = @descripcion WHERE id_role = @id",
                ConexionMySql.P("@descripcion", Validador.Limpio(rol.Descripcion)),
                ConexionMySql.P("@id", rol.IdRol));
            if (filas == 0) return Respuesta.Fallo("No se encontró el rol.");
            return Respuesta.Ok("Descripción del rol actualizada.");
        }
        catch (Exception ex)
        {
            return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex));
        }
    }

    public static Respuesta Eliminar(int id)
    {
        return Respuesta.Fallo("No se eliminan roles: los usuarios dependen de ellos y los permisos también.");
    }

    private static string NombreVisible(string nombre)
    {
        return nombre switch
        {
            "COMERCIAL" => "Comercial",
            "TECNICO" => "Técnico",
            "MAESTRO" => "Maestro (acceso total)",
            _ => nombre
        };
    }

    private static Rol Mapear(MySqlDataReader lector)
    {
        return new Rol
        {
            IdRol = LectorFila.Entero(lector, "id_role"),
            Nombre = LectorFila.Texto(lector, "nombre"),
            Descripcion = LectorFila.TextoNulo(lector, "descripcion")
        };
    }
}
