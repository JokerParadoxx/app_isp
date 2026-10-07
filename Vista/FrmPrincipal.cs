using AppIsp.Controlador;
using AppIsp.Modelo;

namespace AppIsp.Vista;

/// <summary>
/// Ventana principal. El menú de la izquierda cambia según el rol.
/// Cada módulo se crea una sola vez y se vuelve a mostrar al pulsarlo.
/// </summary>
public class FrmPrincipal : Form
{
    private readonly Panel _contenido = new() { Dock = DockStyle.Fill, BackColor = TemaVisual.Fondo };
    private readonly Dictionary<string, UserControl> _abiertos = new();
    private readonly Dictionary<string, Button> _botones = new();
    private string _claveActual = "";

    public bool VolverAlLogin { get; private set; }

    private sealed class ItemMenu
    {
        public string Clave { get; }
        public string Texto { get; }
        public Func<UserControl> Crear { get; }
        public ItemMenu(string clave, string texto, Func<UserControl> crear)
        {
            Clave = clave;
            Texto = texto;
            Crear = crear;
        }
    }

    public FrmPrincipal()
    {
        Text = "ISP PLC — " + SesionActual.RolVisible;
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1100, 700);
        BackColor = TemaVisual.Fondo;
        StartPosition = FormStartPosition.CenterScreen;
        Font = TemaVisual.Fuente;

