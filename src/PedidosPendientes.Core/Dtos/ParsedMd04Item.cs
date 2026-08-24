namespace PedidosPendientes.Core.Dtos;

/// <summary>Fila parseada del listado MD04 de NEXUS (necesidades de materiales).</summary>
public class ParsedMd04Item
{
    public string Material { get; set; } = string.Empty;
    public string? TextoBreve { get; set; }
    public string? Almacen { get; set; }

    /// <summary>"rojo" | "amarillo" | "verde".</summary>
    public string Semaforo { get; set; } = "rojo";

    /// <summary>Status de NEXUS tal cual viene ("XOO" = rojo, "OXO" = amarillo, "OOX" = verde).</summary>
    public string? StatusNexus { get; set; }

    /// <summary>Área de planificación de necesidades (p. ej. "L025-1000").</summary>
    public string? AreaPlanificacion { get; set; }

    /// <summary>Nº de excepciones ME3: pedido fuera de plazo de entrega.</summary>
    public int? PedidosFueraPlazo { get; set; }

    /// <summary>Nº de excepciones ME6: por debajo de stock de seguridad.</summary>
    public int? BajoStockSeguridad { get; set; }

    public decimal? Stock { get; set; }
    public decimal? PuntoPedido { get; set; }
    public decimal? ConsumoMedio { get; set; }
}
