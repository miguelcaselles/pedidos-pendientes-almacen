namespace PedidosPendientes.Core.Entities;

/// <summary>
/// Respuesta del proveedor recogida mediante el enlace firmado (HMAC) del correo
/// de reclamación. Puede ser global para todo el documento o por línea (referenciada
/// por Material, ya que el almacén no maneja "Posición").
/// </summary>
public class ProviderResponse
{
    public int Id { get; set; }

    public int? ProveedorId { get; set; }
    public string? ProveedorNombre { get; set; }

    public string DocumentoCompras { get; set; } = string.Empty;

    /// <summary>Si null =&gt; respuesta global del documento; si tiene valor =&gt; respuesta de esa línea (material).</summary>
    public string? Material { get; set; }

    /// <summary>
    /// Estado notificado por el proveedor. Valores:
    /// registrado_entregado | en_falta | envio_parcial | pendiente_fabricacion |
    /// anulado | fecha_estimada | no_localizado | otro
    /// </summary>
    public string Estado { get; set; } = string.Empty;

    public DateOnly? FechaEstimada { get; set; }

    public string? Comentario { get; set; }

    /// <summary>Flujo de revisión interna: pendiente | revisada | aplicada.</summary>
    public string RevisionEstado { get; set; } = "pendiente";

    public string? Ip { get; set; }
    public string? UserAgent { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
