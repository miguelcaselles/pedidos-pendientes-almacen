using System.Globalization;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using PedidosPendientes.Core.Abstractions;
using PedidosPendientes.Core.Dtos;

namespace PedidosPendientes.Infrastructure.Excel;

/// <summary>
/// Parser del Excel de pedidos pendientes del almacén usando DocumentFormat.OpenXml
/// en modo lectura por streaming (SAX). Es robusto ante ficheros reales de SAP que
/// incrustan imágenes/logos (que rompen otras librerías). Detecta las columnas por
/// cabecera (tolerante a mayúsculas, acentos y abreviaturas) y, si no las reconoce,
/// usa un mapa posicional de respaldo con el orden conocido del fichero de almacén.
/// </summary>
public partial class ExcelOrderParser : IExcelOrderParser
{
    // Patrones de detección por cabecera (coincidencia parcial, ignora mayúsculas).
    private static readonly Dictionary<string, Regex> ColumnPatterns = new()
    {
        ["documentoCompras"] = DocumentoRegex(),
        ["expedienteAdministrativo"] = ExpedienteRegex(),
        ["centroCoste"] = CentroCosteRegex(),
        ["material"] = MaterialRegex(),
        ["tipoImputacion"] = TipoImputacionRegex(),
        ["textoBreve"] = TextoBreveRegex(),
        ["numMaterialProveedor"] = MatProvRegex(),
        ["fechaDocumento"] = FechaRegex(),
        ["historialEntrega"] = HistorialRegex(),
        ["proveedor"] = ProveedorRegex(),
        ["referenciaProveedor"] = ReferenciaRegex(),
        ["almacen"] = AlmacenRegex(),
        ["porEntregarCantidad"] = PorEntregarRegex(),
    };

    // Mapa posicional de respaldo (orden real del Excel de almacén).
    private static readonly Dictionary<string, int> FallbackColumnMap = new()
    {
        ["documentoCompras"] = 0,
        ["expedienteAdministrativo"] = 1,
        ["centroCoste"] = 2,
        ["material"] = 3,
        ["tipoImputacion"] = 4,
        ["textoBreve"] = 5,
        ["numMaterialProveedor"] = 6,
        ["fechaDocumento"] = 7,
        ["historialEntrega"] = 8,
        ["proveedor"] = 9,
        ["referenciaProveedor"] = 10,
        ["almacen"] = 11,
        ["porEntregarCantidad"] = 12,
    };

    public IReadOnlyList<ParsedOrder> Parse(Stream xlsx)
    {
        using var doc = SpreadsheetDocument.Open(xlsx, false);
        var wbPart = doc.WorkbookPart;
        if (wbPart?.Workbook is null) return [];

        var sheet = wbPart.Workbook.Descendants<Sheet>().FirstOrDefault();
        if (sheet?.Id?.Value is null) return [];

        var wsPart = (WorksheetPart)wbPart.GetPartById(sheet.Id!.Value);
        var sst = wbPart.SharedStringTablePart?.SharedStringTable;

        // 1ª pasada: leer la primera fila (cabeceras) y construir el mapa de columnas.
        Dictionary<string, int>? map = null;
        var result = new List<ParsedOrder>();

        using var reader = OpenXmlReader.Create(wsPart);
        while (reader.Read())
        {
            if (reader.ElementType != typeof(Row)) continue;

            var cells = ReadRowCells((Row)reader.LoadCurrentElement()!, sst);

            if (map is null)
            {
                map = DetectColumns(cells);
                continue; // la primera fila es la cabecera
            }

            if (!map.TryGetValue("documentoCompras", out var docCol)) return [];
            var doc0 = Get(cells, docCol).Trim();
            if (string.IsNullOrEmpty(doc0)) continue;

            var rawProveedor = Get(cells, Idx(map, "proveedor")).Trim();
            var (provCodigo, provNombre) = SplitProveedor(rawProveedor);
            var historial = Get(cells, Idx(map, "historialEntrega")).Trim();

            result.Add(new ParsedOrder
            {
                DocumentoCompras = doc0,
                ExpedienteAdministrativo = NullIfEmpty(Get(cells, Idx(map, "expedienteAdministrativo"))),
                CentroCoste = NullIfEmpty(Get(cells, Idx(map, "centroCoste"))),
                Material = Get(cells, Idx(map, "material")).Trim(),
                TipoImputacion = NullIfEmpty(Get(cells, Idx(map, "tipoImputacion"))),
                TextoBreve = NullIfEmpty(Get(cells, Idx(map, "textoBreve"))),
                NumMaterialProveedor = NullIfEmpty(Get(cells, Idx(map, "numMaterialProveedor"))),
                FechaDocumento = ParseDate(Get(cells, Idx(map, "fechaDocumento"))),
                TieneHistorialEntrega = historial.Length > 0,
                ProveedorCodigo = provCodigo,
                ProveedorNombre = provNombre,
                ReferenciaProveedor = NullIfEmpty(Get(cells, Idx(map, "referenciaProveedor"))),
                Almacen = NullIfEmpty(Get(cells, Idx(map, "almacen"))),
                PorEntregarCantidad = ParseDecimal(Get(cells, Idx(map, "porEntregarCantidad"))),
            });
        }

        return result;
    }

