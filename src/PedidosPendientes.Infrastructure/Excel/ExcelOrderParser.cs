using System.Globalization;
using System.Text.RegularExpressions;
using PedidosPendientes.Core.Abstractions;
using PedidosPendientes.Core.Dtos;

namespace PedidosPendientes.Infrastructure.Excel;

/// <summary>
/// Parser del Excel de pedidos pendientes del almacén (lectura SAX compartida en
/// <see cref="ExcelSax"/>). Detecta las columnas por cabecera (tolerante a mayúsculas,
/// acentos y abreviaturas) y, si no las reconoce, usa un mapa posicional de respaldo
/// con el orden conocido del fichero de almacén.
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
        ["cantidadPedido"] = CantidadPedidoRegex(),
        ["porEntregarCantidad"] = PorEntregarRegex(),
        ["contratoMarco"] = ContratoMarcoRegex(),
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
        var rows = ExcelSax.ReadAllRows(xlsx);
        if (rows.Count == 0) return [];

        var map = DetectColumns(rows[0]);
        if (!map.TryGetValue("documentoCompras", out var docCol)) return [];

        var result = new List<ParsedOrder>();
        foreach (var cells in rows.Skip(1))
        {
            var doc0 = ExcelSax.Get(cells, docCol).Trim();
            if (string.IsNullOrEmpty(doc0)) continue;

            var rawProveedor = ExcelSax.Get(cells, Idx(map, "proveedor")).Trim();
            var (provCodigo, provNombre) = SplitProveedor(rawProveedor);
            var historial = ExcelSax.Get(cells, Idx(map, "historialEntrega")).Trim();

            result.Add(new ParsedOrder
            {
                DocumentoCompras = doc0,
                ExpedienteAdministrativo = ExcelSax.NullIfEmpty(ExcelSax.Get(cells, Idx(map, "expedienteAdministrativo"))),
                CentroCoste = ExcelSax.NullIfEmpty(ExcelSax.Get(cells, Idx(map, "centroCoste"))),
                Material = ExcelSax.Get(cells, Idx(map, "material")).Trim(),
                TipoImputacion = ExcelSax.NullIfEmpty(ExcelSax.Get(cells, Idx(map, "tipoImputacion"))),
                TextoBreve = ExcelSax.NullIfEmpty(ExcelSax.Get(cells, Idx(map, "textoBreve"))),
                NumMaterialProveedor = ExcelSax.NullIfEmpty(ExcelSax.Get(cells, Idx(map, "numMaterialProveedor"))),
                FechaDocumento = ParseDate(ExcelSax.Get(cells, Idx(map, "fechaDocumento"))),
                TieneHistorialEntrega = historial.Length > 0,
                ProveedorCodigo = provCodigo,
                ProveedorNombre = provNombre,
                ReferenciaProveedor = ExcelSax.NullIfEmpty(ExcelSax.Get(cells, Idx(map, "referenciaProveedor"))),
                Almacen = ExcelSax.NullIfEmpty(ExcelSax.Get(cells, Idx(map, "almacen"))),
                CantidadPedido = ExcelSax.ParseDecimal(ExcelSax.Get(cells, Idx(map, "cantidadPedido"))),
                PorEntregarCantidad = ExcelSax.ParseDecimal(ExcelSax.Get(cells, Idx(map, "porEntregarCantidad"))),
                ContratoMarco = ExcelSax.NullIfEmpty(ExcelSax.Get(cells, Idx(map, "contratoMarco"))),
            });
        }

        return result;
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

    // "Cantidad de pedido" (total pedido), distinta de "Por entregar (cantidad)".
    [GeneratedRegex(@"cantidad\s*(de\s*)?pedido", RegexOptions.IgnoreCase)]
    private static partial Regex CantidadPedidoRegex();

    [GeneratedRegex(@"por\s*entregar", RegexOptions.IgnoreCase)]
    private static partial Regex PorEntregarRegex();

    [GeneratedRegex(@"contrato\s*marco|acuerdo\s*marco", RegexOptions.IgnoreCase)]
    private static partial Regex ContratoMarcoRegex();

    [GeneratedRegex(@"^(\d+)")]
    private static partial Regex LeadingDigitsRegex();
}
