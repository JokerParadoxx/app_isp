using AppIsp.Controlador;

namespace AppIsp.Vista;

/// <summary>Dos historiales: cambios comerciales y comandos de aprovisionamiento.</summary>
public class UcAuditoria : PantallaBase, IPantallaRecargable
{
    private readonly DataGridView _grilla = InterfazAyuda.Grilla();
    private bool _comercial = true;
    private readonly Button _btnComercial;
    private readonly Button _btnTecnico;

    public UcAuditoria() : base("Auditoría", "Qué cambió en los servicios y qué comandos se simularon sobre los routers.")
    {
        var barra = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = TemaVisual.Fondo };
        _btnComercial = InterfazAyuda.Boton("Log comercial", EstiloBoton.Primario);
        _btnTecnico = InterfazAyuda.Boton("Log técnico", EstiloBoton.Secundario);
        _btnComercial.Location = new Point(0, 6);
        _btnTecnico.Location = new Point(160, 6);
        _btnComercial.Click += (_, _) => { _comercial = true; Recargar(); };
        _btnTecnico.Click += (_, _) => { _comercial = false; Recargar(); };

        var eliminar = InterfazAyuda.Boton("Eliminar registro", EstiloBoton.Peligro);
        eliminar.Location = new Point(320, 6);
        eliminar.Click += (_, _) => Eliminar();
        barra.Controls.AddRange(new Control[] { _btnComercial, _btnTecnico, eliminar });

        Cuerpo.Controls.Add(InterfazAyuda.MarcoGrilla(_grilla));
        Cuerpo.Controls.Add(barra);
    }

    public void Recargar()
    {
        try
        {
            if (_comercial)
            {
                _grilla.DataSource = LogComercialControlador.Leer();
                InterfazAyuda.PrepararGrilla(_grilla,
                    ("id_log", "Id", true),
                    ("cliente", "Cliente", true),
                    ("plan", "Plan", true),
                    ("usuario", "Usuario", true),
                    ("tipo_cambio", "Cambio", true),
                    ("detalle", "Detalle", true),
                    ("fecha_cambio", "Fecha", true));
            }
            else
            {
                _grilla.DataSource = LogProvisionamientoControlador.Leer();
                InterfazAyuda.PrepararGrilla(_grilla,
                    ("id_log", "Id", true),
                    ("serie", "Router", true),
                    ("usuario", "Usuario", true),
                    ("accion", "Acción", true),
                    ("detalle", "Detalle", true),
                    ("fecha_accion", "Fecha", true));
            }
            MostrarEstado(_comercial ? "Cambios comerciales." : "Aprovisionamiento técnico.");
        }
        catch (Exception ex) { InterfazAyuda.Error(ex); }
    }

    private void Eliminar()
    {
        var id = InterfazAyuda.IdFila(_grilla, "id_log");
        if (id == 0 || !InterfazAyuda.Confirmar("¿Eliminar este registro de auditoría?")) return;
        Informar(_comercial
            ? LogComercialControlador.Eliminar(id)
            : LogProvisionamientoControlador.Eliminar(id));
        if (Estado.ForeColor == TemaVisual.Exito) Recargar();
    }
}
