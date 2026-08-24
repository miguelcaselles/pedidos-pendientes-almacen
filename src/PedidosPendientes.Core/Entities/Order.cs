namespace PedidosPendientes.Core.Entities;

/// <summary>
/// Línea de pedido pendiente del almacén de suministros.
/// Importada desde el Excel de SAP (carga manual, no destructiva).
/// Clave única de negocio: DocumentoCompras + Material (el almacén no tiene "Posición").
/// </summary>
public class Order
{
    public int Id { get; set; }

    // --- Datos que provienen del Excel de almacén ---

    /// <summary>Documento de compras (cabecera del pedido en SAP). Excel col. 0.</summary>
    public string DocumentoCompras { get; set; } = string.Empty;

    /// <summary>Nº de expediente administrativo (contratación pública). Excel col. 1.</summary>
    public string? ExpedienteAdministrativo { get; set; }

    /// <summary>Centro de coste. Excel col. 2.</summary>
    public string? CentroCoste { get; set; }

    /// <summary>Código de material SAP (interno). Excel col. 3. Forma parte de la clave única.</summary>
    public string Material { get; set; } = string.Empty;

    /// <summary>Tipo de imputación. Excel col. 4.</summary>
    public string? TipoImputacion { get; set; }

    /// <summary>Texto breve / descripción del artículo. Excel col. 5.</summary>
    public string? TextoBreve { get; set; }

    /// <summary>Nº de material del proveedor (referencia comercial). Excel col. 6.</summary>
    public string? NumMaterialProveedor { get; set; }

    /// <summary>Fecha del documento de pedido. Excel col. 7.</summary>
    public DateOnly FechaDocumento { get; set; }

    /// <summary>Indica si la línea tiene historial de entrega/orden. Excel col. 8 (no vacía).</summary>
    public bool TieneHistorialEntrega { get; set; }

    /// <summary>Código del proveedor (parte numérica de col. 9 "código nombre").</summary>
    public string? ProveedorCodigo { get; set; }

    /// <summary>Nombre del proveedor (parte textual de col. 9).</summary>
    public string? ProveedorNombre { get; set; }

    /// <summary>Referencia del proveedor. Excel col. 10.</summary>
    public string? ReferenciaProveedor { get; set; }

    /// <summary>Almacén destino. Excel col. 11.</summary>
    public string? Almacen { get; set; }

    /// <summary>Cantidad total pedida ("Cantidad de pedido" del ME2N).</summary>
    public decimal? CantidadPedido { get; set; }

    /// <summary>Cantidad pendiente de entregar. Excel col. 12.</summary>
    public decimal? PorEntregarCantidad { get; set; }

    /// <summary>
    /// Nº de contrato marco de la línea ("Contrato marco" del ME2N). Si viene,
    /// el pedido está sujeto a acuerdo marco (base de la auditoría de proveedores).
    /// </summary>
    public string? ContratoMarco { get; set; }

    // --- Estado de gestión (NO viene del Excel; se conserva entre cargas) ---

    public bool Recibido { get; set; }
    public DateTimeOffset? RecibidoAt { get; set; }
    public decimal? CantidadRecibida { get; set; }

    public bool Reclamado { get; set; }
    public DateTimeOffset? ReclamadoAt { get; set; }
    public int ReclamadoCount { get; set; }

    public bool EnFalta { get; set; }
    public DateTimeOffset? EnFaltaAt { get; set; }
    public bool PausaReclamacion { get; set; }

    public bool Anulado { get; set; }
    public DateTimeOffset? AnuladoAt { get; set; }

    public string? Comentarios { get; set; }
    public string? Incidencia { get; set; }

    // --- Auditoría ---

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Momento de la última carga de Excel que tocó esta línea.</summary>
    public DateTimeOffset LastUploadAt { get; set; }
}
