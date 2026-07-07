using PedidosPendientes.Core.Entities;

namespace PedidosPendientes.Web.Models;

public class SemaforoViewModel
{
    public IReadOnlyList<SemaforoRow> Rows { get; set; } = [];

    public DateTimeOffset? CargadoAt { get; set; }

    public int Rojos { get; set; }
    public int RojosSinPedido { get; set; }
    public int Amarillos { get; set; }
    public int Verdes { get; set; }

    /// <summary>Filtro activo: "sinpedido" (rojos sin pedido) | "rojos" | "todos".</summary>
    public string Filtro { get; set; } = "sinpedido";
}

public class SemaforoRow
{
    public required StockMd04Item Item { get; init; }

    /// <summary>Pedidos pendientes lanzados para este material (vacío = ¡sin pedido!).</summary>
    public IReadOnlyList<Order> PedidosPendientes { get; init; } = [];

    public bool TienePedido => PedidosPendientes.Count > 0;
}
