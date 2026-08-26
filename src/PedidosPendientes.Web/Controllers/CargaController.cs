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
    private readonly ICecosImportService _cecos;
    private readonly ILogger<CargaController> _log;

    public CargaController(AppDbContext db, IOrderImportService pedidos,
        IMd04ImportService md04, IClasificacionImportService abc, ICecosImportService cecos,
        ILogger<CargaController> log)
    {
        _db = db;
        _pedidos = pedidos;
        _md04 = md04;
        _abc = abc;
        _cecos = cecos;
        _log = log;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var vm = new CargaViewModel
        {
            UltimaPedidos = await UltimaCargaAsync("pedidos", ct),
            UltimaMd04 = await UltimaCargaAsync("md04", ct),
            UltimaMd04Na = await UltimaCargaAsync("md04na", ct),
            UltimaAbc = await UltimaCargaAsync("abc", ct),
            UltimaCecos = await UltimaCargaAsync("cecos", ct),
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
    public async Task<IActionResult> Md04(IFormFile? archivo, string? ambito, CancellationToken ct)
        => await ImportarAsync(archivo, (s, n) => _md04.ImportAsync(s, n, ambito ?? "almacenaje", ct));

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> Clasificacion(IFormFile? archivo, CancellationToken ct)
        => await ImportarAsync(archivo, (s, n) => _abc.ImportAsync(s, n, ct));

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> Cecos(IFormFile? archivo, CancellationToken ct)
        => await ImportarAsync(archivo, (s, n) => _cecos.ImportAsync(s, n, ct));

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

        try
        {
            await using var stream = archivo.OpenReadStream();
            var result = await import(stream, Path.GetFileName(archivo.FileName));

            if (result.Success)
                TempData["Success"] = result.Message;
            else
                TempData["Error"] = result.Message;
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            // Doble clic o dos cargas a la vez: la otra carga ya hizo el trabajo.
            TempData["Error"] = "Había otra carga en curso en ese momento. Revisa el historial: probablemente los datos ya están importados.";
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            _log.LogError(ex, "Error de base de datos al importar {File}", archivo.FileName);
            TempData["Error"] = "La importación no se pudo guardar en la base de datos (¿algún dato fuera de lo esperado o una carga simultánea?). No se ha perdido nada: los datos anteriores siguen intactos.";
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error inesperado al importar {File}", archivo.FileName);
            TempData["Error"] = "La importación falló de forma inesperada. Verifica que el fichero es el listado correcto y vuelve a intentarlo.";
        }

        return RedirectToAction(nameof(Index));
    }

    private Task<UploadLog?> UltimaCargaAsync(string tipo, CancellationToken ct)
        => _db.UploadLogs.AsNoTracking()
            .Where(u => u.Tipo == tipo && u.Success)
            .OrderByDescending(u => u.At)
            .FirstOrDefaultAsync(ct);
}
