using AppIsp.Controlador;
using AppIsp.Modelo;

namespace AppIsp.Vista;

/// <summary>Disponibilidad por usuario, fecha y hora. El alta y el cambio van en otra ventana.</summary>
public class UcAgenda : PantallaBase, IPantallaRecargable
{
    private readonly DataGridView _grilla = InterfazAyuda.Grilla();
    private readonly TextBox _busqueda;

    public UcAgenda() : base("Agenda", "Bloques de una hora, de 09:00 a 20:00. Nuevo o Modificar abre el calendario en otra ventana.")
    {
        var barra = InterfazAyuda.BarraBusqueda(out _busqueda, (_, _) => Recargar());
        if (PermisosControlador.AgendarDisponibilidad)
        {
            var nuevo = InterfazAyuda.Boton("Nuevo", EstiloBoton.Primario);
            var modificar = InterfazAyuda.Boton("Modificar", EstiloBoton.Secundario);
            var eliminar = InterfazAyuda.Boton("Eliminar", EstiloBoton.Peligro);
            nuevo.Click += (_, _) => Abrir(0);
            modificar.Click += (_, _) => Modificar();
            eliminar.Click += (_, _) => Eliminar();
            _grilla.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) Modificar(); };
            Cuerpo.Controls.Add(InterfazAyuda.MarcoGrilla(_grilla));
            Cuerpo.Controls.Add(InterfazAyuda.BarraAcciones(nuevo, modificar, eliminar));
        }
        else
        {
            Cuerpo.Controls.Add(InterfazAyuda.MarcoGrilla(_grilla));
        }

        Cuerpo.Controls.Add(barra);
    }

    public void Recargar()
    {
        try
        {
            _grilla.DataSource = AgendaControlador.Leer(_busqueda.Text);
            InterfazAyuda.PrepararGrilla(_grilla,
                ("id_disponibilidad", "Id", true),
                ("usuario", "Usuario", true),
                ("fecha", "Fecha", true),
                ("bloque_horario", "Bloque", true),
                ("disponible", "Disponible", true));
        }
        catch (Exception ex) { InterfazAyuda.Error(ex); }
    }

    private int IdSeleccionado() => InterfazAyuda.IdFila(_grilla, "id_disponibilidad");

    private void Modificar()
    {
        var id = IdSeleccionado();
        if (id == 0) { MostrarEstado("Seleccione un bloque.", true); return; }
        Abrir(id);
    }

    private void Abrir(int id)
    {
        DisponibilidadAgenda? actual = null;
        if (id > 0)
        {
            actual = AgendaControlador.LeerPorId(id);
            if (actual == null)
            {
                MostrarEstado("No se encontró el bloque.", true);
                return;
            }
        }

        using var ventana = new FrmEditor(id == 0 ? "Nuevo bloque" : "Modificar bloque", 480, 640);
        var usuario = InterfazAyuda.Lista();
        var calendario = InterfazAyuda.Calendario();
        var diaElegido = new Label { AutoSize = false, Height = 24, ForeColor = TemaVisual.Texto };
        var bloque = InterfazAyuda.Lista();
        var disponible = InterfazAyuda.Casilla("Disponible");
        var sincronizando = false;
        var diaCargado = DateTime.Today;
        InterfazAyuda.CargarOpciones(usuario, UsuarioControlador.ListarOpciones(false).Where(o => o.Id != 0).ToList());
        InterfazAyuda.CargarValores(bloque, Catalogos.BloquesAgenda);
        disponible.Checked = true;

        void Pintar() => diaElegido.Text = "Día elegido: " + calendario.SelectionStart.ToString("dddd d 'de' MMMM 'de' yyyy");
        void MostrarDia(DateTime dia, bool permitirPasado)
        {
            sincronizando = true;
            var fecha = dia.Date;
            calendario.MaxDate = DateTime.Today.AddYears(3);
            var minimo = permitirPasado && fecha < DateTime.Today ? fecha : DateTime.Today;
            if (calendario.SelectionStart.Date < minimo)
                calendario.SetDate(minimo);
            calendario.MinDate = minimo;
            if (fecha < calendario.MinDate) fecha = calendario.MinDate;
            calendario.SetDate(fecha);
            diaCargado = fecha;
            sincronizando = false;
            Pintar();
        }

        calendario.DateChanged += (_, _) =>
        {
            if (sincronizando) return;
            var dia = calendario.SelectionStart.Date;
            if (dia < DateTime.Today && dia != diaCargado.Date)
            {
                sincronizando = true;
                calendario.SetDate(DateTime.Today);
                sincronizando = false;
                InterfazAyuda.ErrorTexto("No se pueden elegir días anteriores a hoy.");
            }
            Pintar();
        };

        if (actual == null) MostrarDia(DateTime.Today, false);
        else
        {
            InterfazAyuda.SeleccionarId(usuario, actual.IdUsuario);
            MostrarDia(actual.Fecha, true);
            bloque.SelectedValue = actual.BloqueHorario;
            disponible.Checked = actual.Disponible;
        }

        var form = ventana.Formulario;
        InterfazAyuda.AgregarFila(form, InterfazAyuda.TituloFicha("Bloque de una hora"), 36);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Usuario", usuario), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.TituloFicha("Día"), 32);
        InterfazAyuda.AgregarFila(form, diaElegido, 28);
        InterfazAyuda.AgregarFila(form, calendario, 180);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Bloque horario", bloque), 68);
        InterfazAyuda.AgregarFila(form, disponible, 40);

        var guardar = InterfazAyuda.Boton("Guardar", EstiloBoton.Primario);
        var cancelar = InterfazAyuda.Boton("Cancelar", EstiloBoton.Secundario);
        cancelar.DialogResult = DialogResult.Cancel;
        guardar.Click += (_, _) =>
        {
            var item = new DisponibilidadAgenda
            {
                IdDisponibilidad = id,
                IdUsuario = InterfazAyuda.IdSeleccionado(usuario),
                Fecha = calendario.SelectionStart.Date,
                BloqueHorario = InterfazAyuda.ValorSeleccionado(bloque),
                Disponible = disponible.Checked
            };
            ventana.CerrarSi(id == 0 ? AgendaControlador.Crear(item) : AgendaControlador.Actualizar(item));
        };
        InterfazAyuda.AgregarFila(form, InterfazAyuda.FilaBotones(guardar, cancelar), 56);
        ventana.CancelButton = cancelar;
        ventana.AcceptButton = guardar;

        if (ventana.ShowDialog(FindForm()) == DialogResult.OK)
        {
            MostrarEstado(id == 0 ? "Bloque creado." : "Bloque actualizado.");
            Recargar();
        }
    }

    private void Eliminar()
    {
        var id = IdSeleccionado();
        if (id == 0 || !InterfazAyuda.Confirmar("¿Eliminar este bloque?")) return;
        Informar(AgendaControlador.Eliminar(id));
        if (Estado.ForeColor == TemaVisual.Exito) Recargar();
    }
}
