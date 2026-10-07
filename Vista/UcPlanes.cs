using AppIsp.Controlador;
using AppIsp.Modelo;

namespace AppIsp.Vista;

/// <summary>Catálogo de planes. Solo lo abre el maestro.</summary>
public class UcPlanes : PantallaBase, IPantallaRecargable
{
    private readonly DataGridView _grilla = InterfazAyuda.Grilla();
    private readonly TextBox _busqueda;
    private readonly TextBox _nombre = InterfazAyuda.Caja();
    private readonly TextBox _descripcion = InterfazAyuda.Caja();
    private readonly NumericUpDown _velocidad = InterfazAyuda.Numero(100000, 0);
    private readonly NumericUpDown _precio = InterfazAyuda.Numero(100000000, 0);
    private readonly CheckBox _activo = InterfazAyuda.Casilla("Plan activo, se puede vender");
    private int _id;
    private bool _sincronizando;

    public UcPlanes() : base("Planes", "Velocidad y precio mensual. Un plan inactivo no se ofrece en un alta nueva.")
    {
        var barra = InterfazAyuda.BarraBusqueda(out _busqueda, (_, _) => Recargar());
        var ficha = InterfazAyuda.CrearFicha(out var form);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.TituloFicha("Plan"), 36);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Nombre", _nombre), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Descripción", _descripcion), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Velocidad (Mbps)", _velocidad), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Precio mensual", _precio), 68);
        InterfazAyuda.AgregarFila(form, _activo, 40);
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
        _activo.Checked = true;
    }

    public void Recargar()
    {
        try
        {
            _sincronizando = true;
            _grilla.DataSource = PlanControlador.Leer(_busqueda.Text);
            InterfazAyuda.PrepararGrilla(_grilla,
                ("id_plan", "Id", true),
                ("nombre", "Nombre", true),
                ("descripcion", "Descripción", true),
                ("velocidad_mbps", "Mbps", true),
                ("precio_mensual", "Precio", true),
                ("activo", "Activo", true));
        }
        catch (Exception ex) { InterfazAyuda.Error(ex); }
        finally { _sincronizando = false; }
        AlSeleccionar();
    }

    private void AlSeleccionar()
    {
        if (_sincronizando) return;
        var id = InterfazAyuda.IdFila(_grilla, "id_plan");
        if (id == 0) return;
        var plan = PlanControlador.LeerPorId(id);
        if (plan == null) return;
        _id = plan.IdPlan;
        _nombre.Text = plan.Nombre;
        _descripcion.Text = plan.Descripcion ?? "";
        _velocidad.Value = Math.Min(_velocidad.Maximum, plan.VelocidadMbps);
        _precio.Value = Math.Min(_precio.Maximum, plan.PrecioMensual);
        _activo.Checked = plan.Activo;
    }

    private void PrepararNuevo()
    {
        _id = 0;
        _nombre.Clear();
        _descripcion.Clear();
        _velocidad.Value = 100;
        _precio.Value = 0;
        _activo.Checked = true;
        _grilla.ClearSelection();
        MostrarEstado("Nuevo plan.");
    }

    private void Guardar()
    {
        var plan = new Plan
        {
            IdPlan = _id,
            Nombre = _nombre.Text,
            Descripcion = _descripcion.Text,
            VelocidadMbps = (int)_velocidad.Value,
            PrecioMensual = _precio.Value,
            Activo = _activo.Checked
        };
        Informar(_id == 0 ? PlanControlador.Crear(plan) : PlanControlador.Actualizar(plan));
        if (Estado.ForeColor == TemaVisual.Exito) Recargar();
    }

    private void Eliminar()
    {
        if (_id == 0 || !InterfazAyuda.Confirmar("¿Eliminar este plan? No se puede si ya hay servicios usándolo.")) return;
        Informar(PlanControlador.Eliminar(_id));
        if (Estado.ForeColor == TemaVisual.Exito) Recargar();
    }
}
