using MySqlConnector;

namespace AppIsp.Modelo;

/// <summary>
/// Ayuda a leer una fila sin repetir GetOrdinal en cada controlador.
/// MySQL a veces entrega los TINYINT(1) como bool y a veces como número;
/// Booleano acepta los dos para que activo y disponible no fallen.
/// </summary>
public static class LectorFila
{
    public static int Entero(MySqlDataReader lector, string columna)
    {
        return Convert.ToInt32(lector.GetValue(lector.GetOrdinal(columna)));
    }

    public static int? EnteroNulo(MySqlDataReader lector, string columna)
    {
        var indice = lector.GetOrdinal(columna);
        if (lector.IsDBNull(indice))
            return null;
        return Convert.ToInt32(lector.GetValue(indice));
    }

    public static string Texto(MySqlDataReader lector, string columna)
    {
        var indice = lector.GetOrdinal(columna);
        if (lector.IsDBNull(indice))
            return "";
        return Convert.ToString(lector.GetValue(indice)) ?? "";
    }

    public static string? TextoNulo(MySqlDataReader lector, string columna)
    {
        var indice = lector.GetOrdinal(columna);
        if (lector.IsDBNull(indice))
            return null;
        var texto = Convert.ToString(lector.GetValue(indice));
        return string.IsNullOrWhiteSpace(texto) ? null : texto;
    }

    public static bool Booleano(MySqlDataReader lector, string columna)
    {
        var indice = lector.GetOrdinal(columna);
        if (lector.IsDBNull(indice))
            return false;

        var valor = lector.GetValue(indice);
        return valor switch
        {
            bool bandera => bandera,
            byte numero => numero != 0,
            sbyte numero => numero != 0,
            short numero => numero != 0,
            int numero => numero != 0,
            long numero => numero != 0,
            _ => Convert.ToInt32(valor) != 0
        };
    }

    public static DateTime Fecha(MySqlDataReader lector, string columna)
    {
        return lector.GetDateTime(lector.GetOrdinal(columna));
    }

    public static DateTime? FechaNula(MySqlDataReader lector, string columna)
    {
        var indice = lector.GetOrdinal(columna);
        if (lector.IsDBNull(indice))
            return null;
        return lector.GetDateTime(indice);
    }

    public static decimal Dinero(MySqlDataReader lector, string columna)
    {
        return Convert.ToDecimal(lector.GetValue(lector.GetOrdinal(columna)));
    }
}
