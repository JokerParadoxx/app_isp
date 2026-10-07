using AppIsp.Controlador;
using AppIsp.Modelo;

namespace AppIsp.Vista;

/// <summary>Lista de visitas. Crear o modificar abre el calendario en otra ventana.</summary>
public class UcInstalaciones : PantallaBase, IPantallaRecargable
{
    private readonly DataGridView _grilla = InterfazAyuda.Grilla();
    private readonly TextBox _busqueda;

    public UcInstalaciones() : base("Instalaciones", "Nueva o Modificar abre la visita en otra ventana, con el calendario del día.")
    {
        var barra = InterfazAyuda.BarraBusqueda(out _busqueda, (_, _) => Recargar());
        var botones = new List<Button>();
        if (PermisosControlador.CrearInstalacion)
        {
            var nuevo = InterfazAyuda.Boton("Nueva", EstiloBoton.Primario);
            nuevo.Click += (_, _) => Abrir(0);
            botones.Add(nuevo);
        }
        var modificar = InterfazAyuda.Boton("Modificar", EstiloBoton.Secundario);
        modificar.Click += (_, _) => Modificar();
        botones.Add(modificar);
        if (PermisosControlador.EliminarInstalacion)
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
            _grilla.DataSource = InstalacionControlador.Leer(_busqueda.Text);
            InterfazAyuda.PrepararGrilla(_grilla,
                ("id_instalacion", "Id", true),
                ("cliente", "Cliente", true),
                ("plan", "Plan", true),
                ("tecnico", "Técnico", true),
                ("fecha_programada", "Programada", true),
                ("fecha_real", "Real", true),
                ("estado", "Estado", true),
                ("comentario", "Comentario", true));
        }
        catch (Exception ex) { InterfazAyuda.Error(ex); }
    }

    private int IdSeleccionado() => InterfazAyuda.IdFila(_grilla, "id_instalacion");

    private void Modificar()
    {
        var id = IdSeleccionado();
        if (id == 0) { MostrarEstado("Seleccione una instalación.", true); return; }
        Abrir(id);
    }

    private void Abrir(int id)
    {
        Instalacion? actual = null;
        if (id > 0)
        {
            actual = InstalacionControlador.LeerPorId(id);
            if (actual == null)
            {
                MostrarEstado("No se encontró la instalación.", true);
                return;
            }
        }

        using var ventana = new FrmEditor(id == 0 ? "Nueva instalación" : "Modificar instalación", 500, 820);
        var servicio = InterfazAyuda.Lista();
        var tecnico = InterfazAyuda.Lista();
        var calendario = InterfazAyuda.Calendario();
        var hora = InterfazAyuda.Hora();
        var diaElegido = new Label { AutoSize = false, Height = 24, ForeColor = TemaVisual.Texto };
        var real = InterfazAyuda.FechaOpcional(true);
        var estado = InterfazAyuda.Lista();
        var comentario = InterfazAyuda.Caja();
        var sincronizando = false;
        var diaCargado = DateTime.Today;
        InterfazAyuda.CargarOpciones(servicio, ServicioControlador.ListarOpciones());
        InterfazAyuda.CargarOpciones(tecnico, UsuarioControlador.ListarOpciones(true));
        InterfazAyuda.CargarValores(estado, Catalogos.EstadosInstalacion);

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

        if (actual == null)
        {
            MostrarDia(DateTime.Today, false);
            hora.Value = DateTime.Today.AddHours(9);
            estado.SelectedValue = "PENDIENTE";
        }
        else
        {
            InterfazAyuda.SeleccionarId(servicio, actual.IdServicio);
            InterfazAyuda.SeleccionarId(tecnico, actual.IdTecnico ?? 0);
            MostrarDia(actual.FechaProgramada, true);
            hora.Value = DateTime.Today.Add(actual.FechaProgramada.TimeOfDay);
            real.Checked = actual.FechaReal.HasValue;
            if (actual.FechaReal.HasValue) real.Value = actual.FechaReal.Value;
            estado.SelectedValue = actual.Estado;
            comentario.Text = actual.Comentario ?? "";
        }

        if (PermisosControlador.SoloEstadoInstalacion)
        {
            servicio.Enabled = false;
            tecnico.Enabled = false;
            calendario.Enabled = false;
            hora.Enabled = false;
        }

        var form = ventana.Formulario;
        InterfazAyuda.AgregarFila(form, InterfazAyuda.TituloFicha("Visita"), 36);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Servicio", servicio), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Técnico", tecnico), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.TituloFicha("Día de la visita"), 32);
        InterfazAyuda.AgregarFila(form, diaElegido, 28);
        InterfazAyuda.AgregarFila(form, calendario, 180);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Hora", hora), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Fecha real", real), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Estado", estado), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Comentario", comentario), 68);

        var guardar = InterfazAyuda.Boton("Guardar", EstiloBoton.Primario);
        var cancelar = InterfazAyuda.Boton("Cancelar", EstiloBoton.Secundario);
        cancelar.DialogResult = DialogResult.Cancel;
        guardar.Click += (_, _) =>
        {
            if (id == 0 && !PermisosControlador.CrearInstalacion)
            {
                InterfazAyuda.ErrorTexto("Seleccione una instalación para actualizar su estado.");
                return;
            }

            var item = new Instalacion
            {
                IdInstalacion = id,
                IdServicio = InterfazAyuda.IdSeleccionado(servicio),
                IdTecnico = InterfazAyuda.IdSeleccionado(tecnico),
                FechaProgramada = calendario.SelectionStart.Date.Add(hora.Value.TimeOfDay),
                FechaReal = real.Checked ? real.Value : null,
                Estado = InterfazAyuda.ValorSeleccionado(estado),
                Comentario = comentario.Text
            };
            ventana.CerrarSi(id == 0 ? InstalacionControlador.Crear(item) : InstalacionControlador.Actualizar(item));
        };
        InterfazAyuda.AgregarFila(form, InterfazAyuda.FilaBotones(guardar, cancelar), 56);
        ventana.CancelButton = cancelar;
        ventana.AcceptButton = guardar;

        if (ventana.ShowDialog(FindForm()) == DialogResult.OK)
        {
            MostrarEstado(id == 0 ? "Instalación agendada." : "Instalación actualizada.");
            Recargar();
        }
    }

    private void Eliminar()
    {
        var id = IdSeleccionado();
        if (id == 0 || !InterfazAyuda.Confirmar("¿Eliminar esta instalación?")) return;
        Informar(InstalacionControlador.Eliminar(id));
        if (Estado.ForeColor == TemaVisual.Exito) Recargar();
    }
}
