using PedidosPendientes.Core.Dtos;

namespace PedidosPendientes.Core.Abstractions;

/// <summary>
/// Parser del Excel de pedidos pendientes del almacén. Detecta las columnas por
/// cabecera (tolerante a variaciones) y devuelve las filas válidas.
/// </summary>
public interface IExcelOrderParser
{
    IReadOnlyList<ParsedOrder> Parse(Stream xlsx);
}
