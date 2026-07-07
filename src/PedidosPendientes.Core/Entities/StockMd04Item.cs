namespace PedidosPendientes.Core.Entities;

/// <summary>
/// Instantánea del listado de necesidades de NEXUS (MD04) para los materiales de
/// almacenaje. Cada carga sustituye por completo la anterior (es una foto del día).
/// El semáforo marca en rojo los materiales bajo mínimo que exigen pedido.
/// </summary>
public class StockMd04Item
{
    public int Id { get; set; }

    /// <summary>Código de material NEXUS/SAP.</summary>
    public string Material { get; set; } = string.Empty;

    public string? TextoBreve { get; set; }

    public string? Almacen { get; set; }

    /// <summary>Estado del semáforo: "rojo" | "amarillo" | "verde".</summary>
    public string Semaforo { get; set; } = "rojo";

    /// <summary>Stock disponible según MD04.</summary>
    public decimal? Stock { get; set; }

    /// <summary>Punto de pedido / stock mínimo.</summary>
    public decimal? PuntoPedido { get; set; }

    /// <summary>Consumo medio (si el listado lo incluye).</summary>
    public decimal? ConsumoMedio { get; set; }

    /// <summary>Momento de la carga a la que pertenece esta fila.</summary>
    public DateTimeOffset CargadoAt { get; set; }
}
