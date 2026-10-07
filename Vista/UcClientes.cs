using AppIsp.Controlador;
using AppIsp.Modelo;

namespace AppIsp.Vista;

/// <summary>Listado de abonados. Alta y cambio se hacen en una ventana aparte.</summary>
public class UcClientes : PantallaBase, IPantallaRecargable
{
    private readonly DataGridView _grilla = InterfazAyuda.Grilla();
    private readonly TextBox _busqueda;

    public UcClientes() : base("Clientes", "La lista queda a la vista. Nuevo o Modificar abre la ficha en otra ventana.")
    {
        var barra = InterfazAyuda.BarraBusqueda(out _busqueda, (_, _) => Recargar());
        InterfazAyuda.Pista(_busqueda, "Nombre, identificación o correo");

        var nuevo = InterfazAyuda.Boton("Nuevo", EstiloBoton.Primario);
        var modificar = InterfazAyuda.Boton("Modificar", EstiloBoton.Secundario);
        nuevo.Click += (_, _) => Abrir(0);
        modificar.Click += (_, _) => Modificar();
        var botones = new List<Button> { nuevo, modificar };
        if (PermisosControlador.EliminarClientes)
        {
            var eliminar = InterfazAyuda.Boton("Eliminar", EstiloBoton.Peligro);
            eliminar.Click += (_, _) => Eliminar();
            botones.Add(eliminar);
        }

        _grilla.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) Modificar(); };
        Cuerpo.Controls.Add(InterfazAyuda.MarcoGrilla(_grilla));
        Cuerpo.Controls.Add(InterfazAyuda.BarraAcciones(botones.ToArray()));
        Cuerpo.Controls.Add(barra);
    }

    public void Recargar()
    {
        try
        {
            _grilla.DataSource = ClienteControlador.Leer(_busqueda.Text);
            InterfazAyuda.PrepararGrilla(_grilla,
                ("id_cliente", "Id", true),
                ("nombre", "Nombre", true),
                ("identificacion", "Identificación", true),
                ("direccion", "Dirección", true),
                ("comuna", "Comuna", true),
                ("telefono", "Teléfono", true),
                ("email", "Correo", true),
                ("estado", "Estado", true),
                ("fecha_alta", "Alta", true),
                ("fecha_baja", "Baja", true));
        }
        catch (Exception ex) { InterfazAyuda.Error(ex); }
    }

    private int IdSeleccionado() => InterfazAyuda.IdFila(_grilla, "id_cliente");

    private void Modificar()
    {
        var id = IdSeleccionado();
        if (id == 0) { MostrarEstado("Seleccione un cliente.", true); return; }
        Abrir(id);
    }

    private void Abrir(int id)
    {
        using var ventana = new FrmEditor(id == 0 ? "Nuevo cliente" : "Modificar cliente", 480, 760);
        if (!ArmarCliente(ventana, id)) return;
        if (ventana.ShowDialog(FindForm()) == DialogResult.OK)
        {
            MostrarEstado(id == 0 ? "Cliente creado." : "Cliente actualizado.");
            Recargar();
        }
    }

    private bool ArmarCliente(FrmEditor ventana, int id)
    {
        Cliente? actual = null;
        if (id > 0)
        {
            actual = ClienteControlador.LeerPorId(id);
            if (actual == null)
            {
                MostrarEstado("No se encontró el cliente.", true);
                return false;
            }
        }

        var nombre = InterfazAyuda.Caja();
        var identificacion = InterfazAyuda.Caja();
        var direccion = InterfazAyuda.Caja();
        var comuna = InterfazAyuda.Caja();
        var telefono = InterfazAyuda.Caja();
        var email = InterfazAyuda.Caja();
        var estado = InterfazAyuda.Lista();
        var alta = InterfazAyuda.Fecha();
        var baja = InterfazAyuda.FechaOpcional();
        InterfazAyuda.CargarValores(estado, Catalogos.EstadosCliente);

        if (actual == null)
        {
            alta.Value = DateTime.Today;
            estado.SelectedIndex = 0;
        }
        else
        {
            nombre.Text = actual.Nombre;
            identificacion.Text = actual.Identificacion;
            direccion.Text = actual.Direccion;
            comuna.Text = actual.Comuna ?? "";
            telefono.Text = actual.Telefono ?? "";
            email.Text = actual.Email ?? "";
            estado.SelectedValue = actual.Estado;
            alta.Value = actual.FechaAlta;
            baja.Checked = actual.FechaBaja.HasValue;
            if (actual.FechaBaja.HasValue) baja.Value = actual.FechaBaja.Value;
        }

        var form = ventana.Formulario;
        InterfazAyuda.AgregarFila(form, InterfazAyuda.TituloFicha("Ficha del cliente"), 36);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Nombre", nombre), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Identificación", identificacion), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Dirección", direccion), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Comuna", comuna), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Teléfono", telefono), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Correo", email), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Estado", estado), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Fecha de alta", alta), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Fecha de baja", baja), 68);

        var guardar = InterfazAyuda.Boton("Guardar", EstiloBoton.Primario);
        var cancelar = InterfazAyuda.Boton("Cancelar", EstiloBoton.Secundario);
        cancelar.DialogResult = DialogResult.Cancel;
        guardar.Click += (_, _) =>
        {
            var cliente = new Cliente
            {
                IdCliente = id,
                Nombre = nombre.Text,
                Identificacion = identificacion.Text,
                Direccion = direccion.Text,
                Comuna = comuna.Text,
                Telefono = telefono.Text,
                Email = email.Text,
                Estado = InterfazAyuda.ValorSeleccionado(estado),
                FechaAlta = alta.Value,
                FechaBaja = baja.Checked ? baja.Value : null
            };
            ventana.CerrarSi(id == 0 ? ClienteControlador.Crear(cliente) : ClienteControlador.Actualizar(cliente));
        };
        InterfazAyuda.AgregarFila(form, InterfazAyuda.FilaBotones(guardar, cancelar), 56);
        ventana.CancelButton = cancelar;
        ventana.AcceptButton = guardar;
        return true;
    }

    private void Eliminar()
    {
        var id = IdSeleccionado();
        if (id == 0) { MostrarEstado("Seleccione un cliente.", true); return; }
        if (!InterfazAyuda.Confirmar("¿Eliminar este cliente? Si tiene servicios o facturas, la base no lo va a permitir."))
            return;
        Informar(ClienteControlador.Eliminar(id));
        if (Estado.ForeColor == TemaVisual.Exito) Recargar();
    }
}
