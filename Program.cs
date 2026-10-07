using System.Globalization;
using AppIsp.Controlador;
using AppIsp.Modelo;
using AppIsp.Vista;

namespace AppIsp;

/// <summary>
/// Punto de entrada de la aplicación. Este archivo vive en la raíz de app_isp
/// y es el que abre el programa: primero el login y, si la clave es correcta,
/// la ventana principal según el rol.
/// </summary>
internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        // Permite probar la base sin abrir ventanas:
        //   dotnet run -- --probar-conexion
        if (args.Length > 0 && args[0] == "--probar-conexion")
        {
            ProbarConexion();
            return;
        }

        // Fechas y números con formato de Chile (24-09-2026, miles con punto).
        var cultura = new CultureInfo("es-CL");
        CultureInfo.DefaultThreadCurrentCulture = cultura;
        CultureInfo.DefaultThreadCurrentUICulture = cultura;
        Thread.CurrentThread.CurrentCulture = cultura;
        Thread.CurrentThread.CurrentUICulture = cultura;

        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, evento) =>
        {
            MessageBox.Show(
                ConexionMySql.MensajeAmigable(evento.Exception),
                "ISP PLC",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        };

        // El login y el menú se turnan. Al cerrar sesión se vuelve a pedir la clave.
        while (true)
        {
            using var login = new FrmLogin();
            if (login.ShowDialog() != DialogResult.OK)
                break;

            using var principal = new FrmPrincipal();
            principal.ShowDialog();

            SesionActual.Cerrar();
            if (!principal.VolverAlLogin)
                break;
        }
    }

    /// <summary>
    /// Abre MySQL y confirma que existan las tablas. No crea usuarios.
    /// </summary>
    private static void ProbarConexion()
    {
        try
        {
            var preparacion = AutenticacionControlador.PrepararPrimerAcceso();
            Console.WriteLine(preparacion.Exito
                ? "OK " + preparacion.Mensaje
                : "ERROR " + preparacion.Mensaje);
            Environment.ExitCode = preparacion.Exito ? 0 : 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR " + ConexionMySql.MensajeAmigable(ex));
            Environment.ExitCode = 1;
        }
    }
}
