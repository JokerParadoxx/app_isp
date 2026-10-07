namespace AppIsp.Controlador;

/// <summary>
/// Revisa textos antes de enviarlos a MySQL.
/// Si todo está bien devuelve null. Si no, devuelve la frase del error.
/// </summary>
public static class Validador
{
    public static string? Obligatorio(string? valor, string campo, int maximo)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return "El campo \"" + campo + "\" es obligatorio.";
        if (valor.Trim().Length > maximo)
            return "El campo \"" + campo + "\" admite como máximo " + maximo + " caracteres.";
        return null;
    }

    public static string? Opcional(string? valor, string campo, int maximo)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return null;
        if (valor.Trim().Length > maximo)
            return "El campo \"" + campo + "\" admite como máximo " + maximo + " caracteres.";
        return null;
    }

    public static string? EmailOpcional(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return null;
        var correo = valor.Trim();
        if (correo.Length > 100 || !correo.Contains('@') || !correo.Contains('.'))
            return "El correo no tiene un formato válido.";
        return null;
    }

    public static string? EmailObligatorio(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return "El correo es obligatorio.";
        return EmailOpcional(valor);
    }

    public static string? EnLista(string? valor, string[] permitidos, string campo)
    {
        if (string.IsNullOrWhiteSpace(valor) || !permitidos.Contains(valor))
            return "El valor de \"" + campo + "\" no es válido.";
        return null;
    }

    public static string? Limpio(string? valor)
    {
        return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }
}
