using AppIsp.Controlador;
using AppIsp.Modelo;

namespace AppIsp.Vista;

/// <summary>Tickets. El tipo queda fijo según el rol, salvo para el maestro.</summary>
public class UcTickets : PantallaBase, IPantallaRecargable
{
    private readonly DataGridView _grilla = InterfazAyuda.Grilla();
    private readonly TextBox _busqueda;
    private readonly ComboBox _filtroEstado = InterfazAyuda.Lista();
    private readonly ComboBox _cliente = InterfazAyuda.Lista();
    private readonly ComboBox _servicio = InterfazAyuda.Lista();
    private readonly ComboBox _tipo = InterfazAyuda.Lista();
    private readonly TextBox _categoria = InterfazAyuda.Caja();
    private readonly ComboBox _prioridad = InterfazAyuda.Lista();
    private readonly ComboBox _estado = InterfazAyuda.Lista();
    private readonly ComboBox _asignado = InterfazAyuda.Lista();
    private readonly TextBox _descripcion = InterfazAyuda.CajaMultilinea();
    private int _id;
    private bool _sincronizando;

    public UcTickets() : base("Tickets", "Solicitudes comerciales o fallas técnicas, según el rol con el que entró.")
    {
        var barra = InterfazAyuda.BarraBusqueda(out _busqueda, (_, _) => Recargar());
        _filtroEstado.Width = 180;
        _filtroEstado.Location = new Point(430, 10);
        barra.Controls.Add(_filtroEstado);
        var estados = new List<string> { "TODOS" };
        estados.AddRange(Catalogos.EstadosTicket);
        InterfazAyuda.CargarValores(_filtroEstado, estados);
        InterfazAyuda.CargarValores(_tipo, Catalogos.TiposTicket);
        InterfazAyuda.CargarValores(_prioridad, Catalogos.Prioridades);
        InterfazAyuda.CargarValores(_estado, Catalogos.EstadosTicket);
        if (!PermisosControlador.EsRolMaestro)
            _tipo.Enabled = false;

        _cliente.SelectedIndexChanged += (_, _) => CargarServicios();

        var ficha = InterfazAyuda.CrearFicha(out var form);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.TituloFicha("Ticket"), 36);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Cliente", _cliente), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Servicio", _servicio), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Tipo", _tipo), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Categoría", _categoria), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Prioridad", _prioridad), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Estado", _estado), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Asignado a", _asignado), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Descripción", _descripcion, 100), 108);

        var nuevo = InterfazAyuda.Boton("Nuevo", EstiloBoton.Secundario);
        var guardar = InterfazAyuda.Boton("Guardar", EstiloBoton.Primario);
        var eliminar = InterfazAyuda.Boton("Eliminar", EstiloBoton.Peligro);
        nuevo.Click += (_, _) => PrepararNuevo();
        guardar.Click += (_, _) => Guardar();
        eliminar.Click += (_, _) => Eliminar();
        InterfazAyuda.AgregarFila(form, InterfazAyuda.FilaBotones(nuevo, guardar, eliminar), 56);

        _grilla.SelectionChanged += (_, _) => AlSeleccionar();
        Cuerpo.Controls.Add(InterfazAyuda.MarcoGrilla(_grilla));
        Cuerpo.Controls.Add(ficha);
        Cuerpo.Controls.Add(barra);
    }

    public void Recargar()
    {
        try
        {
            _sincronizando = true;
            InterfazAyuda.CargarOpciones(_cliente, ClienteControlador.ListarOpciones());
            InterfazAyuda.CargarOpciones(_asignado, UsuarioControlador.ListarOpciones(false));
            _grilla.DataSource = TicketControlador.Leer(_busqueda.Text, InterfazAyuda.ValorSeleccionado(_filtroEstado));
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
        }
        catch (Exception ex) { InterfazAyuda.Error(ex); }
        finally { _sincronizando = false; }
        AlSeleccionar();
    }

    private void CargarServicios()
    {
        if (_sincronizando) return;
        var idCliente = InterfazAyuda.IdSeleccionado(_cliente);
        InterfazAyuda.CargarOpciones(_servicio, ServicioControlador.ListarPorCliente(idCliente));
    }

    private void AlSeleccionar()
    {
        if (_sincronizando) return;
        var id = InterfazAyuda.IdFila(_grilla, "id_ticket");
        if (id == 0) return;
        var ticket = TicketControlador.LeerPorId(id);
        if (ticket == null) return;
        _sincronizando = true;
        _id = ticket.IdTicket;
        InterfazAyuda.SeleccionarId(_cliente, ticket.IdCliente);
        InterfazAyuda.CargarOpciones(_servicio, ServicioControlador.ListarPorCliente(ticket.IdCliente));
        InterfazAyuda.SeleccionarId(_servicio, ticket.IdServicio ?? 0);
        _tipo.SelectedValue = ticket.Tipo;
        _categoria.Text = ticket.Categoria;
        _prioridad.SelectedValue = ticket.Prioridad;
        _estado.SelectedValue = ticket.Estado;
        InterfazAyuda.SeleccionarId(_asignado, ticket.IdAsignado ?? 0);
        _descripcion.Text = ticket.Descripcion;
        _sincronizando = false;
    }

    private void PrepararNuevo()
    {
        _id = 0;
        _categoria.Clear();
        _descripcion.Clear();
        _prioridad.SelectedValue = "MEDIA";
        _estado.SelectedValue = "ABIERTO";
        if (PermisosControlador.EsRolTecnico) _tipo.SelectedValue = "TECNICO";
        else _tipo.SelectedValue = "COMERCIAL";
        _grilla.ClearSelection();
        MostrarEstado("Nuevo ticket. Elija el cliente y describa el caso.");
    }

    private Ticket LeerFormulario()
    {
        return new Ticket
        {
            IdTicket = _id,
            IdCliente = InterfazAyuda.IdSeleccionado(_cliente),
            IdServicio = InterfazAyuda.IdSeleccionado(_servicio),
            Tipo = InterfazAyuda.ValorSeleccionado(_tipo),
            Categoria = _categoria.Text,
            Prioridad = InterfazAyuda.ValorSeleccionado(_prioridad),
            Estado = InterfazAyuda.ValorSeleccionado(_estado),
            IdAsignado = InterfazAyuda.IdSeleccionado(_asignado),
            Descripcion = _descripcion.Text
        };
    }

    private void Guardar()
    {
        var ticket = LeerFormulario();
        Informar(_id == 0 ? TicketControlador.Crear(ticket) : TicketControlador.Actualizar(ticket));
        if (Estado.ForeColor == TemaVisual.Exito) Recargar();
    }

    private void Eliminar()
    {
        if (_id == 0 || !InterfazAyuda.Confirmar("¿Eliminar este ticket?")) return;
        Informar(TicketControlador.Eliminar(_id));
        if (Estado.ForeColor == TemaVisual.Exito) Recargar();
    }
}
