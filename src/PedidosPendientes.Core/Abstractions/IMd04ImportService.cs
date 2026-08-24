using PedidosPendientes.Core.Dtos;

namespace PedidosPendientes.Core.Abstractions;

/// <summary>
/// Importación del listado MD04 de NEXUS. Se cargan dos listados independientes
/// (ámbito "almacenaje" y "noalmacenaje"); cada carga es una instantánea completa
/// que sustituye los datos de la carga anterior de su mismo ámbito.
/// </summary>
public interface IMd04ImportService
{
    Task<UploadResult> ImportAsync(Stream xlsx, string fileName, string ambito, CancellationToken ct = default);
}
