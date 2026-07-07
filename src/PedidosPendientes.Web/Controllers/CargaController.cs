using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PedidosPendientes.Core.Abstractions;
using PedidosPendientes.Core.Dtos;
using PedidosPendientes.Core.Entities;
using PedidosPendientes.Infrastructure.Data;
using PedidosPendientes.Web.Models;

namespace PedidosPendientes.Web.Controllers;

public class CargaController : Controller
{
    private readonly AppDbContext _db;
    private readonly IOrderImportService _pedidos;
    private readonly IMd04ImportService _md04;
    private readonly IClasificacionImportService _abc;

    public CargaController(AppDbContext db, IOrderImportService pedidos,
        IMd04ImportService md04, IClasificacionImportService abc)
    {
        _db = db;
        _pedidos = pedidos;
        _md04 = md04;
        _abc = abc;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var vm = new CargaViewModel
        {
            UltimaPedidos = await UltimaCargaAsync("pedidos", ct),
            UltimaMd04 = await UltimaCargaAsync("md04", ct),
            UltimaAbc = await UltimaCargaAsync("abc", ct),
            Historial = await _db.UploadLogs.AsNoTracking()
                .OrderByDescending(u => u.At).Take(15).ToListAsync(ct),
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(20_000_000)] // 20 MB
    public async Task<IActionResult> Pedidos(IFormFile? archivo, CancellationToken ct)
        => await ImportarAsync(archivo, (s, n) => _pedidos.ImportAsync(s, n, ct));

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> Md04(IFormFile? archivo, CancellationToken ct)
        => await ImportarAsync(archivo, (s, n) => _md04.ImportAsync(s, n, ct));

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> Clasificacion(IFormFile? archivo, CancellationToken ct)
        => await ImportarAsync(archivo, (s, n) => _abc.ImportAsync(s, n, ct));

    private async Task<IActionResult> ImportarAsync(IFormFile? archivo,
        Func<Stream, string, Task<UploadResult>> import)
    {
        if (archivo is null || archivo.Length == 0)
        {
            TempData["Error"] = "Selecciona un archivo Excel (.xlsx) antes de cargar.";
            return RedirectToAction(nameof(Index));
        }

        var ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (ext != ".xlsx" && ext != ".xlsm")
        {
            TempData["Error"] = "El archivo debe ser un Excel (.xlsx).";
            return RedirectToAction(nameof(Index));
        }

        await using var stream = archivo.OpenReadStream();
        var result = await import(stream, Path.GetFileName(archivo.FileName));

        if (result.Success)
            TempData["Success"] = result.Message;
        else
            TempData["Error"] = result.Message;

        return RedirectToAction(nameof(Index));
    }

    private Task<UploadLog?> UltimaCargaAsync(string tipo, CancellationToken ct)
        => _db.UploadLogs.AsNoTracking()
            .Where(u => u.Tipo == tipo && u.Success)
            .OrderByDescending(u => u.At)
            .FirstOrDefaultAsync(ct);
}
