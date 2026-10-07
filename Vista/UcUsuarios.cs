using AppIsp.Controlador;
using AppIsp.Modelo;

namespace AppIsp.Vista;

/// <summary>
/// Listado de cuentas. Crear y modificar abre una ventana con la ficha.
/// La contraseña se deja en blanco al editar si no se quiere cambiar.
/// </summary>
public class UcUsuarios : PantallaBase, IPantallaRecargable
{
    private readonly DataGridView _grilla = InterfazAyuda.Grilla();
    private readonly TextBox _busqueda;

    public UcUsuarios() : base("Usuarios y roles", "Nuevo o Modificar abre la cuenta en otra ventana. Un doble clic también la abre.")
    {
        var barra = InterfazAyuda.BarraBusqueda(out _busqueda, (_, _) => Recargar());
        var nuevo = InterfazAyuda.Boton("Nuevo", EstiloBoton.Primario);
        var modificar = InterfazAyuda.Boton("Modificar", EstiloBoton.Secundario);
        var eliminar = InterfazAyuda.Boton("Eliminar", EstiloBoton.Peligro);
        nuevo.Click += (_, _) => Abrir(0);
        modificar.Click += (_, _) => Modificar();
        eliminar.Click += (_, _) => Eliminar();
        _grilla.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) Modificar(); };

        Cuerpo.Controls.Add(InterfazAyuda.MarcoGrilla(_grilla));
        Cuerpo.Controls.Add(InterfazAyuda.BarraAcciones(nuevo, modificar, eliminar));
        Cuerpo.Controls.Add(barra);
    }

    public void Recargar()
    {
        try
        {
            _grilla.DataSource = UsuarioControlador.Leer(_busqueda.Text);
            InterfazAyuda.PrepararGrilla(_grilla,
                ("id_usuario", "Id", true),
                ("nombre", "Nombre", true),
                ("email", "Correo", true),
                ("rol", "Rol", true),
                ("activo", "Activo", true),
                ("fecha_creacion", "Creado", true));
        }
        catch (Exception ex) { InterfazAyuda.Error(ex); }
    }

    private int IdSeleccionado() => InterfazAyuda.IdFila(_grilla, "id_usuario");

    private void Modificar()
    {
        var id = IdSeleccionado();
        if (id == 0) { MostrarEstado("Seleccione un usuario.", true); return; }
        Abrir(id);
    }

    private void Abrir(int id)
    {
        Usuario? actual = null;
        if (id > 0)
        {
            actual = UsuarioControlador.LeerPorId(id);
            if (actual == null)
            {
                MostrarEstado("No se encontró el usuario.", true);
                return;
            }
        }

        using var ventana = new FrmEditor(id == 0 ? "Nuevo usuario" : "Modificar usuario", 480, 720);
        var idTexto = InterfazAyuda.Caja();
        var nombre = InterfazAyuda.Caja();
        var email = InterfazAyuda.Caja();
        var clave = InterfazAyuda.Caja();
        var rol = InterfazAyuda.Lista();
        var activo = InterfazAyuda.Casilla("Usuario activo");
        var descripcion = InterfazAyuda.Caja();
        clave.UseSystemPasswordChar = true;
        activo.Checked = true;
        InterfazAyuda.CargarOpciones(rol, RolControlador.ListarOpciones());
        rol.SelectedIndexChanged += (_, _) =>
        {
            var idRol = InterfazAyuda.IdSeleccionado(rol);
            if (idRol == 0) return;
            descripcion.Text = RolControlador.LeerPorId(idRol)?.Descripcion ?? "";
        };

        if (actual != null)
        {
            idTexto.Text = actual.IdUsuario.ToString();
            nombre.Text = actual.Nombre;
            email.Text = actual.Email;
            InterfazAyuda.SeleccionarId(rol, actual.IdRol);
            activo.Checked = actual.Activo;
            descripcion.Text = RolControlador.LeerPorId(actual.IdRol)?.Descripcion ?? "";
        }

        var form = ventana.Formulario;
        InterfazAyuda.AgregarFila(form, InterfazAyuda.TituloFicha("Cuenta"), 36);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Id", idTexto), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Nombre", nombre), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Correo", email), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque(id == 0 ? "Contraseña" : "Contraseña (vacía = no cambiar)", clave), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Rol", rol), 68);
        InterfazAyuda.AgregarFila(form, activo, 40);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.TituloFicha("Descripción del rol elegido"), 36);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Descripción", descripcion), 68);

        var guardar = InterfazAyuda.Boton("Guardar", EstiloBoton.Primario);
        var guardarRol = InterfazAyuda.Boton("Guardar descripción", EstiloBoton.Secundario);
        var cancelar = InterfazAyuda.Boton("Cancelar", EstiloBoton.Secundario);
        cancelar.DialogResult = DialogResult.Cancel;
        guardar.Click += (_, _) =>
        {
            if (!int.TryParse(idTexto.Text.Trim(), out var idNuevo) || idNuevo <= 0)
            {
                InterfazAyuda.ErrorTexto("Indique un id numérico mayor que cero.");
                return;
            }

            var usuario = new Usuario
            {
                IdUsuario = idNuevo,
                Nombre = nombre.Text,
                Email = email.Text,
                PasswordNueva = clave.Text,
                IdRol = InterfazAyuda.IdSeleccionado(rol),
                Activo = activo.Checked
            };
            ventana.CerrarSi(id == 0 ? UsuarioControlador.Crear(usuario) : UsuarioControlador.Actualizar(usuario, id));
        };
        guardarRol.Click += (_, _) =>
        {
            var respuesta = RolControlador.Actualizar(new Rol
            {
                IdRol = InterfazAyuda.IdSeleccionado(rol),
                Descripcion = descripcion.Text
            });
            if (!respuesta.Exito) InterfazAyuda.ErrorTexto(respuesta.Mensaje);
            else MostrarEstado(respuesta.Mensaje);
        };
        InterfazAyuda.AgregarFila(form, InterfazAyuda.FilaBotones(guardar, guardarRol, cancelar), 56);
        ventana.CancelButton = cancelar;
        ventana.AcceptButton = guardar;

        if (ventana.ShowDialog(FindForm()) == DialogResult.OK)
        {
            MostrarEstado(id == 0 ? "Usuario creado." : "Usuario actualizado.");
            Recargar();
        }
    }

    private void Eliminar()
    {
        var id = IdSeleccionado();
        if (id == 0) { MostrarEstado("Seleccione un usuario.", true); return; }
        if (!InterfazAyuda.Confirmar("¿Eliminar este usuario?")) return;
        Informar(UsuarioControlador.Eliminar(id));
        if (Estado.ForeColor == TemaVisual.Exito) Recargar();
    }
}
