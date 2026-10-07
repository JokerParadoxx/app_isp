using AppIsp.Modelo;

namespace AppIsp.Vista;

/// <summary>
/// Ventana aparte para crear o modificar un registro.
/// La lista queda en la pantalla de atrás.
/// </summary>
public class FrmEditor : Form
{
    public TableLayoutPanel Formulario { get; }

    public FrmEditor(string titulo, int ancho, int alto)
    {
        Text = titulo;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        Font = TemaVisual.Fuente;
        BackColor = Color.White;
        ClientSize = new Size(ancho, alto);

        Formulario = InterfazAyuda.ColumnaFormulario();
        Controls.Add(Formulario);
    }

    public void CerrarSi(Respuesta respuesta)
    {
        if (!respuesta.Exito)
        {
            InterfazAyuda.ErrorTexto(respuesta.Mensaje);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}
