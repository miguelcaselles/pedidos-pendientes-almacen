using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace PedidosPendientes.Infrastructure.Excel;

/// <summary>
/// Lectura por streaming (SAX) de la primera hoja de un .xlsx con DocumentFormat.OpenXml.
/// Compartida por todos los parsers: es robusta ante ficheros reales de SAP/NEXUS que
/// incrustan imágenes con nombres inválidos (que rompen otras librerías).
/// </summary>
internal static class ExcelSax
{
    /// <summary>Lee todas las filas de la primera hoja como texto, indexadas por columna (0-based).</summary>
    public static List<List<string>> ReadAllRows(Stream xlsx)
    {
        using var doc = SpreadsheetDocument.Open(xlsx, false);
        var wbPart = doc.WorkbookPart;
        if (wbPart?.Workbook is null) return [];

        var sheet = wbPart.Workbook.Descendants<Sheet>().FirstOrDefault();
        if (sheet?.Id?.Value is null) return [];

        var wsPart = (WorksheetPart)wbPart.GetPartById(sheet.Id!.Value);
        var sst = wbPart.SharedStringTablePart?.SharedStringTable;

        var rows = new List<List<string>>();
        using var reader = OpenXmlReader.Create(wsPart);
        while (reader.Read())
        {
            if (reader.ElementType != typeof(Row)) continue;
            rows.Add(ReadRowCells((Row)reader.LoadCurrentElement()!, sst));
        }
        return rows;
    }

    /// <summary>Lee una fila a una lista indexada por columna (0-based), respetando huecos.</summary>
    private static List<string> ReadRowCells(Row row, SharedStringTable? sst)
    {
        var values = new List<string>();
        foreach (var cell in row.Elements<Cell>())
        {
            var colIndex = ColumnIndexFromRef(cell.CellReference?.Value);
            while (values.Count <= colIndex) values.Add(string.Empty);
            values[colIndex] = GetCellText(cell, sst);
        }
        return values;
    }

    private static string GetCellText(Cell cell, SharedStringTable? sst)
    {
        var raw = cell.CellValue?.InnerText ?? string.Empty;
        if (cell.DataType?.Value == CellValues.SharedString && sst is not null)
        {
            if (int.TryParse(raw, out var idx) && idx >= 0 && idx < sst.ChildElements.Count)
                return sst.ElementAt(idx).InnerText;
        }
        if (cell.DataType?.Value == CellValues.InlineString)
            return cell.InlineString?.Text?.Text ?? cell.InnerText;
        return raw;
    }

    /// <summary>Convierte "C3" -> 2 (0-based). Ignora la parte numérica.</summary>
    private static int ColumnIndexFromRef(string? cellRef)
    {
        if (string.IsNullOrEmpty(cellRef)) return 0;
        int col = 0;
        foreach (var ch in cellRef)
        {
            if (ch is >= 'A' and <= 'Z') col = col * 26 + (ch - 'A' + 1);
            else if (ch is >= 'a' and <= 'z') col = col * 26 + (ch - 'a' + 1);
            else break;
        }
        return col - 1;
    }

    public static string Get(List<string> cells, int idx)
        => idx >= 0 && idx < cells.Count ? cells[idx] : string.Empty;

    public static string? NullIfEmpty(string s)
    {
        s = s.Trim();
        return s.Length == 0 ? null : s;
    }

    public static decimal? ParseDecimal(string s)
    {
        s = s.Trim();
        if (string.IsNullOrEmpty(s)) return null;
        // El valor crudo de OpenXML usa punto decimal (invariante).
        if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
            return d;
        // Respaldo: formato europeo con coma decimal.
        s = s.Replace(".", "", StringComparison.Ordinal).Replace(',', '.');
        if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out d))
            return d;
        return null;
    }
}
