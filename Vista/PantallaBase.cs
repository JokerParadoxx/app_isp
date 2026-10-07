namespace AppIsp.Vista;

/// <summary>Las pantallas de módulos se recargan la primera vez que se abren.</summary>
public interface IPantallaRecargable
{
    void Recargar();
}

/// <summary>
/// Base de cada módulo: título, descripción y una franja de estado abajo.
/// El contenido concreto se agrega en Cuerpo.
/// </summary>
public class PantallaBase : UserControl
{
    protected Panel Cuerpo { get; }
    protected Label Estado { get; }

    protected PantallaBase(string titulo, string descripcion)
    {
        Dock = DockStyle.Fill;
        BackColor = TemaVisual.Fondo;

        var encabezado = new Panel
        {
            Dock = DockStyle.Top,
            Height = 72,
            BackColor = TemaVisual.Fondo,
            Padding = new Padding(4, 0, 0, 0)
        };
        var lblTitulo = new Label
        {
            Text = titulo,
            Font = TemaVisual.FuenteTitulo,
            ForeColor = TemaVisual.Texto,
            Dock = DockStyle.Top,
            Height = 36
        };
        var lblDescripcion = new Label
        {
            Text = descripcion,
            Font = TemaVisual.FuentePequena,
            ForeColor = TemaVisual.TextoSuave,
            Dock = DockStyle.Top,
            Height = 24
        };
        encabezado.Controls.Add(lblDescripcion);
        encabezado.Controls.Add(lblTitulo);

        Estado = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 24,
            Font = TemaVisual.FuentePequena,
            ForeColor = TemaVisual.Exito,
            TextAlign = ContentAlignment.MiddleLeft
        };

        Cuerpo = new Panel { Dock = DockStyle.Fill, BackColor = TemaVisual.Fondo, Padding = new Padding(4, 0, 0, 0) };

        Controls.Add(Cuerpo);
        Controls.Add(Estado);
        Controls.Add(encabezado);
    }

    protected void Informar(Modelo.Respuesta respuesta)
    {
        Estado.Text = respuesta.Mensaje;
        Estado.ForeColor = respuesta.Exito ? TemaVisual.Exito : TemaVisual.Peligro;
        if (!respuesta.Exito)
            InterfazAyuda.ErrorTexto(respuesta.Mensaje);
    }

    protected void MostrarEstado(string mensaje, bool error = false)
    {
        Estado.Text = mensaje;
        Estado.ForeColor = error ? TemaVisual.Peligro : TemaVisual.Exito;
    }
}
