using AppIsp.Modelo;

namespace AppIsp.Controlador;

/// <summary>Números del inicio. Cada tarjeta es una cuenta simple sobre la base.</summary>
public static class PanelControlador
{
    public sealed class TarjetaResumen
    {
        public string Titulo { get; set; } = "";
        public int Valor { get; set; }
    }

    public static List<TarjetaResumen> ObtenerTarjetas()
    {
        PermisosControlador.ExigirSesion();
        var tarjetas = new List<TarjetaResumen>();

        if (PermisosControlador.AreaComercial)
        {
            tarjetas.Add(new TarjetaResumen
            {
                Titulo = "Clientes activos",
                Valor = Contar("SELECT COUNT(*) FROM clientes WHERE estado = 'ACTIVO'")
            });
        }
        else
        {
            tarjetas.Add(new TarjetaResumen
            {
                Titulo = "Servicios activos",
                Valor = Contar("SELECT COUNT(*) FROM servicios WHERE estado = 'ACTIVO'")
            });
        }

        var filtroTicket = PermisosControlador.EsRolComercial
            ? " AND tipo = 'COMERCIAL'"
            : PermisosControlador.EsRolTecnico ? " AND tipo = 'TECNICO'" : "";

        tarjetas.Add(new TarjetaResumen
        {
            Titulo = "Tickets abiertos",
            Valor = Contar("SELECT COUNT(*) FROM tickets WHERE estado IN ('ABIERTO','EN_PROCESO')" + filtroTicket)
        });

        tarjetas.Add(new TarjetaResumen
        {
            Titulo = "Instalaciones pendientes",
            Valor = Contar("SELECT COUNT(*) FROM instalaciones WHERE estado IN ('PENDIENTE','EN_PROCESO')")
        });

        if (PermisosControlador.Facturar)
        {
            tarjetas.Add(new TarjetaResumen
            {
                Titulo = "Facturas pendientes",
                Valor = Contar("SELECT COUNT(*) FROM facturas WHERE estado = 'PENDIENTE'")
            });
        }
        else if (PermisosControlador.OperarRouters)
        {
            tarjetas.Add(new TarjetaResumen
            {
                Titulo = "Routers en falla",
                Valor = Contar("SELECT COUNT(*) FROM routers WHERE estado = 'EN_FALLA'")
            });
        }

        return tarjetas;
    }

    private static int Contar(string sql)
    {
        return Convert.ToInt32(ConexionMySql.Escalar(sql));
    }
}
