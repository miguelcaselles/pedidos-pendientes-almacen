using System.Text.RegularExpressions;
using PedidosPendientes.Core.Dtos;

namespace PedidosPendientes.Infrastructure.Excel;

/// <summary>
/// Parser del listado MD04 de NEXUS (necesidades de materiales de almacenaje).
/// Tolerante al formato: detecta por cabecera el material, la descripción, el almacén,
/// el semáforo/estado, el stock, el punto de pedido y el consumo. Si el fichero no trae
/// columna de semáforo, lo deduce: stock &lt;= 0 o stock &lt; punto de pedido =&gt; rojo.
/// </summary>
public static partial class Md04ExcelParser
{
    public static IReadOnlyList<ParsedMd04Item> Parse(Stream xlsx)
    {
        var rows = ExcelSax.ReadAllRows(xlsx);
        if (rows.Count == 0) return [];

        var headers = rows[0].Select(h => h.Trim()).ToList();
        int Find(Regex rx) => headers.FindIndex(h => rx.IsMatch(h));

        var colMaterial = Find(MaterialRegex());
        var colTexto = Find(TextoRegex());
        var colAlmacen = Find(AlmacenRegex());
        var colSemaforo = Find(SemaforoRegex());
        var colStock = Find(StockRegex());
        var colPuntoPedido = Find(PuntoPedidoRegex());
        var colConsumo = Find(ConsumoRegex());

        // Respaldo posicional mínimo: material en la primera columna.
        if (colMaterial == -1) colMaterial = 0;

        var result = new List<ParsedMd04Item>();
        foreach (var cells in rows.Skip(1))
        {
            var material = ExcelSax.Get(cells, colMaterial).Trim();
            if (string.IsNullOrEmpty(material)) continue;

            var stock = ExcelSax.ParseDecimal(ExcelSax.Get(cells, colStock));
            var puntoPedido = ExcelSax.ParseDecimal(ExcelSax.Get(cells, colPuntoPedido));
            var semaforoRaw = ExcelSax.Get(cells, colSemaforo).Trim();

            result.Add(new ParsedMd04Item
            {
                Material = material,
                TextoBreve = ExcelSax.NullIfEmpty(ExcelSax.Get(cells, colTexto)),
                Almacen = ExcelSax.NullIfEmpty(ExcelSax.Get(cells, colAlmacen)),
                Semaforo = NormalizeSemaforo(semaforoRaw, stock, puntoPedido),
                Stock = stock,
                PuntoPedido = puntoPedido,
                ConsumoMedio = ExcelSax.ParseDecimal(ExcelSax.Get(cells, colConsumo)),
            });
        }
        return result;
    }

    /// <summary>Normaliza el semáforo a rojo/amarillo/verde; si no hay columna, lo deduce del stock.</summary>
    private static string NormalizeSemaforo(string raw, decimal? stock, decimal? puntoPedido)
    {
        var s = raw.ToLowerInvariant();
        if (s.Contains("rojo") || s.Contains("red") || s.Contains("crit")) return "rojo";
        if (s.Contains("amarillo") || s.Contains("yellow") || s.Contains("aviso")) return "amarillo";
        if (s.Contains("verde") || s.Contains("green") || s.Contains("ok")) return "verde";

        if (stock is not null)
        {
            if (stock <= 0) return "rojo";
            if (puntoPedido is not null)
            {
                if (stock < puntoPedido) return "rojo";
                if (stock < puntoPedido * 1.25m) return "amarillo";
            }
            return "verde";
        }

        // Sin semáforo ni stock: si el material aparece en el MD04 es que tiene necesidad.
        return "rojo";
    }

    [GeneratedRegex(@"^\s*material\s*$|\bmaterial\b|c[oó]digo", RegexOptions.IgnoreCase)]
    private static partial Regex MaterialRegex();

    [GeneratedRegex(@"texto|descripci[oó]n|denominaci[oó]n", RegexOptions.IgnoreCase)]
    private static partial Regex TextoRegex();

    [GeneratedRegex(@"almac[eé]n", RegexOptions.IgnoreCase)]
    private static partial Regex AlmacenRegex();

    [GeneratedRegex(@"sem[aá]foro|estado|color|situaci[oó]n", RegexOptions.IgnoreCase)]
    private static partial Regex SemaforoRegex();

    [GeneratedRegex(@"stock|disponible|existenc", RegexOptions.IgnoreCase)]
    private static partial Regex StockRegex();

    [GeneratedRegex(@"punto\s*(de\s*)?pedido|m[ií]nimo|reorden", RegexOptions.IgnoreCase)]
    private static partial Regex PuntoPedidoRegex();

    [GeneratedRegex(@"consumo", RegexOptions.IgnoreCase)]
    private static partial Regex ConsumoRegex();
}
