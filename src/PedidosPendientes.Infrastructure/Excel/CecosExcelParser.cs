using System.Text.RegularExpressions;
using PedidosPendientes.Core.Dtos;

namespace PedidosPendientes.Infrastructure.Excel;

/// <summary>
/// Parser del listado "Relación CECOs - Almacenes" de Suministros.
/// Formato real: Centro, Almacén, Denominación-almacén, Datos 1-4 (se ignoran),
/// Centro de coste, Denominación, Descripción. Detección por cabecera con
/// respaldo posicional en el orden conocido.
/// </summary>
public static partial class CecosExcelParser
{
    public static IReadOnlyList<ParsedAlmacenCeco> Parse(Stream xlsx)
    {
        var rows = ExcelSax.ReadAllRows(xlsx);
        if (rows.Count == 0) return [];

        var headers = rows[0].Select(h => h.Trim()).ToList();
        int Find(Regex rx) => headers.FindIndex(h => rx.IsMatch(h));

        var colCentro = Find(CentroRegex());
        var colAlmacen = Find(AlmacenRegex());
        var colDenAlmacen = Find(DenominacionAlmacenRegex());
        var colCentroCoste = Find(CentroCosteRegex());
        var colDenCeco = Find(DenominacionRegex());
        var colDescripcion = Find(DescripcionRegex());

        // Respaldo posicional (orden real del fichero).
        if (colAlmacen == -1 || colCentroCoste == -1)
        {
            colCentro = 0; colAlmacen = 1; colDenAlmacen = 2;
            colCentroCoste = 7; colDenCeco = 8; colDescripcion = 9;
        }

        var result = new List<ParsedAlmacenCeco>();
        foreach (var cells in rows.Skip(1))
        {
            var almacen = ExcelSax.Get(cells, colAlmacen).Trim();
            if (string.IsNullOrEmpty(almacen)) continue;

            result.Add(new ParsedAlmacenCeco
            {
                Centro = ExcelSax.Get(cells, colCentro).Trim(),
                Almacen = almacen,
                DenominacionAlmacen = ExcelSax.NullIfEmpty(ExcelSax.Get(cells, colDenAlmacen)),
                CentroCoste = ExcelSax.NullIfEmpty(ExcelSax.Get(cells, colCentroCoste)),
                DenominacionCentroCoste = ExcelSax.NullIfEmpty(ExcelSax.Get(cells, colDenCeco)),
                Descripcion = ExcelSax.NullIfEmpty(ExcelSax.Get(cells, colDescripcion)),
            });
        }
        return result;
    }

    [GeneratedRegex(@"^\s*centro\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex CentroRegex();

    [GeneratedRegex(@"^\s*almac[eé]n\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex AlmacenRegex();

    [GeneratedRegex(@"denominaci[oó]n.*almac[eé]n", RegexOptions.IgnoreCase)]
    private static partial Regex DenominacionAlmacenRegex();

    [GeneratedRegex(@"centro\s*de\s*coste", RegexOptions.IgnoreCase)]
    private static partial Regex CentroCosteRegex();

    [GeneratedRegex(@"^\s*denominaci[oó]n\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex DenominacionRegex();

    [GeneratedRegex(@"^\s*descripci[oó]n\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex DescripcionRegex();
}