        var lateral = new Panel { Dock = DockStyle.Left, Width = 248, BackColor = TemaVisual.Barra };
        var marca = new Panel { Dock = DockStyle.Top, Height = 88, BackColor = TemaVisual.Barra };
        marca.Controls.Add(new Label
        {
            Text = "Sistema de gestión",
            ForeColor = Color.FromArgb(170, 196, 220),
            Font = TemaVisual.FuentePequena,
            Location = new Point(20, 48),
            AutoSize = true
        });
        marca.Controls.Add(new Label
        {
            Text = "ISP PLC",
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 18F),
            Location = new Point(18, 14),
            AutoSize = true
        });

        var usuario = new Panel { Dock = DockStyle.Top, Height = 68, BackColor = TemaVisual.Barra };
        usuario.Controls.Add(new Label
        {
            Text = SesionActual.RolVisible,
            ForeColor = Color.FromArgb(140, 196, 230),
            Font = TemaVisual.FuentePequena,
            Location = new Point(20, 36),
            AutoSize = true
        });
        usuario.Controls.Add(new Label
        {
            Text = SesionActual.Nombre,
            ForeColor = Color.White,
            Font = TemaVisual.FuenteSemibold,
            Location = new Point(20, 12),
            AutoSize = true,
            MaximumSize = new Size(200, 0)
        });

        var menu = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = TemaVisual.Barra,
            Padding = new Padding(0, 8, 0, 8)
        };

        foreach (var item in MenuSegunRol())
        {
            var boton = new Button
            {
                Text = "   " + item.Texto,
                TextAlign = ContentAlignment.MiddleLeft,
                Width = 232,
                Height = 42,
                Margin = new Padding(8, 2, 8, 2),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(226, 236, 245),
                BackColor = TemaVisual.Barra,
                Font = TemaVisual.Fuente,
                Cursor = Cursors.Hand,
                Tag = item.Clave
            };
            boton.FlatAppearance.BorderSize = 0;
            boton.FlatAppearance.MouseOverBackColor = TemaVisual.BarraHover;
            var clave = item.Clave;
            boton.Click += (_, _) => Mostrar(clave, item.Crear);
            _botones[clave] = boton;
            menu.Controls.Add(boton);
        }

        var salir = new Button
        {
            Text = "Cerrar sesión",
            Dock = DockStyle.Bottom,
            Height = 48,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.FromArgb(226, 236, 245),
            BackColor = TemaVisual.Barra,
            Cursor = Cursors.Hand
        };
        salir.FlatAppearance.BorderSize = 0;
        salir.FlatAppearance.MouseOverBackColor = TemaVisual.BarraHover;
        salir.Click += (_, _) =>
        {
            if (!InterfazAyuda.Confirmar("¿Cerrar la sesión y volver al ingreso?"))
                return;
            VolverAlLogin = true;
            Close();
        };

        var estado = new Panel { Dock = DockStyle.Bottom, Height = 28, BackColor = Color.FromArgb(232, 238, 244) };
        estado.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(16, 0, 0, 0),
            ForeColor = TemaVisual.TextoSuave,
            Font = TemaVisual.FuentePequena,
            Text = "Conectado a " + ConexionMySql.DescripcionSegura + "    ·    " + SesionActual.Email + "    ·    " + SesionActual.RolVisible
        });

        lateral.Controls.Add(menu);
        lateral.Controls.Add(salir);
        lateral.Controls.Add(usuario);
        lateral.Controls.Add(marca);

        var margen = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 12, 20, 8), BackColor = TemaVisual.Fondo };
        margen.Controls.Add(_contenido);

        Controls.Add(margen);
        Controls.Add(estado);
        Controls.Add(lateral);

        Load += (_, _) =>
        {
            if (_botones.ContainsKey("inicio"))
                Mostrar("inicio", () => new UcInicio());
        };
    }

    private void Mostrar(string clave, Func<UserControl> crear)
    {
        if (!_abiertos.TryGetValue(clave, out var pantalla))
        {
            pantalla = crear();
            pantalla.Dock = DockStyle.Fill;
            if (pantalla is UcInicio inicio)
                inicio.SolicitarModulo += AbrirDesdeInicio;
            _abiertos[clave] = pantalla;
            _contenido.Controls.Clear();
            _contenido.Controls.Add(pantalla);
            if (pantalla is IPantallaRecargable recargable)
            {
                try { recargable.Recargar(); }
                catch (Exception ex) { InterfazAyuda.Error(ex); }
            }
        }
        else
        {
            _contenido.Controls.Clear();
            _contenido.Controls.Add(pantalla);
        }

        foreach (var par in _botones)
        {
            var activo = par.Key == clave;
            par.Value.BackColor = activo ? TemaVisual.BarraActiva : TemaVisual.Barra;
            par.Value.Font = activo ? TemaVisual.FuenteSemibold : TemaVisual.Fuente;
        }

        _claveActual = clave;
    }

    private void AbrirDesdeInicio(string clave)
    {
        if (_botones.TryGetValue(clave, out var boton))
            boton.PerformClick();
    }

    private static List<ItemMenu> MenuSegunRol()
    {
        var items = new List<ItemMenu>
        {
            new("inicio", "Inicio", () => new UcInicio())
        };

        void Agregar(string clave, string texto, Func<UserControl> crear)
        {
            if (PermisosControlador.VerModulo(clave))
                items.Add(new ItemMenu(clave, texto, crear));
        }

        Agregar("clientes", "Clientes", () => new UcClientes());
        Agregar("servicios", "Servicios", () => new UcServicios());
        Agregar("instalaciones", "Instalaciones", () => new UcInstalaciones());
        Agregar("tickets", "Tickets", () => new UcTickets());
        Agregar("agenda", "Agenda", () => new UcAgenda());
        Agregar("routers", "Routers", () => new UcRouters());
        Agregar("remota", "Configuración remota", () => new UcConfiguracionRemota());
        Agregar("conexion", "Estado de la conexión", () => new UcEstadoConexion());
        Agregar("provision", "Aprovisionamiento", () => new UcProvisionamiento());
        Agregar("facturas", "Facturación", () => new UcFacturas());
        Agregar("planes", "Planes", () => new UcPlanes());
        Agregar("usuarios", "Usuarios y roles", () => new UcUsuarios());
        Agregar("auditoria", "Auditoría", () => new UcAuditoria());
        return items;
    }
}
