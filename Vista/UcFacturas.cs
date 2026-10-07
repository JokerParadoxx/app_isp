using AppIsp.Controlador;
using AppIsp.Modelo;

namespace AppIsp.Vista;

/// <summary>Facturación del maestro. El total sale de la suma de las líneas.</summary>
public class UcFacturas : PantallaBase, IPantallaRecargable
{
    private readonly DataGridView _facturas = InterfazAyuda.Grilla();
    private readonly DataGridView _detalle = InterfazAyuda.Grilla();
    private readonly TextBox _busqueda;
    private readonly ComboBox _cliente = InterfazAyuda.Lista();
    private readonly TextBox _periodo = InterfazAyuda.Caja();
    private readonly DateTimePicker _emision = InterfazAyuda.Fecha();
    private readonly DateTimePicker _vencimiento = InterfazAyuda.Fecha();
    private readonly ComboBox _estado = InterfazAyuda.Lista();
    private readonly ComboBox _servicio = InterfazAyuda.Lista();
    private readonly TextBox _descripcion = InterfazAyuda.Caja();
    private readonly NumericUpDown _cantidad = InterfazAyuda.Numero(999, 0);
    private readonly NumericUpDown _precio = InterfazAyuda.Numero(100000000, 0);
    private int _id;
    private bool _sincronizando;

