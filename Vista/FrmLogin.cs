using AppIsp.Controlador;
using AppIsp.Modelo;

namespace AppIsp.Vista;

/// <summary>
/// Pantalla de acceso. No cierra sola al pulsar Ingresar: solo vuelve OK
/// cuando el controlador acepta el correo y la contraseña.
/// </summary>
public class FrmLogin : Form
{
    private readonly TextBox _email = InterfazAyuda.Caja();
    private readonly TextBox _clave = InterfazAyuda.Caja();
    private readonly Label _aviso = new();
    private readonly Button _ingresar;

    public FrmLogin()
    {
        Text = "ISP PLC — Ingreso";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        ClientSize = new Size(980, 620);
        BackColor = Color.White;
        Font = TemaVisual.Fuente;

        var izquierda = new Panel { Dock = DockStyle.Left, Width = 420, BackColor = TemaVisual.Barra };
        izquierda.Controls.Add(Texto("MySQL  ·  isp_db", 16, 540, TemaVisual.FuentePequena, Color.FromArgb(170, 196, 220)));
        izquierda.Controls.Add(Texto("• Routers, configuración remota y conexión", 48, 300, TemaVisual.Fuente, Color.FromArgb(214, 228, 240)));
        izquierda.Controls.Add(Texto("• Instalaciones y agenda", 48, 268, TemaVisual.Fuente, Color.FromArgb(214, 228, 240)));
        izquierda.Controls.Add(Texto("• Clientes, planes y facturas", 48, 236, TemaVisual.Fuente, Color.FromArgb(214, 228, 240)));
        izquierda.Controls.Add(Texto("Un escritorio para el equipo comercial,\nel técnico y el administrador.", 48, 150, TemaVisual.Fuente, Color.FromArgb(214, 228, 240), 80));
        izquierda.Controls.Add(Texto("Gestión de la operación", 48, 118, TemaVisual.Fuente, Color.FromArgb(186, 214, 236)));
        izquierda.Controls.Add(new MarcaIsp
        {
            Location = new Point(48, 48),
            Size = new Size(340, 64)
        });

        var caja = new Panel { Width = 420, Height = 440, BackColor = Color.White };
        var credito = new Label
        {
            Text = "Aplicación creada por Héctor Martínez" + Environment.NewLine + "Derechos de copyright. Derechos reservados.",
            Dock = DockStyle.Bottom,
            Height = 52,
            ForeColor = TemaVisual.TextoSuave,
            Font = TemaVisual.FuentePequena,
            TextAlign = ContentAlignment.MiddleCenter
        };
        var derecha = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        derecha.Controls.Add(caja);
        derecha.Controls.Add(credito);
        derecha.Resize += (_, _) =>
        {
            caja.Left = Math.Max(24, (derecha.ClientSize.Width - caja.Width) / 2);
            caja.Top = Math.Max(24, (derecha.ClientSize.Height - caja.Height) / 2);
        };

        var titulo = new Label
        {
            Text = "Iniciar sesión",
            Font = TemaVisual.FuenteTitulo,
            ForeColor = TemaVisual.Texto,
            Location = new Point(8, 24),
            AutoSize = true
        };
        var sub = new Label
        {
            Text = "Use el correo y la contraseña de su usuario.",
            ForeColor = TemaVisual.TextoSuave,
            Location = new Point(10, 68),
            AutoSize = true
        };

        var lblCorreo = Etiqueta("Correo", 110);
        _email.Location = new Point(8, 132);
        _email.Width = 400;
        InterfazAyuda.Pista(_email, "correo@dominio.com");

        var lblClave = Etiqueta("Contraseña", 176);
        _clave.Location = new Point(8, 198);
        _clave.Width = 400;
        _clave.UseSystemPasswordChar = true;

        var mostrar = new CheckBox
        {
            Text = "Mostrar contraseña",
            Location = new Point(8, 236),
            AutoSize = true,
            ForeColor = TemaVisual.TextoSuave
        };
        mostrar.CheckedChanged += (_, _) => _clave.UseSystemPasswordChar = !mostrar.Checked;

        _ingresar = InterfazAyuda.Boton("Ingresar", EstiloBoton.Primario);
        _ingresar.SetBounds(8, 280, 400, 46);
        _ingresar.Click += (_, _) => Entrar();

        var salir = InterfazAyuda.Boton("Salir", EstiloBoton.Secundario);
        salir.Location = new Point(8, 336);
        salir.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        var probar = new LinkLabel
        {
            Text = "Probar conexión",
            Location = new Point(150, 344),
            AutoSize = true,
            LinkColor = TemaVisual.Acento
        };
        probar.Click += (_, _) => Probar();

        _aviso.SetBounds(8, 388, 400, 48);
        _aviso.ForeColor = TemaVisual.TextoSuave;

        caja.Controls.AddRange(new Control[] { titulo, sub, lblCorreo, _email, lblClave, _clave, mostrar, _ingresar, salir, probar, _aviso });
        Controls.Add(derecha);
        Controls.Add(izquierda);
        AcceptButton = _ingresar;
        CancelButton = salir;
    }

    private static Label Etiqueta(string texto, int y)
    {
        return new Label
        {
            Text = texto,
            Location = new Point(8, y),
            AutoSize = true,
            ForeColor = TemaVisual.TextoSuave,
            Font = TemaVisual.FuentePequena
        };
    }

    /// <summary>
    /// Dibuja la marca letra por letra. Segoe UI Semibold en WinForms
    /// encoge el avance y deja ISP PLC pegado.
    /// </summary>
    private sealed class MarcaIsp : Control
    {
        public MarcaIsp()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Font = new Font("Segoe UI", 28F, FontStyle.Bold, GraphicsUnit.Point);
            ForeColor = Color.White;
            BackColor = TemaVisual.Barra;
            Text = "ISP PLC";
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            const int separacion = 3;
            var x = 0;
            foreach (var letra in Text)
            {
                var fragmento = letra.ToString();
                var medida = TextRenderer.MeasureText(e.Graphics, fragmento, Font, Size.Empty, TextFormatFlags.NoPadding);
                TextRenderer.DrawText(e.Graphics, fragmento, Font, new Point(x, 8), ForeColor, TextFormatFlags.NoPadding);
                x += medida.Width + (letra == ' ' ? 14 : separacion);
            }
        }
    }

    private static Label Texto(string texto, int x, int y, Font fuente, Color color, int alto = 28)
    {
        return new Label
        {
            Text = texto,
            Location = new Point(x, y),
            Size = new Size(340, alto),
            Font = fuente,
            ForeColor = color
        };
    }

    private void Entrar()
    {
        _ingresar.Enabled = false;
        UseWaitCursor = true;
        try
        {
            var preparacion = AutenticacionControlador.PrepararPrimerAcceso();
            if (!preparacion.Exito)
            {
                _aviso.ForeColor = TemaVisual.Peligro;
                _aviso.Text = preparacion.Mensaje;
                return;
            }

            var ingreso = AutenticacionControlador.Ingresar(_email.Text, _clave.Text);
            if (!ingreso.Exito)
            {
                _aviso.ForeColor = TemaVisual.Peligro;
                _aviso.Text = ingreso.Mensaje;
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        finally
        {
            _ingresar.Enabled = true;
            UseWaitCursor = false;
        }
    }

    private void Probar()
    {
        var resultado = AutenticacionControlador.PrepararPrimerAcceso();
        _aviso.ForeColor = resultado.Exito ? TemaVisual.Exito : TemaVisual.Peligro;
        _aviso.Text = resultado.Mensaje;
    }
}
