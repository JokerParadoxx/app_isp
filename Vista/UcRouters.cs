using AppIsp.Controlador;
using AppIsp.Modelo;

namespace AppIsp.Vista;

/// <summary>Inventario de CPE. El técnico actualiza. El maestro también crea y elimina.</summary>
public class UcRouters : PantallaBase, IPantallaRecargable
{
    private readonly DataGridView _grilla = InterfazAyuda.Grilla();
    private readonly TextBox _busqueda;
    private readonly TextBox _serie = InterfazAyuda.Caja();
    private readonly TextBox _modelo = InterfazAyuda.Caja();
    private readonly TextBox _mac = InterfazAyuda.Caja();
    private readonly TextBox _ip = InterfazAyuda.Caja();
    private readonly ComboBox _estado = InterfazAyuda.Lista();
    private readonly ComboBox _servicio = InterfazAyuda.Lista();
    private readonly TextBox _ubicacion = InterfazAyuda.Caja();
    private int _id;
    private bool _sincronizando;

    public UcRouters() : base("Routers", "Equipos en almacén o asignados a un servicio. La IP se usa en el aprovisionamiento.")
    {
        var barra = InterfazAyuda.BarraBusqueda(out _busqueda, (_, _) => Recargar());
        InterfazAyuda.CargarValores(_estado, Catalogos.EstadosRouter);
        InterfazAyuda.Pista(_ip, "192.168.1.10");
        var ficha = InterfazAyuda.CrearFicha(out var form);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.TituloFicha("Equipo"), 36);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Serie", _serie), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Modelo", _modelo), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("MAC", _mac), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("IP de gestión", _ip), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Estado", _estado), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Servicio", _servicio), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Ubicación", _ubicacion), 68);

        var botones = new List<Button>();
        if (PermisosControlador.CrearRouters)
        {
            var nuevo = InterfazAyuda.Boton("Nuevo", EstiloBoton.Secundario);
            nuevo.Click += (_, _) => PrepararNuevo();
            botones.Add(nuevo);
        }
        var guardar = InterfazAyuda.Boton("Guardar", EstiloBoton.Primario);
        guardar.Click += (_, _) => Guardar();
        botones.Add(guardar);
        if (PermisosControlador.CrearRouters)
        {
            var eliminar = InterfazAyuda.Boton("Eliminar", EstiloBoton.Peligro);
            eliminar.Click += (_, _) => Eliminar();
            botones.Add(eliminar);
        }
        InterfazAyuda.AgregarFila(form, InterfazAyuda.FilaBotones(botones.ToArray()), 56);
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
            InterfazAyuda.CargarOpciones(_servicio, ServicioControlador.ListarOpciones());
            _grilla.DataSource = RouterControlador.Leer(_busqueda.Text);
            InterfazAyuda.PrepararGrilla(_grilla,
                ("id_router", "Id", true),
                ("serie", "Serie", true),
                ("modelo", "Modelo", true),
                ("mac_address", "MAC", true),
                ("ip_gestion", "IP", true),
                ("estado", "Estado", true),
                ("cliente", "Cliente", true),
                ("ubicacion", "Ubicación", true));
        }
        catch (Exception ex) { InterfazAyuda.Error(ex); }
        finally { _sincronizando = false; }
        AlSeleccionar();
    }

    private void AlSeleccionar()
    {
        if (_sincronizando) return;
        var id = InterfazAyuda.IdFila(_grilla, "id_router");
        if (id == 0) return;
        var router = RouterControlador.LeerPorId(id);
        if (router == null) return;
        _id = router.IdRouter;
        _serie.Text = router.Serie;
        _modelo.Text = router.Modelo;
        _mac.Text = router.MacAddress;
        _ip.Text = router.IpGestion ?? "";
        _estado.SelectedValue = router.Estado;
        InterfazAyuda.SeleccionarId(_servicio, router.IdServicio ?? 0);
        _ubicacion.Text = router.Ubicacion ?? "";
    }

    private void PrepararNuevo()
    {
        _id = 0;
        _serie.Clear();
        _modelo.Clear();
        _mac.Clear();
        _ip.Clear();
        _ubicacion.Clear();
        _estado.SelectedValue = "ALMACEN";
        _grilla.ClearSelection();
        MostrarEstado("Complete serie, modelo y MAC, luego pulse Guardar.");
    }

    private Router LeerFormulario()
    {
        var servicio = InterfazAyuda.IdSeleccionado(_servicio);
        return new Router
        {
            IdRouter = _id,
            Serie = _serie.Text,
            Modelo = _modelo.Text,
            MacAddress = _mac.Text,
            IpGestion = _ip.Text,
            Estado = InterfazAyuda.ValorSeleccionado(_estado),
            IdServicio = servicio == 0 ? null : servicio,
            Ubicacion = _ubicacion.Text
        };
    }

    private void Guardar()
    {
        if (_id == 0 && !PermisosControlador.CrearRouters)
        {
            MostrarEstado("Seleccione un router para actualizarlo.", true);
            return;
        }
        var router = LeerFormulario();
        Informar(_id == 0 ? RouterControlador.Crear(router) : RouterControlador.Actualizar(router));
        if (Estado.ForeColor == TemaVisual.Exito) Recargar();
    }

    private void Eliminar()
    {
        if (_id == 0 || !InterfazAyuda.Confirmar("¿Eliminar este router?")) return;
        Informar(RouterControlador.Eliminar(_id));
        if (Estado.ForeColor == TemaVisual.Exito) Recargar();
    }
}
