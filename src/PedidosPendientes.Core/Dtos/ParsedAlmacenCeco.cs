namespace PedidosPendientes.Core.Dtos;

/// <summary>Fila parseada del listado de relación CECOs - Almacenes.</summary>
public class ParsedAlmacenCeco
{
    public string Centro { get; set; } = string.Empty;
    public string Almacen { get; set; } = string.Empty;
    public string? DenominacionAlmacen { get; set; }
    public string? CentroCoste { get; set; }
    public string? DenominacionCentroCoste { get; set; }
    public string? Descripcion { get; set; }
}
