using System.Runtime.InteropServices;
using AppIsp.Modelo;

namespace AppIsp.Vista;

/// <summary>Colores y fuentes compartidos. Cambiarlos aquí cambia toda la aplicación.</summary>
public static class TemaVisual
{
    public static readonly Color Fondo = Color.FromArgb(243, 246, 250);
    public static readonly Color Barra = Color.FromArgb(14, 39, 68);
    public static readonly Color BarraHover = Color.FromArgb(23, 55, 95);
    public static readonly Color BarraActiva = Color.FromArgb(31, 92, 158);
    public static readonly Color Acento = Color.FromArgb(27, 108, 168);
    public static readonly Color Texto = Color.FromArgb(28, 40, 52);
    public static readonly Color TextoSuave = Color.FromArgb(92, 107, 122);
    public static readonly Color Borde = Color.FromArgb(213, 222, 232);
    public static readonly Color Peligro = Color.FromArgb(180, 35, 24);
    public static readonly Color Exito = Color.FromArgb(6, 118, 71);
    public static readonly Color Advertencia = Color.FromArgb(181, 71, 8);
    public static readonly Color Blanco = Color.White;

    public static readonly Font FuenteTitulo = new("Segoe UI Semibold", 20F, FontStyle.Regular, GraphicsUnit.Point);
    public static readonly Font Fuente = new("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
    public static readonly Font FuenteSemibold = new("Segoe UI Semibold", 10F, FontStyle.Regular, GraphicsUnit.Point);
    public static readonly Font FuentePequena = new("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

    public static Color ColorEstado(string? estado)
    {
        return (estado ?? "").ToUpperInvariant() switch
        {
            "ACTIVO" or "PAGADA" or "RESUELTO" or "COMPLETADA" or "CERRADO" or "ASIGNADO" => Exito,
            "BAJA" or "ANULADA" or "CANCELADA" or "CANCELADO" or "CRITICA" or "RETIRADO" or "EN_FALLA" => Peligro,
            "SUSPENDIDO" or "VENCIDA" or "ALTA" or "PENDIENTE" or "ABIERTO" or "MEDIA" or "EN_PROCESO" => Advertencia,
            _ => Texto
        };
    }
}

public enum EstiloBoton
{
    Primario,
    Secundario,
    Peligro
}

/// <summary>Arma controles con el mismo aspecto para no repetir el diseño en cada pantalla.</summary>
public static class InterfazAyuda
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

    public static TextBox Caja()
    {
        return new TextBox
        {
            BorderStyle = BorderStyle.FixedSingle,
            Font = TemaVisual.Fuente,
            Height = 28
        };
    }

    public static TextBox CajaMultilinea()
    {
        return new TextBox
        {
            BorderStyle = BorderStyle.FixedSingle,
            Font = TemaVisual.Fuente,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Height = 72
        };
    }

    public static ComboBox Lista()
    {
        return new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            Font = TemaVisual.Fuente
        };
    }

    public static NumericUpDown Numero(decimal maximo, int decimales)
    {
        return new NumericUpDown
        {
            Font = TemaVisual.Fuente,
            Maximum = maximo,
            DecimalPlaces = decimales,
            ThousandsSeparator = true,
            BorderStyle = BorderStyle.FixedSingle,
            Width = 160
        };
    }

    public static MonthCalendar Calendario()
    {
        var calendario = new MonthCalendar
        {
            MaxSelectionCount = 1,
            ShowToday = true,
            ShowTodayCircle = true,
            MinDate = DateTime.Today,
            MaxDate = DateTime.Today.AddYears(3),
            ScrollChange = 1
        };
        calendario.SetDate(DateTime.Today);
        return calendario;
    }

