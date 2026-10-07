using AppIsp.Controlador;

namespace AppIsp.Vista;

/// <summary>Resumen al entrar. Los números dependen del rol que inició sesión.</summary>
public class UcInicio : PantallaBase, IPantallaRecargable
{
    public event Action<string>? SolicitarModulo;

    private readonly FlowLayoutPanel _tarjetas = new()
    {
        Dock = DockStyle.Top,
        Height = 130,
        WrapContents = false,
        BackColor = TemaVisual.Fondo
    };
    private readonly DataGridView _grilla = InterfazAyuda.Grilla();

    public UcInicio() : base("Inicio", "Un vistazo a lo que está pendiente hoy.")
    {
        var marco = InterfazAyuda.MarcoGrilla(_grilla);
        marco.Padding = new Padding(0, 8, 0, 0);
        Cuerpo.Controls.Add(marco);
        if (PermisosControlador.AreaTecnica)
        {
            var accesos = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 52,
                WrapContents = false,
                BackColor = TemaVisual.Fondo,
                Padding = new Padding(0, 8, 0, 0)
            };
            var remota = InterfazAyuda.Boton("Configuración remota", EstiloBoton.Primario);
            var conexion = InterfazAyuda.Boton("Estado de la conexión", EstiloBoton.Secundario);
            remota.Click += (_, _) => SolicitarModulo?.Invoke("remota");
            conexion.Click += (_, _) => SolicitarModulo?.Invoke("conexion");
            accesos.Controls.Add(remota);
            accesos.Controls.Add(conexion);
            Cuerpo.Controls.Add(accesos);
        }
        Cuerpo.Controls.Add(_tarjetas);
    }

    public void Recargar()
    {
        _tarjetas.Controls.Clear();
        foreach (var tarjeta in PanelControlador.ObtenerTarjetas())
            _tarjetas.Controls.Add(CrearTarjeta(tarjeta.Titulo, tarjeta.Valor.ToString("N0")));

        _grilla.DataSource = TicketControlador.Leer("", "TODOS", 12);
        InterfazAyuda.PrepararGrilla(_grilla,
            ("id_ticket", "N°", true),
            ("cliente", "Cliente", true),
            ("tipo", "Tipo", true),
            ("categoria", "Categoría", true),
            ("prioridad", "Prioridad", true),
            ("estado", "Estado", true),
            ("asignado", "Asignado", true),
            ("fecha_creacion", "Creado", true),
            ("fecha_cierre", "Cierre", true),
            ("resumen", "Resumen", true));
        MostrarEstado("Últimos tickets visibles para su rol.");
    }

    private static Panel CrearTarjeta(string titulo, string valor)
    {
        var panel = new Panel
        {
            Width = 220,
            Height = 108,
            BackColor = Color.White,
            Margin = new Padding(0, 0, 16, 0)
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
            Location = new Point(16, 16),
            AutoSize = true
        });
        panel.Controls.Add(new Label
        {
            Text = valor,
            Font = new Font("Segoe UI Semibold", 26F),
            ForeColor = TemaVisual.Acento,
            Location = new Point(14, 44),
            AutoSize = true
        });
        return panel;
    }
}
