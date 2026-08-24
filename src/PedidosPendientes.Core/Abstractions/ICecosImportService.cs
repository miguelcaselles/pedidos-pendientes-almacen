using PedidosPendientes.Core.Dtos;

namespace PedidosPendientes.Core.Abstractions;

/// <summary>
/// Importación del listado de relación CECOs - Almacenes. Cada carga es una
/// instantánea completa: sustituye los datos de la carga anterior.
/// </summary>
public interface ICecosImportService
{
    Task<UploadResult> ImportAsync(Stream xlsx, string fileName, CancellationToken ct = default);
}
