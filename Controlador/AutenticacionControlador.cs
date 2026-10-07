using AppIsp.Modelo;

namespace AppIsp.Controlador;

/// <summary>
/// Comprueba el correo y la contraseña contra los usuarios que ya existen en la base.
/// No crea cuentas automáticas.
/// </summary>
public static class AutenticacionControlador
{
    public static Respuesta PrepararPrimerAcceso()
    {
        try
        {
            var idRol = ConexionMySql.Escalar("SELECT id_role FROM roles WHERE nombre = 'MAESTRO'");
            if (idRol == null)
                return Respuesta.Fallo("No existe el rol MAESTRO. Ejecute el script ddl base de datos.txt.");

            // Quita la cuenta de prueba que la app llegó a crear sola.
            ConexionMySql.Ejecutar(
                "DELETE FROM usuarios WHERE LOWER(email) = 'maestro@isp.local'");

            // La cuenta que ya está en la base (hehumam@gmail.com) queda como maestro.
            ConexionMySql.Ejecutar(
                @"UPDATE usuarios
                  SET id_role = @rol, activo = 1
                  WHERE LOWER(email) = 'hehumam@gmail.com'",
                ConexionMySql.P("@rol", Convert.ToInt32(idRol)));

            return Respuesta.Ok("Conexión correcta con " + ConexionMySql.DescripcionSegura + ".");
        }
        catch (Exception ex)
        {
            return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex));
        }
    }

    public static Respuesta Ingresar(string email, string contrasena)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(contrasena))
            return Respuesta.Fallo("Escriba el correo y la contraseña.");

        try
        {
            // El mismo mensaje sirve si el correo no existe o si la clave está mal,
            // para no contar qué cuentas hay registradas.
            var tabla = ConexionMySql.Consultar(
                @"SELECT u.id_usuario, u.nombre, u.email, u.password_hash, u.activo, u.id_role, r.nombre AS rol
                  FROM usuarios u
                  JOIN roles r ON r.id_role = u.id_role
                  WHERE LOWER(u.email) = LOWER(@email)
                  LIMIT 1",
                ConexionMySql.P("@email", email.Trim()));

            if (tabla.Rows.Count == 0)
                return Respuesta.Fallo("Correo o contraseña incorrectos.");

            var fila = tabla.Rows[0];
            var guardada = Convert.ToString(fila["password_hash"]) ?? "";
            var idUsuario = Convert.ToInt32(fila["id_usuario"]);

            // La base puede traer la clave tal cual (por ejemplo kuroro.91) o ya hasheada.
            var esTextoPlano = !guardada.StartsWith("PBKDF2$", StringComparison.Ordinal);
            var claveOk = esTextoPlano
                ? string.Equals(contrasena, guardada, StringComparison.Ordinal)
                : SeguridadContrasena.Verificar(contrasena, guardada);

            if (!claveOk)
                return Respuesta.Fallo("Correo o contraseña incorrectos.");

            if (esTextoPlano)
            {
                ConexionMySql.Ejecutar(
                    "UPDATE usuarios SET password_hash = @hash WHERE id_usuario = @id",
                    ConexionMySql.P("@hash", SeguridadContrasena.GenerarHash(contrasena)),
                    ConexionMySql.P("@id", idUsuario));
            }

            if (Convert.ToInt32(fila["activo"]) == 0)
                return Respuesta.Fallo("El usuario está desactivado. Pida al maestro que lo habilite.");

            SesionActual.Iniciar(
                Convert.ToInt32(fila["id_usuario"]),
                Convert.ToString(fila["nombre"]) ?? "",
                Convert.ToString(fila["email"]) ?? "",
                Convert.ToInt32(fila["id_role"]),
                Convert.ToString(fila["rol"]) ?? "");

            return Respuesta.Ok("Bienvenido, " + SesionActual.Nombre + ".");
        }
        catch (Exception ex)
        {
            return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex));
        }
    }
}
