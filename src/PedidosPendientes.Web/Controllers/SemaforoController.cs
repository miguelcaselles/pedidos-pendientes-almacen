using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PedidosPendientes.Infrastructure.Data;
using PedidosPendientes.Web.Models;

namespace PedidosPendientes.Web.Controllers;

/// <summary>
/// Semáforo NEXUS MD04: materiales de almacenaje bajo mínimo (rojo) cruzados con
/// los pedidos pendientes, para detectar los que NO tienen pedido lanzado.
/// </summary>
public class SemaforoController : Controller
{
    private readonly AppDbContext _db;

    public SemaforoController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? filtro, CancellationToken ct)
    {
        filtro = (filtro ?? "sinpedido").ToLowerInvariant();

        var items = await _db.StockMd04.AsNoTracking()
            .OrderBy(s => s.Semaforo == "rojo" ? 0 : s.Semaforo == "amarillo" ? 1 : 2)
            .ThenBy(s => s.Material)
            .ToListAsync(ct);

        // Pedidos pendientes agrupados por material (para saber si hay pedido lanzado).
        var materiales = items.Select(i => i.Material).Distinct().ToList();
        var pendientes = await _db.Orders.AsNoTracking()
            .Where(o => !o.Recibido && !o.TieneHistorialEntrega && !o.Anulado
                        && materiales.Contains(o.Material))
            .ToListAsync(ct);
        var pedidosPorMaterial = pendientes.GroupBy(o => o.Material)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<Core.Entities.Order>)g.ToList());

        var rows = items.Select(i => new SemaforoRow
        {
            Item = i,
            PedidosPendientes = pedidosPorMaterial.TryGetValue(i.Material, out var ps) ? ps : [],
        }).ToList();

        var vm = new SemaforoViewModel
        {
            CargadoAt = items.Count > 0 ? items.Max(i => i.CargadoAt) : null,
            Rojos = rows.Count(r => r.Item.Semaforo == "rojo"),
            RojosSinPedido = rows.Count(r => r.Item.Semaforo == "rojo" && !r.TienePedido),
            Amarillos = rows.Count(r => r.Item.Semaforo == "amarillo"),
            Verdes = rows.Count(r => r.Item.Semaforo == "verde"),
            Filtro = filtro,
            Rows = filtro switch
            {
                "rojos" => rows.Where(r => r.Item.Semaforo == "rojo").ToList(),
                "todos" => rows,
                _ => rows.Where(r => r.Item.Semaforo == "rojo" && !r.TienePedido).ToList(),
            },
        };

        return View(vm);
    }
}
