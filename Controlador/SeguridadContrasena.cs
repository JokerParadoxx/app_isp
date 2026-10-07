using System.Security.Cryptography;

namespace AppIsp.Controlador;

/// <summary>
/// Guarda la contraseña con PBKDF2 (muchas vueltas de SHA-256 más una sal aleatoria).
/// En la base nunca queda la clave original, solo el hash con formato:
/// PBKDF2$salEnBase64$hashEnBase64
/// </summary>
public static class SeguridadContrasena
{
    private const int Vueltas = 100_000;

    public static string GenerarHash(string contrasena)
    {
        var sal = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(contrasena, sal, Vueltas, HashAlgorithmName.SHA256, 32);
        return "PBKDF2$" + Convert.ToBase64String(sal) + "$" + Convert.ToBase64String(hash);
    }

    public static bool Verificar(string contrasena, string almacenado)
    {
        try
        {
            var partes = almacenado.Split('$');
            if (partes.Length != 3 || partes[0] != "PBKDF2")
                return false;

            var sal = Convert.FromBase64String(partes[1]);
            var esperado = Convert.FromBase64String(partes[2]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(contrasena, sal, Vueltas, HashAlgorithmName.SHA256, 32);
            return CryptographicOperations.FixedTimeEquals(actual, esperado);
        }
        catch
        {
            return false;
        }
    }
}
