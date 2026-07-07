namespace PedidosPendientes.Core.Dtos;

/// <summary>Fila parseada del listado MD04 de NEXUS (necesidades de almacenaje).</summary>
public class ParsedMd04Item
{
    public string Material { get; set; } = string.Empty;
    public string? TextoBreve { get; set; }
    public string? Almacen { get; set; }

    /// <summary>"rojo" | "amarillo" | "verde".</summary>
    public string Semaforo { get; set; } = "rojo";

    public decimal? Stock { get; set; }
    public decimal? PuntoPedido { get; set; }
    public decimal? ConsumoMedio { get; set; }
}
