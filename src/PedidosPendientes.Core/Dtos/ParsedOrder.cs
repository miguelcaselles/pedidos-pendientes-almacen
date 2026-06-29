namespace PedidosPendientes.Core.Dtos;

/// <summary>
/// Resultado de parsear una fila del Excel de almacén. Solo contiene los campos
/// que vienen del Excel; el estado de gestión se conserva en la base de datos.
/// </summary>
public class ParsedOrder
{
    public string DocumentoCompras { get; set; } = string.Empty;
    public string? ExpedienteAdministrativo { get; set; }
    public string? CentroCoste { get; set; }
    public string Material { get; set; } = string.Empty;
    public string? TipoImputacion { get; set; }
    public string? TextoBreve { get; set; }
    public string? NumMaterialProveedor { get; set; }
    public DateOnly FechaDocumento { get; set; }
    public bool TieneHistorialEntrega { get; set; }
    public string? ProveedorCodigo { get; set; }
    public string? ProveedorNombre { get; set; }
    public string? ReferenciaProveedor { get; set; }
    public string? Almacen { get; set; }
    public decimal? PorEntregarCantidad { get; set; }
}
