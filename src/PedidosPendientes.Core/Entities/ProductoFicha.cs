namespace PedidosPendientes.Core.Entities;

/// <summary>
/// Ficha de producto del catálogo: datos descriptivos, materiales alternativos y
/// presupuesto. Se busca por código NEXUS o por descripción. Los documentos
/// (ficha técnica del material) se guardan como adjuntos.
/// </summary>
public class ProductoFicha
{
    public int Id { get; set; }

    /// <summary>Código de material NEXUS/SAP. Único.</summary>
    public string Material { get; set; } = string.Empty;

    /// <summary>Denominación del artículo.</summary>
    public string? Denominacion { get; set; }

    /// <summary>Descripción ampliada / datos de la ficha.</summary>
    public string? DescripcionAmpliada { get; set; }

    /// <summary>Materiales alternativos (códigos o descripciones, texto libre).</summary>
    public string? MaterialesAlternativos { get; set; }

    /// <summary>Precio/presupuesto unitario orientativo (€).</summary>
    public decimal? PresupuestoUnitario { get; set; }

    /// <summary>Observaciones (proveedor habitual, condiciones, etc.).</summary>
    public string? Notas { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public List<ProductoAdjunto> Adjuntos { get; set; } = [];
}
