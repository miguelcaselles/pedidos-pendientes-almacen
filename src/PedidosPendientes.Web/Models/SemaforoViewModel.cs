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

    /// <summary>Pedidos pendientes lanzados para este material (vacío = ¡sin pedido!).</summary>
    public IReadOnlyList<Order> PedidosPendientes { get; init; } = [];

    public bool TienePedido => PedidosPendientes.Count > 0;
}
