namespace AppIsp.Modelo;

/// <summary>
/// Resultado de una operación que modifica datos.
/// El controlador no abre ventanas: solo dice si salió bien y deja un mensaje.
/// La vista decide si lo muestra en la barra de estado o en un cuadro de diálogo.
/// </summary>
public sealed class Respuesta
{
    public bool Exito { get; }
    public string Mensaje { get; }

    private Respuesta(bool exito, string mensaje)
    {
        Exito = exito;
        Mensaje = mensaje;
    }

    public static Respuesta Ok(string mensaje) => new(true, mensaje);

    public static Respuesta Fallo(string mensaje) => new(false, mensaje);
}
