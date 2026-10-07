using AppIsp.Controlador;
using AppIsp.Modelo;

namespace AppIsp.Vista;

/// <summary>
/// El identificador del equipo es la MAC. La ficha muestra el cliente y el perfil del plan.
/// </summary>
public class UcProvisionamiento : PantallaBase, IPantallaRecargable
{
    private readonly DataGridView _routers = InterfazAyuda.Grilla();

    public UcProvisionamiento() : base(
        "Aprovisionamiento",
        "La MAC identifica al equipo. Aprovisionar abre la ventana con el cliente y el perfil del plan.")
    {
        var aprovisionar = InterfazAyuda.Boton("Aprovisionar", EstiloBoton.Primario);
        aprovisionar.Click += (_, _) => Abrir();
        _routers.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) Abrir(); };
        Cuerpo.Controls.Add(InterfazAyuda.MarcoGrilla(_routers));
        Cuerpo.Controls.Add(InterfazAyuda.BarraAcciones(aprovisionar));
    }

    public void Recargar()
    {
        try
        {
            _routers.DataSource = ProvisionamientoControlador.LeerEquipos();
            InterfazAyuda.PrepararGrilla(_routers,
                ("id_router", "Id", false),
                ("mac_address", "MAC", true),
                ("id_cliente", "Id cliente", true),
                ("cliente", "Cliente", true),
                ("plan", "Plan", true),
                ("velocidad_mbps", "Mbps", true),
                ("modelo", "Modelo", true),
                ("ip_gestion", "IP", true),
                ("estado", "Estado", true));
            if (_routers.Rows.Count > 0)
                _routers.Rows[0].Selected = true;
        }
        catch (Exception ex) { InterfazAyuda.Error(ex); }
    }

    private void Abrir()
    {
        var id = InterfazAyuda.IdFila(_routers, "id_router");
        if (id == 0)
        {
            InterfazAyuda.ErrorTexto("Seleccione un router en la lista.");
            return;
        }

        var ficha = ProvisionamientoControlador.LeerFicha(id);
        if (ficha == null)
        {
            InterfazAyuda.ErrorTexto("No se encontró el router.");
            return;
        }

        using var ventana = new FrmEditor("Aprovisionar " + ficha.Mac, 540, 760);
        var form = ventana.Formulario;
        InterfazAyuda.AgregarFila(form, InterfazAyuda.TituloFicha("Identificador"), 36);
        InterfazAyuda.AgregarFila(form, Dato("MAC", ficha.Mac), 52);
        InterfazAyuda.AgregarFila(form, Dato("Modelo", ficha.Modelo), 52);
        InterfazAyuda.AgregarFila(form, Dato("Serie", ficha.Serie), 52);
        InterfazAyuda.AgregarFila(form, Dato("IP de gestión", string.IsNullOrWhiteSpace(ficha.IpGestion) ? "Sin IP" : ficha.IpGestion), 52);
        InterfazAyuda.AgregarFila(form, Dato("Estado del equipo", Catalogos.TextoAmigable(ficha.Estado)), 52);
        InterfazAyuda.AgregarFila(form, Dato("Ubicación", string.IsNullOrWhiteSpace(ficha.Ubicacion) ? "Sin ubicación" : ficha.Ubicacion), 52);

        InterfazAyuda.AgregarFila(form, InterfazAyuda.TituloFicha("Cliente asociado"), 36);
        if (ficha.IdCliente == 0)
        {
            InterfazAyuda.AgregarFila(form, Dato("Cliente", "Este equipo no está asociado a un cliente."), 52);
        }
        else
        {
            InterfazAyuda.AgregarFila(form, Dato("Id del cliente", ficha.IdCliente.ToString()), 52);
            InterfazAyuda.AgregarFila(form, Dato("Nombre", ficha.Cliente), 52);
            InterfazAyuda.AgregarFila(form, Dato("Identificación", ficha.Identificacion), 52);
            InterfazAyuda.AgregarFila(form, Dato("Estado del servicio", Catalogos.TextoAmigable(ficha.EstadoServicio)), 52);
        }

        InterfazAyuda.AgregarFila(form, InterfazAyuda.TituloFicha("Perfil del plan"), 36);
        if (string.IsNullOrWhiteSpace(ficha.Plan))
        {
            InterfazAyuda.AgregarFila(form, Dato("Plan", "Sin plan asociado."), 52);
        }
        else
        {
            InterfazAyuda.AgregarFila(form, Dato("Plan", ficha.Plan), 52);
            InterfazAyuda.AgregarFila(form, Dato("Perfil", string.IsNullOrWhiteSpace(ficha.Perfil) ? ficha.Plan : ficha.Perfil), 52);
            InterfazAyuda.AgregarFila(form, Dato("Velocidad", ficha.VelocidadMbps + " Mbps"), 52);
            InterfazAyuda.AgregarFila(form, Dato("Precio mensual", ficha.PrecioMensual.ToString("N0")), 52);
        }

        var ip = InterfazAyuda.Caja();
        ip.Text = ficha.IpGestion ?? "";
        var config = InterfazAyuda.CajaMultilinea();
        InterfazAyuda.AgregarFila(form, InterfazAyuda.TituloFicha("Comandos"), 36);
        var reiniciar = InterfazAyuda.Boton("Reiniciar", EstiloBoton.Secundario);
        reiniciar.Click += (_, _) => ventana.CerrarSi(ProvisionamientoControlador.Reiniciar(ficha.IdRouter));
        InterfazAyuda.AgregarFila(form, InterfazAyuda.FilaBotones(reiniciar), 52);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Nueva IP de gestión", ip), 68);
        var cambiarIp = InterfazAyuda.Boton("Cambiar IP", EstiloBoton.Primario);
        cambiarIp.Click += (_, _) => ventana.CerrarSi(ProvisionamientoControlador.CambiarIp(ficha.IdRouter, ip.Text));
        InterfazAyuda.AgregarFila(form, InterfazAyuda.FilaBotones(cambiarIp), 52);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Configuración a registrar", config, 100), 108);
        var enviar = InterfazAyuda.Boton("Enviar configuración", EstiloBoton.Primario);
        enviar.Click += (_, _) => ventana.CerrarSi(ProvisionamientoControlador.EnviarConfiguracion(ficha.IdRouter, config.Text));
        InterfazAyuda.AgregarFila(form, InterfazAyuda.FilaBotones(enviar), 52);
        var cerrar = InterfazAyuda.Boton("Cerrar", EstiloBoton.Secundario);
        cerrar.DialogResult = DialogResult.Cancel;
        InterfazAyuda.AgregarFila(form, InterfazAyuda.FilaBotones(cerrar), 52);
        ventana.CancelButton = cerrar;

        if (ventana.ShowDialog(FindForm()) == DialogResult.OK)
        {
            MostrarEstado("Acción registrada para " + ficha.Mac + ".");
            Recargar();
        }
    }

    private static Label Dato(string titulo, string valor)
    {
        return new Label
        {
            Text = titulo + ":  " + valor,
            AutoSize = false,
            Dock = DockStyle.Fill,
            ForeColor = TemaVisual.Texto,
            TextAlign = ContentAlignment.MiddleLeft
        };
    }
}
