using PedidosPendientes.Core;
using PedidosPendientes.Core.Entities;

namespace PedidosPendientes.Web.Models;

public class ConfiguracionViewModel
{
    public ReclamacionParametros Parametros { get; set; } = new();

    public IReadOnlyList<CriticidadUbicacion> Criticidades { get; set; } = [];

    public int MaterialesClasificados { get; set; }
    public int ClaseA { get; set; }
    public int ClaseB { get; set; }
    public int ClaseC { get; set; }
}