    /// <summary>Lee una fila a un array indexado por columna (0-based), respetando huecos.</summary>
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

    private static Dictionary<string, int> DetectColumns(List<string> headerCells)
    {
        var headers = headerCells.Select(h => h.Trim()).ToList();
        var map = new Dictionary<string, int>();
        foreach (var (field, pattern) in ColumnPatterns)
        {
            var idx = headers.FindIndex(h => pattern.IsMatch(h));
            if (idx != -1) map[field] = idx;
        }

        // Necesitamos al menos documento + material para fiarnos de las cabeceras.
        if (!map.ContainsKey("documentoCompras") || !map.ContainsKey("material"))
            return new Dictionary<string, int>(FallbackColumnMap);

        return map;
    }

    private static int Idx(Dictionary<string, int> map, string field)
        => map.TryGetValue(field, out var i) ? i : -1;

    private static string Get(List<string> cells, int idx)
        => idx >= 0 && idx < cells.Count ? cells[idx] : string.Empty;

    private static string? NullIfEmpty(string s)
    {
        s = s.Trim();
        return s.Length == 0 ? null : s;
    }

    /// <summary>Separa "2000004761 BECTON DICKINSON, S.A." en (código, nombre).</summary>
    private static (string? codigo, string? nombre) SplitProveedor(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return (null, null);
        var m = LeadingDigitsRegex().Match(raw);
        if (m.Success)
        {
            var codigo = m.Groups[1].Value;
            var nombre = raw[m.Length..].Trim();
            return (codigo, string.IsNullOrEmpty(nombre) ? raw : nombre);
        }
        return (null, raw);
    }

    private static DateOnly ParseDate(string str)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        str = str.Trim();
        if (string.IsNullOrEmpty(str)) return today;

        // Número de serie de Excel (fechas almacenadas como número).
        if (double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var serial)
            && serial > 1000)
        {
            try { return DateOnly.FromDateTime(DateTime.FromOADate(serial)); }
            catch { /* sigue */ }
        }

        // ISO (incluye "2022-07-28T22:00:44.000Z" de la muestra).
        if (DateTime.TryParse(str, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var iso))
            return DateOnly.FromDateTime(iso);

        // Formatos europeos dd.MM.yyyy / dd/MM/yyyy.
        string[] formats = ["dd.MM.yyyy", "d.M.yyyy", "dd/MM/yyyy", "d/M/yyyy"];
        if (DateTime.TryParseExact(str, formats, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var eu))
            return DateOnly.FromDateTime(eu);

        return today;
    }

    private static decimal? ParseDecimal(string s)
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

    [GeneratedRegex(@"documento\s*compra", RegexOptions.IgnoreCase)]
    private static partial Regex DocumentoRegex();

    [GeneratedRegex(@"expediente|expedient", RegexOptions.IgnoreCase)]
    private static partial Regex ExpedienteRegex();

    [GeneratedRegex(@"centro\s*de\s*coste|centro\s*coste", RegexOptions.IgnoreCase)]
    private static partial Regex CentroCosteRegex();

    [GeneratedRegex(@"^\s*material\s*$|\bmaterial\b", RegexOptions.IgnoreCase)]
    private static partial Regex MaterialRegex();

    [GeneratedRegex(@"tipo\s*de\s*imputaci|imputaci", RegexOptions.IgnoreCase)]
    private static partial Regex TipoImputacionRegex();

    [GeneratedRegex(@"texto\s*breve", RegexOptions.IgnoreCase)]
    private static partial Regex TextoBreveRegex();

    [GeneratedRegex(@"n[oº°ú]?\.?\s*mat(?:e(?:rial)?)?\s*(?:de\s*)?(?:prov|\.?\s*prov)|mat.*prov", RegexOptions.IgnoreCase)]
    private static partial Regex MatProvRegex();

    [GeneratedRegex(@"fecha\s*doc", RegexOptions.IgnoreCase)]
    private static partial Regex FechaRegex();

    [GeneratedRegex(@"historial", RegexOptions.IgnoreCase)]
    private static partial Regex HistorialRegex();

    // Importante: debe casar "Proveedor/Centro suministrador" (col. 9) y NO
    // "Nº material de proveedor" (col. 6). Por eso exige "/centro", "suministrador"
    // o que empiece por "proveedor".
    [GeneratedRegex(@"proveedor\s*/\s*centro|proveedor.*suministrador|^proveedor\b", RegexOptions.IgnoreCase)]
    private static partial Regex ProveedorRegex();

    [GeneratedRegex(@"referencia\s*del?\s*proveedor|referencia.*prov", RegexOptions.IgnoreCase)]
    private static partial Regex ReferenciaRegex();

    [GeneratedRegex(@"almac[eé]n", RegexOptions.IgnoreCase)]
    private static partial Regex AlmacenRegex();

    [GeneratedRegex(@"por\s*entregar", RegexOptions.IgnoreCase)]
    private static partial Regex PorEntregarRegex();

    [GeneratedRegex(@"^(\d+)")]
    private static partial Regex LeadingDigitsRegex();
}
