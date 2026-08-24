namespace PedidosPendientes.Core.Entities;

/// <summary>
/// Documento adjunto a una ficha de producto (ficha técnica del material,
/// certificados, presupuestos...). El contenido se guarda en la base de datos
/// para simplificar copias de seguridad y el despliegue en granja de IIS.
/// </summary>
public class ProductoAdjunto
{
    public int Id { get; set; }

    public int ProductoFichaId { get; set; }
    public ProductoFicha? ProductoFicha { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = "application/octet-stream";

    /// <summary>Tamaño en bytes.</summary>
    public long Size { get; set; }

    public byte[] Contenido { get; set; } = [];

    public DateTimeOffset SubidoAt { get; set; }
}
