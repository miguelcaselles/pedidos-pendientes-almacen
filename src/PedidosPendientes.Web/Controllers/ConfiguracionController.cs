using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PedidosPendientes.Core;
using PedidosPendientes.Core.Abstractions;
using PedidosPendientes.Core.Entities;
using PedidosPendientes.Infrastructure.Data;
using PedidosPendientes.Web.Models;

namespace PedidosPendientes.Web.Controllers;

/// <summary>
/// Parametrización operativa: plazos de reclamación por clase A/B/C, plazos de
/// respuesta/llamada, acuerdo marco y criticidad de almacenes / centros de coste.
/// </summary>
public class ConfiguracionController : Controller
{
    private readonly AppDbContext _db;
    private readonly ISettingsService _settings;

    public ConfiguracionController(AppDbContext db, ISettingsService settings)
    {
        _db = db;
        _settings = settings;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var clases = await _db.MaterialClasificaciones.AsNoTracking()
            .GroupBy(m => m.Clase)
            .Select(g => new { Clase = g.Key, Total = g.Count() })
            .ToListAsync(ct);

        var vm = new ConfiguracionViewModel
        {
            Parametros = await _settings.GetParametrosAsync(ct),
            Criticidades = await _db.CriticidadUbicaciones.AsNoTracking()
                .OrderBy(c => c.Tipo).ThenBy(c => c.Codigo).ToListAsync(ct),
            MaterialesClasificados = clases.Sum(c => c.Total),
            ClaseA = clases.FirstOrDefault(c => c.Clase == "A")?.Total ?? 0,
            ClaseB = clases.FirstOrDefault(c => c.Clase == "B")?.Total ?? 0,
            ClaseC = clases.FirstOrDefault(c => c.Clase == "C")?.Total ?? 0,
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar(ReclamacionParametros parametros, CancellationToken ct)
    {
        await _settings.SaveParametrosAsync(parametros, ct);
        TempData["Success"] = "Parámetros guardados.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AgregarCriticidad(string tipo, string codigo,
        string? descripcion, string nivel, CancellationToken ct)
    {
        tipo = tipo == "centroCoste" ? "centroCoste" : "almacen";
        nivel = nivel == "alto" ? "alto" : "critico";
        codigo = (codigo ?? string.Empty).Trim();

        if (codigo.Length == 0)
        {
            TempData["Error"] = "Indica el código del almacén o centro de coste.";
            return RedirectToAction(nameof(Index));
        }

        var existe = await _db.CriticidadUbicaciones
            .AnyAsync(c => c.Tipo == tipo && c.Codigo == codigo, ct);
        if (existe)
        {
            TempData["Error"] = $"El código {codigo} ya está parametrizado.";
            return RedirectToAction(nameof(Index));
        }

        _db.CriticidadUbicaciones.Add(new CriticidadUbicacion
        {
            Tipo = tipo,
            Codigo = codigo,
            Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim(),
            Nivel = nivel,
        });
        await _db.SaveChangesAsync(ct);

        TempData["Success"] = $"Ubicación crítica {codigo} añadida.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarCriticidad(int id, CancellationToken ct)
    {
        var c = await _db.CriticidadUbicaciones.FindAsync([id], ct);
        if (c is not null)
        {
            _db.CriticidadUbicaciones.Remove(c);
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = $"Ubicación {c.Codigo} eliminada.";
        }
        return RedirectToAction(nameof(Index));
    }
}
