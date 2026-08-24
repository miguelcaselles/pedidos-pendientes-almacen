using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PedidosPendientes.Core.Entities;
using PedidosPendientes.Infrastructure.Data;
using PedidosPendientes.Web.Models;

namespace PedidosPendientes.Web.Controllers;

/// <summary>
/// Catálogo de producto: buscador por código o descripción sobre todos los
/// materiales conocidos (pedidos, MD04, clasificación y fichas creadas) y ficha
/// por material con denominación, materiales alternativos, presupuesto y la
/// ficha técnica del material como documento adjunto.
/// </summary>
public class CatalogoController : Controller
{
    private const int MaxResultados = 200;
    private const long MaxAdjuntoBytes = 10_000_000; // 10 MB

    private static readonly string[] ExtensionesPermitidas =
        [".pdf", ".doc", ".docx", ".xls", ".xlsx", ".png", ".jpg", ".jpeg"];

    private readonly AppDbContext _db;

    public CatalogoController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index(string? search, CancellationToken ct)
    {
        var fichas = await _db.ProductoFichas.AsNoTracking()
            .Select(f => new { f.Material, f.Denominacion, f.PresupuestoUnitario, NumAdjuntos = f.Adjuntos.Count })
            .ToListAsync(ct);
        var fichasPorMaterial = fichas.ToDictionary(f => f.Material);

        // Universo de materiales conocidos: pedidos + MD04 + clasificación + fichas.
        var dePedidos = await _db.Orders.AsNoTracking()
            .GroupBy(o => o.Material)
            .Select(g => new
            {
                Material = g.Key,
                Denominacion = g.Max(o => o.TextoBreve),
                Proveedor = g.Max(o => o.ProveedorNombre),
            })
            .ToListAsync(ct);
        var deMd04 = await _db.StockMd04.AsNoTracking()
            .Select(s => new { s.Material, Denominacion = s.TextoBreve })
            .ToListAsync(ct);
        var clases = await _db.MaterialClasificaciones.AsNoTracking()
            .ToDictionaryAsync(m => m.Material, m => m.Clase, ct);

        var universo = new Dictionary<string, CatalogoResultado>();
        void Add(string material, string? denominacion, string? proveedor)
        {
            if (!universo.TryGetValue(material, out var r))
            {
                r = new CatalogoResultado { Material = material };
                universo[material] = r;
            }
            r.Denominacion ??= denominacion;
            r.ProveedorHabitual ??= proveedor;
        }

        foreach (var f in fichas) Add(f.Material, f.Denominacion, null);
        foreach (var p in dePedidos) Add(p.Material, p.Denominacion, p.Proveedor);
        foreach (var s in deMd04) Add(s.Material, s.Denominacion, null);
        foreach (var m in clases.Keys) Add(m, null, null);

        foreach (var r in universo.Values)
        {
            if (fichasPorMaterial.TryGetValue(r.Material, out var f))
            {
                r.TieneFicha = true;
                r.NumAdjuntos = f.NumAdjuntos;
                r.PresupuestoUnitario = f.PresupuestoUnitario;
                r.Denominacion = f.Denominacion ?? r.Denominacion;
            }
            clases.TryGetValue(r.Material, out var clase);
            r.Clase = clase;
        }

        IEnumerable<CatalogoResultado> resultados = universo.Values;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            resultados = resultados.Where(r =>
                r.Material.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                (r.Denominacion?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.ProveedorHabitual?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        var lista = resultados
            .OrderByDescending(r => r.TieneFicha)
            .ThenBy(r => r.Material)
            .ToList();

        var vm = new CatalogoIndexViewModel
        {
            Search = search,
            FichasCreadas = fichas.Count,
            Truncado = lista.Count > MaxResultados,
            Rows = lista.Take(MaxResultados).ToList(),
        };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Ficha(string material, CancellationToken ct)
    {
        material = (material ?? string.Empty).Trim();
        if (material.Length == 0) return RedirectToAction(nameof(Index));

        var ficha = await _db.ProductoFichas.AsNoTracking()
            .Include(f => f.Adjuntos)
            .FirstOrDefaultAsync(f => f.Material == material, ct);

        // Contexto del material en el resto de la aplicación.
        var lineas = await _db.Orders.AsNoTracking()
            .Where(o => o.Material == material)
            .Select(o => new { o.TextoBreve, o.ProveedorNombre, o.DocumentoCompras, Pendiente = !o.Recibido && !o.TieneHistorialEntrega && !o.Anulado })
            .ToListAsync(ct);
        var md04Texto = await _db.StockMd04.AsNoTracking()
            .Where(s => s.Material == material && s.TextoBreve != null)
            .Select(s => s.TextoBreve)
            .FirstOrDefaultAsync(ct);
        var clase = await _db.MaterialClasificaciones.AsNoTracking()
            .Where(m => m.Material == material)
            .Select(m => m.Clase)
            .FirstOrDefaultAsync(ct);

        var vm = new CatalogoFichaViewModel
        {
            Ficha = ficha ?? new ProductoFicha { Material = material },
            EsNueva = ficha is null,
            Clase = clase,
            DenominacionSugerida = lineas.Select(l => l.TextoBreve).FirstOrDefault(t => t != null) ?? md04Texto,
            ProveedorHabitual = lineas.Where(l => l.ProveedorNombre != null)
                .GroupBy(l => l.ProveedorNombre)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key,
            PedidosHistoricos = lineas.Select(l => l.DocumentoCompras).Distinct().Count(),
            PendientesActuales = lineas.Count(l => l.Pendiente),
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar(string material, string? denominacion,
        string? descripcionAmpliada, string? materialesAlternativos,
        decimal? presupuestoUnitario, string? notas, CancellationToken ct)
    {
        material = (material ?? string.Empty).Trim();
        if (material.Length == 0) return RedirectToAction(nameof(Index));

        var ficha = await _db.ProductoFichas.FirstOrDefaultAsync(f => f.Material == material, ct);
        if (ficha is null)
        {
            ficha = new ProductoFicha { Material = material };
            _db.ProductoFichas.Add(ficha);
        }

        ficha.Denominacion = Limpiar(denominacion, 255);
        ficha.DescripcionAmpliada = Limpiar(descripcionAmpliada, 2000);
        ficha.MaterialesAlternativos = Limpiar(materialesAlternativos, 1000);
        ficha.PresupuestoUnitario = presupuestoUnitario is < 0 ? null : presupuestoUnitario;
        ficha.Notas = Limpiar(notas, 2000);

        await _db.SaveChangesAsync(ct);
        TempData["Success"] = $"Ficha del material {material} guardada.";
        return RedirectToAction(nameof(Ficha), new { material });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(12_000_000)]
    public async Task<IActionResult> SubirAdjunto(string material, IFormFile? archivo, CancellationToken ct)
    {
        material = (material ?? string.Empty).Trim();
        if (material.Length == 0) return RedirectToAction(nameof(Index));

        if (archivo is null || archivo.Length == 0)
        {
            TempData["Error"] = "Selecciona un archivo antes de subirlo.";
            return RedirectToAction(nameof(Ficha), new { material });
        }
        if (archivo.Length > MaxAdjuntoBytes)
        {
            TempData["Error"] = "El archivo supera el tamaño máximo (10 MB).";
            return RedirectToAction(nameof(Ficha), new { material });
        }
        var ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (!ExtensionesPermitidas.Contains(ext))
        {
            TempData["Error"] = "Formato no admitido. Usa PDF, Word, Excel o imagen (PNG/JPG).";
            return RedirectToAction(nameof(Ficha), new { material });
        }

        var ficha = await _db.ProductoFichas.FirstOrDefaultAsync(f => f.Material == material, ct);
        if (ficha is null)
        {
            ficha = new ProductoFicha { Material = material };
            _db.ProductoFichas.Add(ficha);
        }

        using var ms = new MemoryStream();
        await archivo.CopyToAsync(ms, ct);

        ficha.Adjuntos.Add(new ProductoAdjunto
        {
            FileName = Path.GetFileName(archivo.FileName),
            ContentType = archivo.ContentType is { Length: > 0 } ctType ? ctType : "application/octet-stream",
            Size = archivo.Length,
            Contenido = ms.ToArray(),
        });

        await _db.SaveChangesAsync(ct);
        TempData["Success"] = $"Documento «{Path.GetFileName(archivo.FileName)}» adjuntado a la ficha.";
        return RedirectToAction(nameof(Ficha), new { material });
    }

    [HttpGet]
    public async Task<IActionResult> Adjunto(int id, CancellationToken ct)
    {
        var adj = await _db.ProductoAdjuntos.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, ct);
        if (adj is null) return NotFound();
        return File(adj.Contenido, adj.ContentType, adj.FileName);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarAdjunto(int id, string material, CancellationToken ct)
    {
        var adj = await _db.ProductoAdjuntos.FindAsync([id], ct);
        if (adj is not null)
        {
            _db.ProductoAdjuntos.Remove(adj);
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = $"Documento «{adj.FileName}» eliminado.";
        }
        return RedirectToAction(nameof(Ficha), new { material });
    }

    private static string? Limpiar(string? s, int max)
    {
        s = s?.Trim();
        if (string.IsNullOrEmpty(s)) return null;
        return s.Length <= max ? s : s[..max];
    }
}
