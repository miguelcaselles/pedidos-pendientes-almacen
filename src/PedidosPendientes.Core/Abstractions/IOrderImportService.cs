using PedidosPendientes.Core.Dtos;

namespace PedidosPendientes.Core.Abstractions;

/// <summary>
/// Importación no destructiva de pedidos desde el Excel de almacén.
/// Hace upsert por (DocumentoCompras + Material) conservando el estado de gestión
/// (recibido, reclamado, anulado, comentarios...) ya registrado en la base de datos.
/// </summary>
public interface IOrderImportService
{
    Task<UploadResult> ImportAsync(Stream xlsx, CancellationToken ct = default);
}
