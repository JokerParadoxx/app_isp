using AppIsp.Controlador;
using AppIsp.Modelo;

namespace AppIsp.Vista;

/// <summary>
/// Mediciones de la IP de gestión del router. La ve el técnico y el maestro.
/// </summary>
public class UcEstadoConexion : PantallaBase, IPantallaRecargable
{
    private readonly ComboBox _router = InterfazAyuda.Lista();
    private readonly DataGridView _grilla = InterfazAyuda.Grilla();
    private readonly FlowLayoutPanel _tarjetas = new()
    {
        Dock = DockStyle.Top,
        Height = 130,
        WrapContents = false,
        BackColor = TemaVisual.Fondo
    };
    private bool _sincronizando;

    public UcEstadoConexion() : base(
        "Estado de la conexión",
        "Latencia, pérdida y últimas mediciones contra la IP de gestión del router.")
    {
        var barra = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = TemaVisual.Fondo };
        barra.Controls.Add(new Label
        {
            Text = "Router",
            AutoSize = true,
            Location = new Point(0, 14),
            ForeColor = TemaVisual.TextoSuave
        });
        _router.Location = new Point(64, 8);
        _router.Width = 420;
        _router.SelectedIndexChanged += (_, _) => { if (!_sincronizando) Cargar(); };
        var medir = InterfazAyuda.Boton("Medir ahora", EstiloBoton.Primario);
        medir.Location = new Point(500, 6);
        medir.Click += (_, _) => Medir();
        barra.Controls.Add(medir);
        barra.Controls.Add(_router);

        var marco = InterfazAyuda.MarcoGrilla(_grilla);
        marco.Padding = new Padding(0, 8, 0, 0);
        Cuerpo.Controls.Add(marco);
        Cuerpo.Controls.Add(_tarjetas);
        Cuerpo.Controls.Add(barra);
    }

    public void Recargar()
    {
        try
        {
            _sincronizando = true;
            var id = InterfazAyuda.IdSeleccionado(_router);
            InterfazAyuda.CargarOpciones(_router, RouterControlador.ListarOpciones());
            if (id > 0) InterfazAyuda.SeleccionarId(_router, id);
        }
        catch (Exception ex) { InterfazAyuda.Error(ex); }
        finally { _sincronizando = false; }
        Cargar();
    }

    private void Cargar()
    {
        var id = InterfazAyuda.IdSeleccionado(_router);
        _tarjetas.Controls.Clear();
        if (id <= 0)
        {
            MostrarEstado("No hay routers para medir.", true);
            _grilla.DataSource = null;
            return;
        }

        try
        {
            var resumen = ConfiguracionRedControlador.Resumen(id);
            _tarjetas.Controls.Add(Tarjeta("Estado", Catalogos.TextoAmigable(resumen.estado)));
            _tarjetas.Controls.Add(Tarjeta("Latencia", resumen.latencia + " ms"));
            _tarjetas.Controls.Add(Tarjeta("Pérdida", resumen.perdida + " %"));
            _tarjetas.Controls.Add(Tarjeta("Mediciones", resumen.muestras.ToString("N0")));
            _tarjetas.Controls.Add(Tarjeta("Conectados", resumen.conectados.ToString("N0")));
            _grilla.DataSource = ConfiguracionRedControlador.LeerMediciones(id);
            InterfazAyuda.PrepararGrilla(_grilla,
                ("fecha", "Fecha", true),
                ("estado", "Estado", true),
                ("latencia_ms", "Latencia ms", true),
                ("perdida_pct", "Pérdida %", true),
                ("exitosos", "Respuestas", true),
                ("intentos", "Intentos", true),
                ("detalle", "Detalle", true));
            MostrarEstado("Historial de la conexión de gestión.");
        }
        catch (Exception ex) { InterfazAyuda.Error(ex); }
    }

    private void Medir()
    {
        UseWaitCursor = true;
        try
        {
            Informar(ConfiguracionRedControlador.Medir(InterfazAyuda.IdSeleccionado(_router)));
            if (Estado.ForeColor == TemaVisual.Exito) Cargar();
        }
        finally { UseWaitCursor = false; }
    }

    private static Panel Tarjeta(string titulo, string valor)
    {
        var panel = new Panel
        {
            Width = 180,
            Height = 108,
            BackColor = Color.White,
            Margin = new Padding(0, 0, 12, 0)
        };
        panel.Paint += (_, e) =>
        {
            using var lapiz = new Pen(TemaVisual.Borde);
            e.Graphics.DrawRectangle(lapiz, 0, 0, panel.Width - 1, panel.Height - 1);
        };
        panel.Controls.Add(new Label
        {
            Text = titulo,
            ForeColor = TemaVisual.TextoSuave,
            Location = new Point(14, 16),
            AutoSize = true
        });
        panel.Controls.Add(new Label
        {
            Text = valor,
            Font = TemaVisual.FuenteSemibold,
            ForeColor = TemaVisual.Acento,
            Location = new Point(14, 48),
            MaximumSize = new Size(150, 48),
            AutoSize = true
        });
        return panel;
    }
}
