using AppIsp.Controlador;
using AppIsp.Modelo;

namespace AppIsp.Vista;

/// <summary>
/// Alta, baja y cambio de plan. El técnico ve la grilla, pero los botones no aparecen.
/// </summary>
public class UcServicios : PantallaBase, IPantallaRecargable
{
    private readonly DataGridView _grilla = InterfazAyuda.Grilla();
    private readonly TextBox _busqueda;
    private readonly ComboBox _cliente = InterfazAyuda.Lista();
    private readonly ComboBox _plan = InterfazAyuda.Lista();
    private readonly TextBox _comentario = InterfazAyuda.Caja();
    private int _id;
    private bool _sincronizando;

    public UcServicios() : base("Servicios", "Contratar un plan, cambiarlo o darlo de baja. Cada acción queda en el log comercial.")
    {
        var barra = InterfazAyuda.BarraBusqueda(out _busqueda, (_, _) => Recargar());
        var ficha = InterfazAyuda.CrearFicha(out var form);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.TituloFicha("Acción comercial"), 36);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Cliente", _cliente), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Plan", _plan), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Comentario", _comentario), 68);

        if (PermisosControlador.AltaServicio)
        {
            var alta = InterfazAyuda.Boton("Dar de alta", EstiloBoton.Primario);
            var cambio = InterfazAyuda.Boton("Cambiar plan", EstiloBoton.Secundario);
            var baja = InterfazAyuda.Boton("Dar de baja", EstiloBoton.Peligro);
            alta.Click += (_, _) => Alta();
            cambio.Click += (_, _) => Cambio();
            baja.Click += (_, _) => Baja();
            var botones = new List<Button> { alta, cambio, baja };
            if (PermisosControlador.EsRolMaestro)
            {
                var suspender = InterfazAyuda.Boton("Suspender", EstiloBoton.Secundario);
                var reactivar = InterfazAyuda.Boton("Reactivar", EstiloBoton.Secundario);
                var eliminar = InterfazAyuda.Boton("Eliminar", EstiloBoton.Peligro);
                suspender.Click += (_, _) => Accion(ServicioControlador.Suspender(_id));
                reactivar.Click += (_, _) => Accion(ServicioControlador.Reactivar(_id));
                eliminar.Click += (_, _) =>
                {
                    if (_id == 0 || !InterfazAyuda.Confirmar("¿Eliminar el servicio de la base?")) return;
                    Accion(ServicioControlador.Eliminar(_id));
                };
                botones.AddRange(new[] { suspender, reactivar, eliminar });
            }
            InterfazAyuda.AgregarFila(form, InterfazAyuda.FilaBotones(botones.ToArray()), 120);
        }
        else
        {
            _cliente.Enabled = false;
            _plan.Enabled = false;
            _comentario.ReadOnly = true;
            InterfazAyuda.AgregarFila(form, new Label
            {
                Text = "Su rol solo consulta servicios.",
                ForeColor = TemaVisual.TextoSuave,
                Dock = DockStyle.Fill
            }, 40);
        }

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
            InterfazAyuda.CargarOpciones(_plan, PlanControlador.ListarOpciones(true));
            _grilla.DataSource = ServicioControlador.Leer(_busqueda.Text);
            InterfazAyuda.PrepararGrilla(_grilla,
                ("id_servicio", "Id", true),
                ("cliente", "Cliente", true),
                ("plan", "Plan", true),
                ("velocidad_mbps", "Mbps", true),
                ("estado", "Estado", true),
                ("fecha_alta", "Alta", true),
                ("fecha_baja", "Baja", true),
                ("comentario", "Comentario", true));
        }
        catch (Exception ex) { InterfazAyuda.Error(ex); }
        finally { _sincronizando = false; }
        AlSeleccionar();
    }

    private void AlSeleccionar()
    {
        if (_sincronizando) return;
        var id = InterfazAyuda.IdFila(_grilla, "id_servicio");
        if (id == 0) return;
        var servicio = ServicioControlador.LeerPorId(id);
        if (servicio == null) return;
        _id = servicio.IdServicio;
        InterfazAyuda.SeleccionarId(_cliente, servicio.IdCliente);
        InterfazAyuda.SeleccionarId(_plan, servicio.IdPlan);
        _comentario.Text = servicio.Comentario ?? "";
    }

    private void Alta()
    {
        Informar(ServicioControlador.Crear(new Servicio
        {
            IdCliente = InterfazAyuda.IdSeleccionado(_cliente),
            IdPlan = InterfazAyuda.IdSeleccionado(_plan),
            Comentario = _comentario.Text
        }));
        if (Estado.ForeColor == TemaVisual.Exito) Recargar();
    }

    private void Cambio()
    {
        if (_id == 0) { MostrarEstado("Seleccione un servicio de la lista.", true); return; }
        Informar(ServicioControlador.Actualizar(new Servicio
        {
            IdServicio = _id,
            IdPlan = InterfazAyuda.IdSeleccionado(_plan),
            Comentario = _comentario.Text
        }));
        if (Estado.ForeColor == TemaVisual.Exito) Recargar();
    }

    private void Baja()
    {
        if (_id == 0) { MostrarEstado("Seleccione un servicio de la lista.", true); return; }
        if (!InterfazAyuda.Confirmar("¿Dar de baja este servicio?")) return;
        Accion(ServicioControlador.DarDeBaja(_id, _comentario.Text));
    }

    private void Accion(Respuesta respuesta)
    {
        Informar(respuesta);
        if (respuesta.Exito) Recargar();
    }
}
