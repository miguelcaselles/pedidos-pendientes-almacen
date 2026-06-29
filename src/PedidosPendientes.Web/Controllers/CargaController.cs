using Microsoft.AspNetCore.Mvc;
using PedidosPendientes.Core.Abstractions;

namespace PedidosPendientes.Web.Controllers;

public class CargaController : Controller
{
    private readonly IOrderImportService _import;

    public CargaController(IOrderImportService import) => _import = import;

    [HttpGet]
    public IActionResult Index() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(20_000_000)] // 20 MB
    public async Task<IActionResult> Index(IFormFile? archivo, CancellationToken ct)
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
        var result = await _import.ImportAsync(stream, ct);

        if (result.Success)
            TempData["Success"] = result.Message;
        else
            TempData["Error"] = result.Message;

        return RedirectToAction(nameof(Index));
    }
}
