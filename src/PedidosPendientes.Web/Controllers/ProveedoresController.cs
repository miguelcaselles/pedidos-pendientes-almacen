using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PedidosPendientes.Infrastructure.Data;
using PedidosPendientes.Web.Models;
using PedidosPendientes.Web.Services;

namespace PedidosPendientes.Web.Controllers;

/// <summary>
/// Proveedores: datos de contacto, acuerdo marco con su plazo contractual y
/// seguimiento de cumplimiento (pedidos pendientes fuera de plazo = base para penalizar).
/// </summary>
public class ProveedoresController : Controller
{
    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly AppDbContext _db;
    private readonly PedidosOverviewService _overview;
    private readonly AuditoriaService _auditoria;

    public ProveedoresController(AppDbContext db, PedidosOverviewService overview, AuditoriaService auditoria)
    {
        _db = db;
        _overview = overview;
        _auditoria = auditoria;
    }

    public async Task<IActionResult> Index(string? search, bool am, CancellationToken ct)
    {
        var p = await _overview.GetParametrosAsync(ct);

        var query = _db.Proveedores.AsNoTracking().Where(pr => pr.Activo);
        if (am) query = query.Where(pr => pr.AcuerdoMarco);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(pr =>
                EF.Functions.Like(pr.Nombre, $"%{s}%") ||
                EF.Functions.Like(pr.Codigo, $"%{s}%"));
        }

        var proveedores = await query.OrderBy(pr => pr.Nombre).Take(500).ToListAsync(ct);

        // Pendientes enriquecidos para medir cumplimiento por proveedor.
        var pendientes = await _overview.GetPendientesAsync(ct);
        var rows = await _overview.EnrichAsync(pendientes, p, ct);
        var porProveedor = rows.Where(r => r.Order.ProveedorCodigo != null)
            .GroupBy(r => r.Order.ProveedorCodigo!)
            .ToDictionary(g => g.Key, g => g.ToList());

        var vm = new ProveedoresViewModel
        {
            Search = search,
            SoloAcuerdoMarco = am,
            Rows = proveedores.Select(pr =>
            {
                porProveedor.TryGetValue(pr.Codigo, out var suyos);
                return new ProveedorRow
                {
                    Proveedor = pr,
                    PendientesTotales = suyos?.Count ?? 0,
                    PendientesFueraPlazo = suyos?.Count(r => r.FueraPlazo) ?? 0,
                    PlazoAplicable = pr.PlazoEntregaDias
                        ?? (pr.AcuerdoMarco ? p.PlazoAcuerdoMarcoDias : p.PlazoObjetivoEntregaDias),
                };
            }).ToList(),
        };

        return View(vm);
    }

    /// <summary>Auditoría global de proveedores (KPIs + ranking de cumplimiento).</summary>
    public async Task<IActionResult> Auditoria(string? search, bool am, string? orden, CancellationToken ct)
    {
        var vm = await _auditoria.GetGlobalAsync(search, am, orden, ct);
        return View(vm);
    }

    /// <summary>Auditoría detallada de un proveedor concreto.</summary>
    public async Task<IActionResult> AuditoriaDetalle(string codigo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(codigo)) return RedirectToAction(nameof(Auditoria));
        var detalle = await _auditoria.GetProveedorAsync(codigo, ct);
        if (detalle is null) return NotFound();
        return View(detalle);
    }

    /// <summary>Exportación Excel de la auditoría global.</summary>
    public async Task<IActionResult> AuditoriaExcel(string? search, bool am, string? orden, CancellationToken ct)
    {
        var vm = await _auditoria.GetGlobalAsync(search, am, orden, ct);
        var bytes = AuditoriaExcelBuilder.Global(vm);
        var nombre = $"auditoria-proveedores-{DateTime.Now:yyyyMMdd-HHmm}.xlsx";
        return File(bytes, XlsxContentType, nombre);
    }

    /// <summary>Exportación Excel de la auditoría detallada de un proveedor.</summary>
    public async Task<IActionResult> AuditoriaProveedorExcel(string codigo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(codigo)) return RedirectToAction(nameof(Auditoria));
        var detalle = await _auditoria.GetProveedorAsync(codigo, ct);
        if (detalle is null) return NotFound();
        var bytes = AuditoriaExcelBuilder.Proveedor(detalle);
        var slug = new string((detalle.Resumen.Proveedor.Codigo).Where(char.IsLetterOrDigit).ToArray());
        var nombre = $"auditoria-{slug}-{DateTime.Now:yyyyMMdd-HHmm}.xlsx";
        return File(bytes, XlsxContentType, nombre);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var pr = await _db.Proveedores.FindAsync([id], ct);
        if (pr is null) return NotFound();

        return View(new ProveedorEditViewModel
        {
            Id = pr.Id,
            Codigo = pr.Codigo,
            Nombre = pr.Nombre,
            Email = pr.Email,
            Telefono = pr.Telefono,
            AcuerdoMarco = pr.AcuerdoMarco,
            PlazoEntregaDias = pr.PlazoEntregaDias,
            NotasPenalizacion = pr.NotasPenalizacion,
            Activo = pr.Activo,
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProveedorEditViewModel vm, CancellationToken ct)
    {
        var pr = await _db.Proveedores.FindAsync([vm.Id], ct);
        if (pr is null) return NotFound();

        pr.Email = string.IsNullOrWhiteSpace(vm.Email) ? null : vm.Email.Trim();
        pr.Telefono = string.IsNullOrWhiteSpace(vm.Telefono) ? null : vm.Telefono.Trim();
        pr.AcuerdoMarco = vm.AcuerdoMarco;
        pr.PlazoEntregaDias = vm.PlazoEntregaDias is > 0 ? vm.PlazoEntregaDias : null;
        pr.NotasPenalizacion = string.IsNullOrWhiteSpace(vm.NotasPenalizacion) ? null : vm.NotasPenalizacion.Trim();
        pr.Activo = vm.Activo;
        await _db.SaveChangesAsync(ct);

        TempData["Success"] = $"Proveedor {pr.Nombre} actualizado.";
        return RedirectToAction(nameof(Index));
    }
}
