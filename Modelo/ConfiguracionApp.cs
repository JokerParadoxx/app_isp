using System.Text.Json;
using MySqlConnector;

namespace AppIsp.Modelo;

/// <summary>
/// Lee appsettings.json y arma la cadena de conexión.
/// El archivo está en la raíz del proyecto y se copia junto al .exe.
/// </summary>
public static class ConfiguracionApp
{
    private static DatosMySql? _datos;

    public static DatosMySql Actual
    {
        get
        {
            _datos ??= Cargar();
            return _datos;
        }
    }

    /// <summary>
    /// Datos de MySQL tal como quedaron escritos en el archivo de configuración.
    /// </summary>
    public sealed class DatosMySql
    {
        public string Host { get; set; } = "localhost";
        public int Puerto { get; set; } = 3306;
        public string BaseDatos { get; set; } = "isp_db";
        public string Usuario { get; set; } = "root";
        public string Contrasena { get; set; } = "";
    }

    private sealed class ArchivoConfig
    {
        public DatosMySql MySql { get; set; } = new();
    }

    public static string CadenaConexion()
    {
        var datos = Actual;

        // MySqlConnectionStringBuilder escapa la contraseña si trae caracteres especiales.
        var cadena = new MySqlConnectionStringBuilder
        {
            Server = datos.Host,
            Port = (uint)datos.Puerto,
            Database = datos.BaseDatos,
            UserID = datos.Usuario,
            Password = datos.Contrasena,
            SslMode = MySqlSslMode.None,
            AllowPublicKeyRetrieval = true,
            CharacterSet = "utf8mb4",
            ConnectionTimeout = 10
        };

        return cadena.ConnectionString;
    }

    private static DatosMySql Cargar()
    {
        var ruta = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(ruta))
        {
            throw new FileNotFoundException(
                "No se encontró appsettings.json junto al programa. Ruta buscada: " + ruta);
        }

        var json = File.ReadAllText(ruta);
        var archivo = JsonSerializer.Deserialize<ArchivoConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (archivo?.MySql == null || string.IsNullOrWhiteSpace(archivo.MySql.BaseDatos))
            throw new InvalidOperationException("appsettings.json no tiene el bloque MySql.");

        return archivo.MySql;
    }
}
