namespace PedidosPendientes.Core.Entities;

/// <summary>
/// Instantánea del listado de necesidades de NEXUS (MD04). Se cargan dos listados
/// independientes: materiales de almacenaje y de no almacenaje (ámbito). Cada carga
/// sustituye por completo la anterior de su ámbito (es una foto del día).
/// El semáforo viene del Status de NEXUS ("XOO" rojo / "OXO" amarillo / "OOX" verde);
/// las columnas ME3/ME6 marcan las excepciones importantes: pedido fuera de plazo de
/// entrega (3) y por debajo de stock de seguridad (6). El stock numérico no se usa
/// para decidir el color (posibles descuadres), solo se muestra si viene.
/// </summary>
public class StockMd04Item
{
    public int Id { get; set; }

    /// <summary>Ámbito del listado: "almacenaje" | "noalmacenaje".</summary>
    public string Ambito { get; set; } = "almacenaje";

    /// <summary>Código de material NEXUS/SAP.</summary>
    public string Material { get; set; } = string.Empty;

    public string? TextoBreve { get; set; }

    public string? Almacen { get; set; }

    /// <summary>Estado del semáforo: "rojo" | "amarillo" | "verde".</summary>
    public string Semaforo { get; set; } = "rojo";

    /// <summary>Status de NEXUS tal cual viene en el listado (p. ej. "XOO").</summary>
    public string? StatusNexus { get; set; }

    /// <summary>Área de planificación de necesidades (p. ej. "L025-1000").</summary>
    public string? AreaPlanificacion { get; set; }

    /// <summary>Nº de excepciones ME3: pedido fuera de plazo de entrega.</summary>
    public int? PedidosFueraPlazo { get; set; }

    /// <summary>Nº de excepciones ME6: por debajo de stock de seguridad.</summary>
    public int? BajoStockSeguridad { get; set; }

    /// <summary>Stock disponible según MD04 (informativo).</summary>
    public decimal? Stock { get; set; }

    /// <summary>Punto de pedido / stock mínimo (informativo).</summary>
    public decimal? PuntoPedido { get; set; }

    /// <summary>Consumo medio (si el listado lo incluye).</summary>
    public decimal? ConsumoMedio { get; set; }

    /// <summary>Momento de la carga a la que pertenece esta fila.</summary>
    public DateTimeOffset CargadoAt { get; set; }
}
