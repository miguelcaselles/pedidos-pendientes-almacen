namespace PedidosPendientes.Core.Abstractions;

/// <summary>Lectura/escritura de los parámetros operativos guardados en AppSettings.</summary>
public interface ISettingsService
{
    Task<ReclamacionParametros> GetParametrosAsync(CancellationToken ct = default);
    Task SaveParametrosAsync(ReclamacionParametros parametros, CancellationToken ct = default);
}
