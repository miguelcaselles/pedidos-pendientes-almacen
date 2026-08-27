using PedidosPendientes.Core.Entities;

namespace PedidosPendientes.Web.Models;

public class SemaforoViewModel
{
    public IReadOnlyList<SemaforoRow> Rows { get; set; } = [];

    public DateTimeOffset? CargadoAt { get; set; }

    /// <summary>Ámbito activo: "almacenaje" | "noalmacenaje".</summary>
    public string Ambito { get; set; } = "almacenaje";

    public int TotalAlmacenaje { get; set; }
    public int TotalNoAlmacenaje { get; set; }

    public int Rojos { get; set; }
    public int RojosSinPedido { get; set; }
    public int Amarillos { get; set; }
    public int Verdes { get; set; }

    /// <summary>Materiales con excepción ME3 (pedido fuera de plazo de entrega).</summary>
    public int FueraPlazo { get; set; }

    /// <summary>Materiales con excepción ME6 (por debajo de stock de seguridad).</summary>
    public int BajoSeguridad { get; set; }

    /// <summary>Filtro activo: "sinpedido" | "rojos" | "fueraplazo" | "bajoseguridad" | "todos".</summary>
    public string Filtro { get; set; } = "sinpedido";

    /// <summary>El listado trae columnas informativas de stock (formatos antiguos).</summary>
    public bool MuestraStock { get; set; }
}

public class SemaforoRow
{
    public required StockMd04Item Item { get; init; }

    /// <summary>Pedidos lanzados (no recibidos ni anulados) para este material.</summary>
    public IReadOnlyList<Order> PedidosPendientes { get; init; } = [];

    public bool TienePedido => PedidosPendientes.Count > 0;

    /// <summary>
    /// Realmente sin pedido lanzado: no hay líneas ME2N del material Y el propio
    /// MD04 tampoco señala pedido fuera de plazo (ME3 &gt; 0 significa que SAP ya
    /// conoce un pedido, aunque no aparezca en el ME2N cargado).
    /// </summary>
    public bool SinPedido => !TienePedido && (Item.PedidosFueraPlazo ?? 0) == 0;
}
