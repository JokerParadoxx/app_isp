using System.Data;
using AppIsp.Modelo;
using MySqlConnector;

namespace AppIsp.Controlador;

/// <summary>
/// Altas y cambios de quienes usan la aplicación. La contraseña se hashea aquí,
/// nunca en la vista. El listado no trae password_hash.
/// </summary>
public static class UsuarioControlador
{
    public static DataTable Leer(string texto)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.AdministrarUsuarios, "Solo el maestro puede ver los usuarios.");
        if (bloqueo != null) throw new InvalidOperationException(bloqueo.Mensaje);

        var busqueda = (texto ?? "").Trim();
        return ConexionMySql.Consultar(
            @"SELECT u.id_usuario, u.nombre, u.email, r.nombre AS rol,
                     IF(u.activo = 1, 'Sí', 'No') AS activo, u.fecha_creacion
              FROM usuarios u
              JOIN roles r ON r.id_role = u.id_role
              WHERE (@q = '' OR u.nombre LIKE @like OR u.email LIKE @like)
              ORDER BY u.nombre",
            ConexionMySql.P("@q", busqueda),
            ConexionMySql.P("@like", "%" + busqueda + "%"));
    }

    public static Usuario? LeerPorId(int id)
    {
        PermisosControlador.ExigirSesion();
        return ConexionMySql.LeerPrimero(
            @"SELECT u.id_usuario, u.nombre, u.email, u.activo, u.fecha_creacion, u.id_role, r.nombre AS rol
              FROM usuarios u
              JOIN roles r ON r.id_role = u.id_role
              WHERE u.id_usuario = @id",
            Mapear,
            ConexionMySql.P("@id", id));
    }

    public static List<OpcionCombo> ListarOpciones(bool soloTecnicos)
    {
        PermisosControlador.ExigirSesion();
        var tabla = ConexionMySql.Consultar(
            @"SELECT u.id_usuario, u.nombre
              FROM usuarios u
              JOIN roles r ON r.id_role = u.id_role
              WHERE u.activo = 1 AND (@solo = 0 OR r.nombre = 'TECNICO')
              ORDER BY u.nombre",
            ConexionMySql.P("@solo", soloTecnicos ? 1 : 0));

        var lista = new List<OpcionCombo> { new() { Id = 0, Texto = "(Sin asignar)" } };
        foreach (DataRow fila in tabla.Rows)
        {
            lista.Add(new OpcionCombo
            {
                Id = Convert.ToInt32(fila["id_usuario"]),
                Texto = Convert.ToString(fila["nombre"]) ?? ""
            });
        }
        return lista;
    }

    public static Respuesta Crear(Usuario usuario)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.AdministrarUsuarios, "Solo el maestro puede crear usuarios.");
        if (bloqueo != null) return bloqueo;

        var error = Validar(usuario, esNuevo: true);
        if (error != null) return Respuesta.Fallo(error);

        try
        {
            if (IdOcupado(usuario.IdUsuario, 0))
                return Respuesta.Fallo("Ese id ya está en uso.");
            if (CorreoOcupado(usuario.Email, 0))
                return Respuesta.Fallo("Ese correo ya está registrado.");

            ConexionMySql.Insertar(
                @"INSERT INTO usuarios (id_usuario, nombre, email, password_hash, id_role, activo)
                  VALUES (@id, @nombre, @email, @hash, @rol, @activo)",
                ConexionMySql.P("@id", usuario.IdUsuario),
                ConexionMySql.P("@nombre", usuario.Nombre.Trim()),
                ConexionMySql.P("@email", usuario.Email.Trim().ToLowerInvariant()),
                ConexionMySql.P("@hash", SeguridadContrasena.GenerarHash(usuario.PasswordNueva!)),
                ConexionMySql.P("@rol", usuario.IdRol),
                ConexionMySql.P("@activo", usuario.Activo ? 1 : 0));

            return Respuesta.Ok("Usuario creado.");
        }
        catch (Exception ex)
        {
            return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex));
        }
    }

    public static Respuesta Actualizar(Usuario usuario, int idAnterior)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.AdministrarUsuarios, "Solo el maestro puede editar usuarios.");
        if (bloqueo != null) return bloqueo;

        var error = Validar(usuario, esNuevo: false);
        if (error != null) return Respuesta.Fallo(error);

        if (idAnterior == SesionActual.IdUsuario && !usuario.Activo)
            return Respuesta.Fallo("No puede desactivar el usuario con el que entró.");

        try
        {
            if (IdOcupado(usuario.IdUsuario, idAnterior))
                return Respuesta.Fallo("Ese id ya está en uso.");
            if (CorreoOcupado(usuario.Email, idAnterior))
                return Respuesta.Fallo("Ese correo ya está registrado.");

            ConexionMySql.Ejecutar(
                @"UPDATE usuarios
                  SET id_usuario = @idNuevo, nombre = @nombre, email = @email, id_role = @rol, activo = @activo
                  WHERE id_usuario = @id",
                ConexionMySql.P("@idNuevo", usuario.IdUsuario),
                ConexionMySql.P("@nombre", usuario.Nombre.Trim()),
                ConexionMySql.P("@email", usuario.Email.Trim().ToLowerInvariant()),
                ConexionMySql.P("@rol", usuario.IdRol),
                ConexionMySql.P("@activo", usuario.Activo ? 1 : 0),
                ConexionMySql.P("@id", idAnterior));

            if (!string.IsNullOrWhiteSpace(usuario.PasswordNueva))
            {
                ConexionMySql.Ejecutar(
                    "UPDATE usuarios SET password_hash = @hash WHERE id_usuario = @id",
                    ConexionMySql.P("@hash", SeguridadContrasena.GenerarHash(usuario.PasswordNueva)),
                    ConexionMySql.P("@id", usuario.IdUsuario));
            }

            if (idAnterior == SesionActual.IdUsuario && usuario.IdUsuario != idAnterior)
                SesionActual.CambiarId(usuario.IdUsuario);

            return Respuesta.Ok("Usuario actualizado.");
        }
        catch (Exception ex)
        {
            return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex));
        }
    }

    public static Respuesta Eliminar(int id)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.AdministrarUsuarios, "Solo el maestro puede eliminar usuarios.");
        if (bloqueo != null) return bloqueo;
        if (id == SesionActual.IdUsuario)
            return Respuesta.Fallo("No puede eliminar el usuario con el que entró.");

        try
        {
            var filas = ConexionMySql.Ejecutar("DELETE FROM usuarios WHERE id_usuario = @id", ConexionMySql.P("@id", id));
            if (filas == 0) return Respuesta.Fallo("No se encontró el usuario.");
            return Respuesta.Ok("Usuario eliminado.");
        }
        catch (Exception ex)
        {
            return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex));
        }
    }

    private static bool IdOcupado(int id, int idIgnorado)
    {
        var cantidad = Convert.ToInt32(ConexionMySql.Escalar(
            "SELECT COUNT(*) FROM usuarios WHERE id_usuario = @id AND id_usuario <> @ignorar",
            ConexionMySql.P("@id", id),
            ConexionMySql.P("@ignorar", idIgnorado)));
        return cantidad > 0;
    }

    private static bool CorreoOcupado(string email, int idIgnorado)
    {
        var cantidad = Convert.ToInt32(ConexionMySql.Escalar(
            "SELECT COUNT(*) FROM usuarios WHERE LOWER(email) = LOWER(@email) AND id_usuario <> @id",
            ConexionMySql.P("@email", email.Trim()),
            ConexionMySql.P("@id", idIgnorado)));
        return cantidad > 0;
    }

    private static string? Validar(Usuario usuario, bool esNuevo)
    {
        var error = Validador.Obligatorio(usuario.Nombre, "Nombre", 100)
            ?? Validador.EmailObligatorio(usuario.Email);
        if (error != null) return error;
        if (usuario.IdUsuario <= 0) return "Indique un id numérico mayor que cero.";
        if (usuario.IdRol <= 0) return "Seleccione un rol.";
        if (esNuevo && string.IsNullOrWhiteSpace(usuario.PasswordNueva))
            return "La contraseña es obligatoria al crear el usuario.";
        if (!string.IsNullOrWhiteSpace(usuario.PasswordNueva) && usuario.PasswordNueva.Trim().Length < 6)
            return "La contraseña debe tener al menos 6 caracteres.";
        return null;
    }

    private static Usuario Mapear(MySqlDataReader lector)
    {
        return new Usuario
        {
            IdUsuario = LectorFila.Entero(lector, "id_usuario"),
            Nombre = LectorFila.Texto(lector, "nombre"),
            Email = LectorFila.Texto(lector, "email"),
            IdRol = LectorFila.Entero(lector, "id_role"),
            RolNombre = LectorFila.Texto(lector, "rol"),
            Activo = LectorFila.Booleano(lector, "activo"),
            FechaCreacion = LectorFila.Fecha(lector, "fecha_creacion")
        };
    }
}