    public static DateTimePicker Hora()
    {
        return new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "HH:mm",
            ShowUpDown = true,
            Font = TemaVisual.Fuente,
            Value = DateTime.Today.AddHours(9)
        };
    }

    public static DateTimePicker Fecha(bool conHora = false)
    {
        return new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = conHora ? "dd-MM-yyyy HH:mm" : "dd-MM-yyyy",
            Font = TemaVisual.Fuente
        };
    }

    public static DateTimePicker FechaOpcional(bool conHora = false)
    {
        var fecha = Fecha(conHora);
        fecha.ShowCheckBox = true;
        fecha.Checked = false;
        return fecha;
    }

    public static CheckBox Casilla(string texto)
    {
        return new CheckBox
        {
            Text = texto,
            AutoSize = true,
            Font = TemaVisual.Fuente,
            ForeColor = TemaVisual.Texto,
            Padding = new Padding(0, 8, 0, 0)
        };
    }

    public static Button Boton(string texto, EstiloBoton estilo)
    {
        var boton = new Button
        {
            Text = texto,
            AutoSize = true,
            MinimumSize = new Size(120, 36),
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            Font = TemaVisual.FuenteSemibold,
            Cursor = Cursors.Hand,
            Padding = new Padding(10, 0, 10, 0)
        };
        boton.FlatAppearance.BorderSize = 0;

        var fondo = estilo switch
        {
            EstiloBoton.Peligro => TemaVisual.Peligro,
            EstiloBoton.Secundario => Color.FromArgb(226, 232, 240),
            _ => TemaVisual.Acento
        };
        boton.BackColor = fondo;
        boton.ForeColor = estilo == EstiloBoton.Secundario ? TemaVisual.Texto : Color.White;
        boton.MouseEnter += (_, _) => boton.BackColor = ControlPaint.Light(fondo, 0.12f);
        boton.MouseLeave += (_, _) => boton.BackColor = fondo;
        return boton;
    }

    public static DataGridView Grilla()
    {
        var grilla = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeight = 40,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            RowTemplate = { Height = 34 },
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            GridColor = TemaVisual.Borde,
            Font = TemaVisual.Fuente
        };

        grilla.ColumnHeadersDefaultCellStyle.BackColor = TemaVisual.Barra;
        grilla.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        grilla.ColumnHeadersDefaultCellStyle.Font = TemaVisual.FuenteSemibold;
        grilla.ColumnHeadersDefaultCellStyle.SelectionBackColor = TemaVisual.Barra;
        grilla.DefaultCellStyle.BackColor = Color.White;
        grilla.DefaultCellStyle.ForeColor = TemaVisual.Texto;
        grilla.DefaultCellStyle.SelectionBackColor = Color.FromArgb(217, 232, 246);
        grilla.DefaultCellStyle.SelectionForeColor = TemaVisual.Texto;
        grilla.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
        grilla.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(247, 250, 252);
        grilla.AdvancedColumnHeadersBorderStyle.All = DataGridViewAdvancedCellBorderStyle.None;

        grilla.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.Value is not string texto)
                return;

            var columna = grilla.Columns[e.ColumnIndex].Name;
            if (e.CellStyle != null && columna is "estado" or "prioridad" or "tipo" or "tipo_cambio" or "accion")
            {
                var color = TemaVisual.ColorEstado(texto);
                e.CellStyle.ForeColor = color;
                e.CellStyle.SelectionForeColor = color;
            }

            var amigable = Catalogos.TextoAmigable(texto);
            if (amigable != texto)
            {
                e.Value = amigable;
                e.FormattingApplied = true;
            }
        };

        return grilla;
    }

    public static void PrepararGrilla(DataGridView grilla, params (string campo, string titulo, bool visible)[] columnas)
    {
        foreach (var (campo, titulo, visible) in columnas)
        {
            if (!grilla.Columns.Contains(campo))
                continue;
            grilla.Columns[campo].HeaderText = titulo;
            grilla.Columns[campo].Visible = visible;
            if (campo is "precio_mensual" or "monto_total" or "precio_unitario" or "subtotal")
                grilla.Columns[campo].DefaultCellStyle.Format = "N0";
        }
    }

    public static Panel Bloque(string etiqueta, Control entrada, int alto = 64)
    {
        var panel = new Panel { Height = alto, Margin = new Padding(0, 0, 0, 4), BackColor = Color.White };
        var lbl = new Label
        {
            Text = etiqueta,
            Dock = DockStyle.Top,
            Height = 18,
            ForeColor = TemaVisual.TextoSuave,
            Font = TemaVisual.FuentePequena
        };
        entrada.Location = new Point(0, 20);
        entrada.Width = 360;
        entrada.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
        panel.Controls.Add(entrada);
        panel.Controls.Add(lbl);
        return panel;
    }

    public static Label TituloFicha(string texto)
    {
        return new Label
        {
            Text = texto,
            Font = TemaVisual.FuenteSemibold,
            ForeColor = TemaVisual.Texto,
            TextAlign = ContentAlignment.BottomLeft,
            Dock = DockStyle.Fill
        };
    }

    public static TableLayoutPanel ColumnaFormulario()
    {
        var tabla = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            AutoScroll = true,
            BackColor = Color.White,
            Padding = new Padding(16, 8, 16, 12)
        };
        tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        return tabla;
    }

    public static void AgregarFila(TableLayoutPanel tabla, Control control, int alto)
    {
        var fila = tabla.RowCount++;
        tabla.RowStyles.Add(new RowStyle(SizeType.Absolute, alto));
        control.Dock = DockStyle.Fill;
        control.Margin = new Padding(0, 2, 8, 2);
        tabla.Controls.Add(control, 0, fila);
    }

    public static FlowLayoutPanel FilaBotones(params Button[] botones)
    {
        var flujo = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Dock = DockStyle.Fill,
            BackColor = Color.White
        };
        foreach (var boton in botones)
        {
            boton.Margin = new Padding(0, 6, 8, 0);
            flujo.Controls.Add(boton);
        }
        return flujo;
    }

    public static Panel CrearFicha(out TableLayoutPanel formulario)
    {
        var borde = new Panel
        {
            Dock = DockStyle.Right,
            Width = 420,
            BackColor = TemaVisual.Borde,
            Padding = new Padding(1, 0, 0, 0)
        };
        var interior = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        formulario = ColumnaFormulario();
        interior.Controls.Add(formulario);
        borde.Controls.Add(interior);
        return borde;
    }

    public static Control MarcoGrilla(DataGridView grilla)
    {
        var marco = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 0, 12, 0),
            BackColor = TemaVisual.Fondo
        };
        var tarjeta = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = TemaVisual.Borde,
            Padding = new Padding(1)
        };
        grilla.Dock = DockStyle.Fill;
        tarjeta.Controls.Add(grilla);
        marco.Controls.Add(tarjeta);
        return marco;
    }

    public static Panel BarraAcciones(params Button[] botones)
    {
        var barra = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 48,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = TemaVisual.Fondo,
            Padding = new Padding(0, 4, 0, 0)
        };
        foreach (var boton in botones)
        {
            boton.Margin = new Padding(0, 0, 8, 0);
            barra.Controls.Add(boton);
        }
        return barra;
    }

    public static Panel BarraBusqueda(out TextBox caja, EventHandler alBuscar)
    {
        var barra = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = TemaVisual.Fondo };
        var busqueda = Caja();
        caja = busqueda;
        busqueda.Width = 280;
        busqueda.Location = new Point(0, 8);
        Pista(busqueda, "Buscar...");
        var boton = Boton("Refrescar", EstiloBoton.Secundario);
        boton.Location = new Point(292, 6);
        boton.Click += alBuscar;
        busqueda.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            alBuscar(busqueda, EventArgs.Empty);
        };
        barra.Controls.Add(boton);
        barra.Controls.Add(busqueda);
        return barra;
    }

    public static void Pista(TextBox caja, string texto)
    {
        void Aplicar() => SendMessage(caja.Handle, 0x1501, (IntPtr)1, texto);
        if (caja.IsHandleCreated) Aplicar();
        else caja.HandleCreated += (_, _) => Aplicar();
    }

    public static void CargarOpciones(ComboBox combo, List<OpcionCombo> opciones)
    {
        combo.DataSource = null;
        combo.DisplayMember = nameof(OpcionCombo.Texto);
        combo.ValueMember = nameof(OpcionCombo.Id);
        combo.DataSource = opciones;
    }

    public static void CargarValores(ComboBox combo, IEnumerable<string> valores)
    {
        var lista = valores.Select(valor => new OpcionTexto
        {
            Valor = valor,
            Texto = Catalogos.TextoAmigable(valor)
        }).ToList();
        combo.DataSource = null;
        combo.DisplayMember = nameof(OpcionTexto.Texto);
        combo.ValueMember = nameof(OpcionTexto.Valor);
        combo.DataSource = lista;
    }

    public static int IdSeleccionado(ComboBox combo)
    {
        if (combo.SelectedValue == null || combo.SelectedValue == DBNull.Value)
            return 0;
        return Convert.ToInt32(combo.SelectedValue);
    }

    public static string ValorSeleccionado(ComboBox combo)
    {
        return Convert.ToString(combo.SelectedValue) ?? "";
    }

    public static void SeleccionarId(ComboBox combo, int id)
    {
        combo.SelectedValue = id;
    }

    public static int IdFila(DataGridView grilla, string columna)
    {
        if (grilla.CurrentRow == null || !grilla.Columns.Contains(columna))
            return 0;
        var valor = grilla.CurrentRow.Cells[columna].Value;
        if (valor == null || valor == DBNull.Value)
            return 0;
        return Convert.ToInt32(valor);
    }

    public static void Error(Exception ex)
    {
        MessageBox.Show(ConexionMySql.MensajeAmigable(ex), "ISP PLC", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    public static void ErrorTexto(string mensaje)
    {
        MessageBox.Show(mensaje, "No se pudo completar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    public static bool Confirmar(string mensaje)
    {
        return MessageBox.Show(mensaje, "ISP PLC", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
    }
}
