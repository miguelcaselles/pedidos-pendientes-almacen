using PedidosPendientes.Core.Dtos;

namespace PedidosPendientes.Core.Abstractions;

/// <summary>
/// Importación del listado de clasificación A/B/C por código NEXUS.
/// Hace upsert por material (una carga nueva actualiza las clases existentes).
/// </summary>
public interface IClasificacionImportService
{
    Task<UploadResult> ImportAsync(Stream xlsx, string fileName, CancellationToken ct = default);
}
