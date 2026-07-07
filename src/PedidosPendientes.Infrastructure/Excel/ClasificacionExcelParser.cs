using System.Text.RegularExpressions;
using PedidosPendientes.Core.Dtos;

namespace PedidosPendientes.Infrastructure.Excel;

/// <summary>
/// Parser del listado de clasificación A/B/C por código NEXUS.
/// Espera al menos una columna de material/código y una de clase (A, B o C);
/// si no reconoce cabeceras, asume material en la 1ª columna y clase en la 2ª.
/// </summary>
public static partial class ClasificacionExcelParser
{
    public static IReadOnlyList<ParsedClasificacion> Parse(Stream xlsx)
    {
        var rows = ExcelSax.ReadAllRows(xlsx);
        if (rows.Count == 0) return [];

        var headers = rows[0].Select(h => h.Trim()).ToList();
        var colMaterial = headers.FindIndex(h => MaterialRegex().IsMatch(h));
        var colClase = headers.FindIndex(h => ClaseRegex().IsMatch(h));

        var skipHeader = true;
        if (colMaterial == -1 || colClase == -1)
        {
            colMaterial = 0;
            colClase = 1;
            // Si la primera fila ya parece un dato (clase A/B/C en la col. 1), no la saltamos.
            skipHeader = !EsClase(ExcelSax.Get(rows[0], colClase));
        }

        var result = new List<ParsedClasificacion>();
        foreach (var cells in rows.Skip(skipHeader ? 1 : 0))
        {
            var material = ExcelSax.Get(cells, colMaterial).Trim();
            var clase = ExcelSax.Get(cells, colClase).Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(material) || !EsClase(clase)) continue;

            result.Add(new ParsedClasificacion { Material = material, Clase = clase });
        }
        return result;
    }

    private static bool EsClase(string s) => s.Trim().ToUpperInvariant() is "A" or "B" or "C";

    [GeneratedRegex(@"material|c[oó]digo|nexus", RegexOptions.IgnoreCase)]
    private static partial Regex MaterialRegex();

    [GeneratedRegex(@"clase|clasificaci[oó]n|abc|urgencia|prioridad", RegexOptions.IgnoreCase)]
    private static partial Regex ClaseRegex();
}
