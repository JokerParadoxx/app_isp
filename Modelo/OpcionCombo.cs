namespace AppIsp.Modelo;

/// <summary>
/// Elemento de un ComboBox enlazado a un id de la base.
/// Id = 0 significa "nada seleccionado" o "sin asignar".
/// </summary>
public sealed class OpcionCombo
{
    public int Id { get; set; }
    public string Texto { get; set; } = "";
}

/// <summary>
/// Elemento de un ComboBox cuyo valor guardado es un texto (por ejemplo un ENUM).
/// Valor es lo que va a MySQL. Texto es lo que ve la persona.
/// </summary>
public sealed class OpcionTexto
{
    public string Valor { get; set; } = "";
    public string Texto { get; set; } = "";
}
