using System.Text.RegularExpressions;
using PedidosPendientes.Core.Dtos;

namespace PedidosPendientes.Infrastructure.Excel;

/// <summary>
/// Parser del listado MD04 de NEXUS (necesidades de materiales).
/// Formato real: Status ("XOO" rojo / "OXO" amarillo / "OOX" verde), Material,
/// Área pl.nec. (p. ej. "L025-1000"), Denominación, ME3 (pedido fuera de plazo de
/// entrega) y ME6 (por debajo de stock de seguridad).
/// Tolerante a variantes: si el fichero trae columnas de stock/punto de pedido las
/// conserva como informativas, y si no hay columna Status deduce el semáforo del
/// stock (respaldo para formatos antiguos).
/// </summary>
public static partial class Md04ExcelParser
{
    public static IReadOnlyList<ParsedMd04Item> Parse(Stream xlsx)
    {
        var rows = ExcelSax.ReadAllRows(xlsx);
        if (rows.Count == 0) return [];

        var headers = rows[0].Select(h => h.Trim()).ToList();
        int Find(Regex rx) => headers.FindIndex(h => rx.IsMatch(h));

        var colStatus = Find(StatusRegex());
        var colMaterial = Find(MaterialRegex());
        var colArea = Find(AreaRegex());
        var colTexto = Find(TextoRegex());
        var colMe3 = Find(Me3Regex());
        var colMe6 = Find(Me6Regex());
        var colAlmacen = Find(AlmacenRegex());
        var colSemaforo = Find(SemaforoRegex());
        var colStock = Find(StockRegex());
        var colPuntoPedido = Find(PuntoPedidoRegex());
        var colConsumo = Find(ConsumoRegex());

        // Sin columna de material reconocible el fichero no es el MD04: se rechaza
        // en vez de importar basura por posición.
        if (colMaterial == -1)
            throw new FormatException("No se reconocen las cabeceras del listado MD04 (falta la columna Material).");

        var result = new List<ParsedMd04Item>();
        foreach (var cells in rows.Skip(1))
        {
            var material = ExcelSax.Get(cells, colMaterial).Trim();
            if (string.IsNullOrEmpty(material)) continue;

            var status = ExcelSax.NullIfEmpty(ExcelSax.Get(cells, colStatus));
            var stock = ExcelSax.ParseDecimal(ExcelSax.Get(cells, colStock));
            var puntoPedido = ExcelSax.ParseDecimal(ExcelSax.Get(cells, colPuntoPedido));
            var semaforoRaw = status ?? ExcelSax.Get(cells, colSemaforo).Trim();

            result.Add(new ParsedMd04Item
            {
                Material = material,
                TextoBreve = ExcelSax.NullIfEmpty(ExcelSax.Get(cells, colTexto)),
                Almacen = ExcelSax.NullIfEmpty(ExcelSax.Get(cells, colAlmacen)),
                Semaforo = NormalizeSemaforo(semaforoRaw, stock, puntoPedido),
                StatusNexus = status,
                AreaPlanificacion = ExcelSax.NullIfEmpty(ExcelSax.Get(cells, colArea)),
                PedidosFueraPlazo = ParseCount(ExcelSax.Get(cells, colMe3)),
                BajoStockSeguridad = ParseCount(ExcelSax.Get(cells, colMe6)),
                Stock = stock,
                PuntoPedido = puntoPedido,
                ConsumoMedio = ExcelSax.ParseDecimal(ExcelSax.Get(cells, colConsumo)),
            });
        }
        return result;
    }

    /// <summary>Contador de excepciones ME3/ME6: entero, vacío = ninguna.</summary>
    private static int? ParseCount(string s)
    {
        s = s.Trim();
        if (s.Length == 0) return null;
        return int.TryParse(s, out var n) ? n : null;
    }

    /// <summary>
    /// Normaliza el semáforo a rojo/amarillo/verde. Reconoce primero el Status de
    /// NEXUS ("XOO"/"OXO"/"OOX": la posición de la X es el color); después texto
    /// ("rojo", "red"...); y como respaldo lo deduce del stock (formatos antiguos).
    /// </summary>
    private static string NormalizeSemaforo(string raw, decimal? stock, decimal? puntoPedido)
    {
        var trimmed = raw.Trim();
        if (NexusStatusRegex().IsMatch(trimmed))
        {
            var pos = trimmed.ToUpperInvariant().IndexOf('X');
            return pos switch { 0 => "rojo", 1 => "amarillo", _ => "verde" };
        }

        var s = trimmed.ToLowerInvariant();
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

    [GeneratedRegex(@"^[XOxo]{3}$")]
    private static partial Regex NexusStatusRegex();

    [GeneratedRegex(@"^\s*status\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex StatusRegex();

    [GeneratedRegex(@"[aá]rea\s*pl", RegexOptions.IgnoreCase)]
    private static partial Regex AreaRegex();

    [GeneratedRegex(@"^\s*ME\s*3\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex Me3Regex();

    [GeneratedRegex(@"^\s*ME\s*6\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex Me6Regex();

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
