using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PedidosPendientes.Infrastructure.Data;
using PedidosPendientes.Web.Models;

namespace PedidosPendientes.Web.Controllers;

/// <summary>
/// Semáforo NEXUS MD04: materiales en rojo (Status) cruzados con los pedidos
/// pendientes para detectar los que NO tienen pedido lanzado, con las excepciones
/// importantes del listado: ME3 (pedido fuera de plazo de entrega) y ME6 (por
/// debajo de stock de seguridad). Dos pestañas: almacenaje y no almacenaje.
/// </summary>
public class SemaforoController : Controller
{
    private readonly AppDbContext _db;

    public SemaforoController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? ambito, string? filtro, CancellationToken ct)
    {
        ambito = (ambito ?? "almacenaje").ToLowerInvariant() == "noalmacenaje" ? "noalmacenaje" : "almacenaje";
        filtro = (filtro ?? "sinpedido").ToLowerInvariant();

        var totales = await _db.StockMd04.AsNoTracking()
            .GroupBy(s => s.Ambito)
            .Select(g => new { Ambito = g.Key, Total = g.Count() })
            .ToListAsync(ct);

        var items = await _db.StockMd04.AsNoTracking()
            .Where(s => s.Ambito == ambito)
            .OrderBy(s => s.Semaforo == "rojo" ? 0 : s.Semaforo == "amarillo" ? 1 : 2)
            .ThenByDescending(s => (s.PedidosFueraPlazo ?? 0) + (s.BajoStockSeguridad ?? 0))
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
            Ambito = ambito,
            TotalAlmacenaje = totales.FirstOrDefault(t => t.Ambito == "almacenaje")?.Total ?? 0,
            TotalNoAlmacenaje = totales.FirstOrDefault(t => t.Ambito == "noalmacenaje")?.Total ?? 0,
            Rojos = rows.Count(r => r.Item.Semaforo == "rojo"),
            RojosSinPedido = rows.Count(r => r.Item.Semaforo == "rojo" && !r.TienePedido),
            Amarillos = rows.Count(r => r.Item.Semaforo == "amarillo"),
            Verdes = rows.Count(r => r.Item.Semaforo == "verde"),
            FueraPlazo = rows.Count(r => r.Item.PedidosFueraPlazo > 0),
            BajoSeguridad = rows.Count(r => r.Item.BajoStockSeguridad > 0),
            MuestraStock = rows.Any(r => r.Item.Stock is not null || r.Item.PuntoPedido is not null),
            Filtro = filtro,
            Rows = filtro switch
            {
                "rojos" => rows.Where(r => r.Item.Semaforo == "rojo").ToList(),
                "fueraplazo" => rows.Where(r => r.Item.PedidosFueraPlazo > 0).ToList(),
                "bajoseguridad" => rows.Where(r => r.Item.BajoStockSeguridad > 0).ToList(),
                "todos" => rows,
                _ => rows.Where(r => r.Item.Semaforo == "rojo" && !r.TienePedido).ToList(),
            },
        };

        return View(vm);
    }
}