    public UcFacturas() : base("Facturación", "Cree la cabecera, agregue líneas y el monto se calcula solo.")
    {
        var barra = InterfazAyuda.BarraBusqueda(out _busqueda, (_, _) => Recargar());
        InterfazAyuda.CargarValores(_estado, Catalogos.EstadosFactura);
        InterfazAyuda.Pista(_periodo, "2026-09");
        _cantidad.Value = 1;
        _cliente.SelectedIndexChanged += (_, _) =>
        {
            if (_sincronizando) return;
            InterfazAyuda.CargarOpciones(_servicio, ServicioControlador.ListarPorCliente(InterfazAyuda.IdSeleccionado(_cliente)));
        };
        _servicio.SelectedIndexChanged += (_, _) => SugerirLinea();

        var ficha = InterfazAyuda.CrearFicha(out var form);
        ficha.Width = 460;
        InterfazAyuda.AgregarFila(form, InterfazAyuda.TituloFicha("Cabecera"), 32);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Cliente", _cliente), 64);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Período AAAA-MM", _periodo), 64);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Emisión", _emision), 64);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Vencimiento", _vencimiento), 64);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Estado", _estado), 64);

        var crear = InterfazAyuda.Boton("Crear factura", EstiloBoton.Primario);
        var guardar = InterfazAyuda.Boton("Guardar estado", EstiloBoton.Secundario);
        var eliminar = InterfazAyuda.Boton("Eliminar", EstiloBoton.Peligro);
        crear.Click += (_, _) => Crear();
        guardar.Click += (_, _) => GuardarEstado();
        eliminar.Click += (_, _) => Eliminar();
        InterfazAyuda.AgregarFila(form, InterfazAyuda.FilaBotones(crear, guardar, eliminar), 52);

        InterfazAyuda.AgregarFila(form, InterfazAyuda.TituloFicha("Línea de detalle"), 32);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Servicio (opcional)", _servicio), 64);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Descripción", _descripcion), 64);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Cantidad", _cantidad), 64);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Precio unitario", _precio), 64);
        var agregar = InterfazAyuda.Boton("Agregar línea", EstiloBoton.Primario);
        var quitar = InterfazAyuda.Boton("Quitar línea", EstiloBoton.Peligro);
        agregar.Click += (_, _) => AgregarLinea();
        quitar.Click += (_, _) => QuitarLinea();
        InterfazAyuda.AgregarFila(form, InterfazAyuda.FilaBotones(agregar, quitar), 52);

        var zonaDetalle = new Panel { Dock = DockStyle.Bottom, Height = 160, Padding = new Padding(0, 8, 12, 0), BackColor = TemaVisual.Fondo };
        var tarjeta = new Panel { Dock = DockStyle.Fill, BackColor = TemaVisual.Borde, Padding = new Padding(1) };
        tarjeta.Controls.Add(_detalle);
        zonaDetalle.Controls.Add(tarjeta);

        _facturas.SelectionChanged += (_, _) => AlElegirFactura();
        Cuerpo.Controls.Add(InterfazAyuda.MarcoGrilla(_facturas));
        Cuerpo.Controls.Add(zonaDetalle);
        Cuerpo.Controls.Add(ficha);
        Cuerpo.Controls.Add(barra);
        _vencimiento.Value = DateTime.Today.AddDays(10);
    }

    public void Recargar()
    {
        try
        {
            _sincronizando = true;
            InterfazAyuda.CargarOpciones(_cliente, ClienteControlador.ListarOpciones());
            _facturas.DataSource = FacturaControlador.Leer(_busqueda.Text);
            InterfazAyuda.PrepararGrilla(_facturas,
                ("id_factura", "N°", true),
                ("cliente", "Cliente", true),
                ("periodo", "Período", true),
                ("fecha_emision", "Emisión", true),
                ("fecha_venc", "Vence", true),
                ("monto_total", "Total", true),
                ("estado", "Estado", true));
        }
        catch (Exception ex) { InterfazAyuda.Error(ex); }
        finally { _sincronizando = false; }
        AlElegirFactura();
    }

    private void AlElegirFactura()
    {
        if (_sincronizando) return;
        _id = InterfazAyuda.IdFila(_facturas, "id_factura");
        if (_id == 0)
        {
            _detalle.DataSource = null;
            return;
        }
        var factura = FacturaControlador.LeerPorId(_id);
        if (factura == null) return;
        _sincronizando = true;
        InterfazAyuda.SeleccionarId(_cliente, factura.IdCliente);
        InterfazAyuda.CargarOpciones(_servicio, ServicioControlador.ListarPorCliente(factura.IdCliente));
        _periodo.Text = factura.Periodo;
        _emision.Value = factura.FechaEmision;
        _vencimiento.Value = factura.FechaVencimiento;
        _estado.SelectedValue = factura.Estado;
        _detalle.DataSource = FacturaControlador.LeerDetalle(_id);
        InterfazAyuda.PrepararGrilla(_detalle,
            ("id_detalle", "Id", true),
            ("descripcion", "Descripción", true),
            ("cantidad", "Cant.", true),
            ("precio_unitario", "Precio", true),
            ("subtotal", "Subtotal", true));
        _sincronizando = false;
    }

    private void SugerirLinea()
    {
        if (_sincronizando) return;
        var idServicio = InterfazAyuda.IdSeleccionado(_servicio);
        if (idServicio == 0) return;
        var sugerencia = FacturaControlador.SugerirDesdeServicio(idServicio);
        if (sugerencia == null) return;
        _descripcion.Text = sugerencia.Descripcion;
        _precio.Value = Math.Min(_precio.Maximum, sugerencia.PrecioUnitario);
        _cantidad.Value = 1;
    }

    private void Crear()
    {
        Informar(FacturaControlador.Crear(new Factura
        {
            IdCliente = InterfazAyuda.IdSeleccionado(_cliente),
            Periodo = _periodo.Text,
            FechaEmision = _emision.Value,
            FechaVencimiento = _vencimiento.Value
        }));
        if (Estado.ForeColor == TemaVisual.Exito) Recargar();
    }

    private void GuardarEstado()
    {
        if (_id == 0) { MostrarEstado("Seleccione una factura.", true); return; }
        Informar(FacturaControlador.Actualizar(new Factura
        {
            IdFactura = _id,
            FechaEmision = _emision.Value,
            FechaVencimiento = _vencimiento.Value,
            Estado = InterfazAyuda.ValorSeleccionado(_estado)
        }));
        if (Estado.ForeColor == TemaVisual.Exito) Recargar();
    }

    private void Eliminar()
    {
        if (_id == 0 || !InterfazAyuda.Confirmar("¿Eliminar la factura y su detalle?")) return;
        Informar(FacturaControlador.Eliminar(_id));
        if (Estado.ForeColor == TemaVisual.Exito) Recargar();
    }

    private void AgregarLinea()
    {
        if (_id == 0) { MostrarEstado("Primero cree o elija una factura.", true); return; }
        Informar(FacturaControlador.AgregarDetalle(new FacturaDetalle
        {
            IdFactura = _id,
            IdServicio = InterfazAyuda.IdSeleccionado(_servicio),
            Descripcion = _descripcion.Text,
            Cantidad = (int)_cantidad.Value,
            PrecioUnitario = _precio.Value
        }));
        if (Estado.ForeColor == TemaVisual.Exito) Recargar();
    }

    private void QuitarLinea()
    {
        var idDetalle = InterfazAyuda.IdFila(_detalle, "id_detalle");
        if (idDetalle == 0) { MostrarEstado("Seleccione una línea.", true); return; }
        Informar(FacturaControlador.QuitarDetalle(idDetalle));
        if (Estado.ForeColor == TemaVisual.Exito) Recargar();
    }
}
