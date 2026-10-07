using System.Data;
using AppIsp.Modelo;
using MySqlConnector;

namespace AppIsp.Controlador;

/// <summary>
/// Ficha de abonados. Comercial y maestro crean y editan.
/// Borrar el registro (no darlo de baja) queda solo para el maestro.
/// ListarOpciones entrega solo id y nombre, para poder elegir un cliente en un ticket.
/// </summary>
public static class ClienteControlador
{
    public static DataTable Leer(string texto)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.EditarClientes, "Su rol no administra la ficha de clientes.");
        if (bloqueo != null) throw new InvalidOperationException(bloqueo.Mensaje);

        var busqueda = (texto ?? "").Trim();
        return ConexionMySql.Consultar(
            @"SELECT id_cliente, nombre, identificacion, direccion, comuna, telefono, email, estado, fecha_alta, fecha_baja
              FROM clientes
              WHERE (@q = '' OR nombre LIKE @like OR identificacion LIKE @like OR IFNULL(email, '') LIKE @like)
              ORDER BY id_cliente DESC",
            ConexionMySql.P("@q", busqueda),
            ConexionMySql.P("@like", "%" + busqueda + "%"));
    }

    public static Cliente? LeerPorId(int id)
    {
        PermisosControlador.ExigirSesion();
        return ConexionMySql.LeerPrimero(
            "SELECT * FROM clientes WHERE id_cliente = @id",
            Mapear,
            ConexionMySql.P("@id", id));
    }

    public static List<OpcionCombo> ListarOpciones()
    {
        PermisosControlador.ExigirSesion();
        var tabla = ConexionMySql.Consultar(
            @"SELECT id_cliente, CONCAT(nombre, ' — ', estado) AS texto
              FROM clientes ORDER BY nombre");
        var lista = new List<OpcionCombo>();
        foreach (DataRow fila in tabla.Rows)
        {
            lista.Add(new OpcionCombo
            {
                Id = Convert.ToInt32(fila["id_cliente"]),
                Texto = Convert.ToString(fila["texto"]) ?? ""
            });
        }
        return lista;
    }

    public static Respuesta Crear(Cliente cliente)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.EditarClientes, "Su rol no puede crear clientes.");
        if (bloqueo != null) return bloqueo;
        var error = Validar(cliente);
        if (error != null) return Respuesta.Fallo(error);

        try
        {
            ConexionMySql.Insertar(
                @"INSERT INTO clientes (nombre, identificacion, direccion, comuna, telefono, email, estado, fecha_alta, fecha_baja)
                  VALUES (@nombre, @identificacion, @direccion, @comuna, @telefono, @email, @estado, @alta, @baja)",
                Parametros(cliente));
            return Respuesta.Ok("Cliente creado.");
        }
        catch (Exception ex)
        {
            return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex));
        }
    }

    public static Respuesta Actualizar(Cliente cliente)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.EditarClientes, "Su rol no puede editar clientes.");
        if (bloqueo != null) return bloqueo;
        var error = Validar(cliente);
        if (error != null) return Respuesta.Fallo(error);

        try
        {
            var filas = ConexionMySql.Ejecutar(
                @"UPDATE clientes
                  SET nombre = @nombre, identificacion = @identificacion, direccion = @direccion,
                      comuna = @comuna, telefono = @telefono, email = @email, estado = @estado,
                      fecha_alta = @alta, fecha_baja = @baja
                  WHERE id_cliente = @id",
                Parametros(cliente));
            if (filas == 0) return Respuesta.Fallo("No se encontró el cliente.");
            return Respuesta.Ok("Cliente actualizado.");
        }
        catch (Exception ex)
        {
            return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex));
        }
    }

    public static Respuesta Eliminar(int id)
    {
        var bloqueo = PermisosControlador.BloquearSi(PermisosControlador.EliminarClientes, "Solo el maestro puede eliminar un cliente. Use el estado BAJA si corresponde.");
        if (bloqueo != null) return bloqueo;

        try
        {
            var filas = ConexionMySql.Ejecutar("DELETE FROM clientes WHERE id_cliente = @id", ConexionMySql.P("@id", id));
            if (filas == 0) return Respuesta.Fallo("No se encontró el cliente.");
            return Respuesta.Ok("Cliente eliminado.");
        }
        catch (Exception ex)
        {
            return Respuesta.Fallo(ConexionMySql.MensajeAmigable(ex));
        }
    }

    private static string? Validar(Cliente cliente)
    {
        return Validador.Obligatorio(cliente.Nombre, "Nombre", 150)
            ?? Validador.Obligatorio(cliente.Identificacion, "Identificación", 50)
            ?? Validador.Obligatorio(cliente.Direccion, "Dirección", 255)
            ?? Validador.Opcional(cliente.Comuna, "Comuna", 100)
            ?? Validador.Opcional(cliente.Telefono, "Teléfono", 50)
            ?? Validador.EmailOpcional(cliente.Email)
            ?? Validador.EnLista(cliente.Estado, Catalogos.EstadosCliente, "Estado");
    }

    private static MySqlParameter[] Parametros(Cliente cliente)
    {
        return new[]
        {
            ConexionMySql.P("@id", cliente.IdCliente),
            ConexionMySql.P("@nombre", cliente.Nombre.Trim()),
            ConexionMySql.P("@identificacion", cliente.Identificacion.Trim()),
            ConexionMySql.P("@direccion", cliente.Direccion.Trim()),
            ConexionMySql.P("@comuna", Validador.Limpio(cliente.Comuna)),
            ConexionMySql.P("@telefono", Validador.Limpio(cliente.Telefono)),
            ConexionMySql.P("@email", Validador.Limpio(cliente.Email)),
            ConexionMySql.P("@estado", cliente.Estado),
            ConexionMySql.P("@alta", cliente.FechaAlta.Date),
            ConexionMySql.P("@baja", cliente.FechaBaja?.Date)
        };
    }

    private static Cliente Mapear(MySqlDataReader lector)
    {
        return new Cliente
        {
            IdCliente = LectorFila.Entero(lector, "id_cliente"),
            Nombre = LectorFila.Texto(lector, "nombre"),
            Identificacion = LectorFila.Texto(lector, "identificacion"),
            Direccion = LectorFila.Texto(lector, "direccion"),
            Comuna = LectorFila.TextoNulo(lector, "comuna"),
            Telefono = LectorFila.TextoNulo(lector, "telefono"),
            Email = LectorFila.TextoNulo(lector, "email"),
            Estado = LectorFila.Texto(lector, "estado"),
            FechaAlta = LectorFila.Fecha(lector, "fecha_alta"),
            FechaBaja = LectorFila.FechaNula(lector, "fecha_baja")
        };
    }
}
