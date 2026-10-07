using System.Data;
using MySqlConnector;

namespace AppIsp.Modelo;

/// <summary>
/// Único lugar que abre MySQL. Los controladores le piden consultar o ejecutar;
/// las vistas no escriben SQL. Cada método abre y cierra su conexión: es más
/// simple de seguir que compartir una conexión entre pantallas.
/// </summary>
public static class ConexionMySql
{
    public static string DescripcionSegura
    {
        get
        {
            try
            {
                var datos = ConfiguracionApp.Actual;
                return datos.Host + ":" + datos.Puerto + "/" + datos.BaseDatos;
            }
            catch
            {
                return "MySQL";
            }
        }
    }

    /// <summary>Abre la conexión. El using del que llama la cierra aunque haya un error.</summary>
    public static MySqlConnection Abrir()
    {
        var conexion = new MySqlConnection(ConfiguracionApp.CadenaConexion());
        conexion.Open();
        return conexion;
    }

    /// <summary>Crea un parámetro. Si el valor viene null, se envía NULL a MySQL.</summary>
    public static MySqlParameter P(string nombre, object? valor)
    {
        return new MySqlParameter(nombre, valor ?? DBNull.Value);
    }

    public static DataTable Consultar(string sql, params MySqlParameter[] parametros)
    {
        using var conexion = Abrir();
        return Consultar(conexion, null, sql, parametros);
    }

    public static DataTable Consultar(MySqlConnection conexion, MySqlTransaction? transaccion, string sql, params MySqlParameter[] parametros)
    {
        using var comando = Comando(conexion, transaccion, sql, parametros);
        using var adaptador = new MySqlDataAdapter(comando);
        var tabla = new DataTable();
        adaptador.Fill(tabla);
        return tabla;
    }

    public static int Ejecutar(string sql, params MySqlParameter[] parametros)
    {
        using var conexion = Abrir();
        return Ejecutar(conexion, null, sql, parametros);
    }

    public static int Ejecutar(MySqlConnection conexion, MySqlTransaction? transaccion, string sql, params MySqlParameter[] parametros)
    {
        using var comando = Comando(conexion, transaccion, sql, parametros);
        return comando.ExecuteNonQuery();
    }

    /// <summary>Ejecuta un INSERT y devuelve el id autogenerado.</summary>
    public static int Insertar(string sql, params MySqlParameter[] parametros)
    {
        using var conexion = Abrir();
        return Insertar(conexion, null, sql, parametros);
    }

    public static int Insertar(MySqlConnection conexion, MySqlTransaction? transaccion, string sql, params MySqlParameter[] parametros)
    {
        using var comando = Comando(conexion, transaccion, sql, parametros);
        comando.ExecuteNonQuery();
        return Convert.ToInt32(comando.LastInsertedId);
    }

    public static object? Escalar(string sql, params MySqlParameter[] parametros)
    {
        using var conexion = Abrir();
        return Escalar(conexion, null, sql, parametros);
    }

    public static object? Escalar(MySqlConnection conexion, MySqlTransaction? transaccion, string sql, params MySqlParameter[] parametros)
    {
        using var comando = Comando(conexion, transaccion, sql, parametros);
        var valor = comando.ExecuteScalar();
        return valor == null || valor == DBNull.Value ? null : valor;
    }

    public static T? LeerPrimero<T>(string sql, Func<MySqlDataReader, T> mapear, params MySqlParameter[] parametros) where T : class
    {
        using var conexion = Abrir();
        using var comando = Comando(conexion, null, sql, parametros);
        using var lector = comando.ExecuteReader();
        if (!lector.Read())
            return null;
        return mapear(lector);
    }

    /// <summary>
    /// Traduce los errores frecuentes de MySQL a una frase que se puede mostrar.
    /// El número es el código oficial del servidor (1062 = duplicado, 1451 = clave foránea, etc.).
    /// </summary>
    public static string MensajeAmigable(Exception ex)
    {
        if (ex is MySqlException mysql)
        {
            return mysql.Number switch
            {
                0 or 1042 => "No se pudo conectar a MySQL (" + DescripcionSegura + "). Revise que el servicio esté encendido.",
                1045 => "MySQL rechazó el usuario o la contraseña. Revise appsettings.json.",
                1049 => "No existe la base de datos. Ejecute el script ddl base de datos.txt para crear isp_db.",
                1146 => "Falta una tabla. Ejecute el script ddl base de datos.txt.",
                1062 => "Ya existe un registro con ese dato único (correo, identificación, serie, MAC u otro).",
                1451 => "No se puede eliminar porque otros registros todavía lo están usando.",
                1452 => "Falta un dato relacionado. Revise el cliente, el plan, el servicio o el usuario.",
                1406 => "Un texto es más largo de lo que permite la base de datos.",
                1292 => "Hay una fecha u hora que MySQL no acepta.",
                _ => "Error de MySQL (" + mysql.Number + "): " + mysql.Message
            };
        }

        return ex.Message;
    }

    private static MySqlCommand Comando(MySqlConnection conexion, MySqlTransaction? transaccion, string sql, params MySqlParameter[] parametros)
    {
        var comando = transaccion == null
            ? new MySqlCommand(sql, conexion)
            : new MySqlCommand(sql, conexion, transaccion);

        if (parametros != null)
        {
            foreach (var parametro in parametros)
                comando.Parameters.Add(parametro);
        }

        return comando;
    }
}
