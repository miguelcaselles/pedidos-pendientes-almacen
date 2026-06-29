using PedidosPendientes.Core.Entities;

namespace PedidosPendientes.Web.Models;

public class OrdersViewModel
{
    public IReadOnlyList<Order> Orders { get; set; } = [];
    public string? Search { get; set; }

    // Indicadores
    public int TotalPendientes { get; set; }
    public int ParaReclamar { get; set; }
    public int Reclamados { get; set; }
    public int RecibidosHoy { get; set; }
    public DateTimeOffset? UltimaCarga { get; set; }
}
