using PedidosPendientes.Core.Dtos;

namespace PedidosPendientes.Core.Abstractions;

/// <summary>
/// Importación del listado MD04 de NEXUS. Cada carga es una instantánea completa:
/// sustituye los datos de la carga anterior.
/// </summary>
public interface IMd04ImportService
{
    Task<UploadResult> ImportAsync(Stream xlsx, string fileName, CancellationToken ct = default);
}
