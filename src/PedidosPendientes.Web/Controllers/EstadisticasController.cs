using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PedidosPendientes.Infrastructure.Data;
using PedidosPendientes.Web.Models;

namespace PedidosPendientes.Web.Controllers;

/// <summary>
/// Estadísticas de periodicidad de pedido por artículo: con cuánta frecuencia se
/// pide cada material, cuándo fue el último pedido y cuándo tocaría el siguiente.
/// Ayuda a detectar materiales cuyo pedido "toca" y no se ha lanzado.
/// </summary>
public class EstadisticasController : Controller
{
    private const int MaxFilas = 300;

    private readonly AppDbContext _db;

    public EstadisticasController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? search, string? orden, CancellationToken ct)
    {
        orden = (orden ?? "pedidos").ToLowerInvariant();
        var hoy = DateOnly.FromDateTime(DateTime.Today);

        // Histórico completo de líneas (anuladas excluidas): base de la periodicidad.
        var lineas = await _db.Orders.AsNoTracking()
            .Where(o => !o.Anulado)
            .Select(o => new
            {
                o.Material,
                o.TextoBreve,
                o.DocumentoCompras,
                o.FechaDocumento,
                o.CantidadPedido,
                o.PorEntregarCantidad,
                o.ProveedorNombre,
                Pendiente = !o.Recibido && !o.TieneHistorialEntrega,
            })
            .ToListAsync(ct);

        var clases = await _db.MaterialClasificaciones.AsNoTracking()
            .ToDictionaryAsync(m => m.Material, m => m.Clase, ct);

        var rows = lineas
            .GroupBy(l => l.Material)
            .Select(g =>
            {
                // Un pedido = un documento de compras; puede haber varias líneas.
                var fechasPedidos = g.GroupBy(l => l.DocumentoCompras)
                    .Select(d => d.Min(l => l.FechaDocumento))
                    .OrderBy(f => f)
                    .ToList();

                double? intervalo = null;
                if (fechasPedidos.Count >= 2)
                {
                    var span = fechasPedidos[^1].DayNumber - fechasPedidos[0].DayNumber;
                    intervalo = (double)span / (fechasPedidos.Count - 1);
                }

                var cantidades = g.Select(l => l.CantidadPedido ?? l.PorEntregarCantidad)
                    .Where(c => c is > 0).Select(c => c!.Value).ToList();

                var proveedor = g.Where(l => l.ProveedorNombre != null)
                    .GroupBy(l => l.ProveedorNombre)
                    .OrderByDescending(p => p.Count())
                    .FirstOrDefault()?.Key;

                clases.TryGetValue(g.Key, out var clase);
                var ultimo = fechasPedidos[^1];

                return new EstadisticaMaterial
                {
                    Material = g.Key,
                    Descripcion = g.OrderByDescending(l => l.FechaDocumento).First().TextoBreve,
                    Clase = clase,
                    NumPedidos = fechasPedidos.Count,
                    PrimerPedido = fechasPedidos[0],
                    UltimoPedido = ultimo,
                    IntervaloMedioDias = intervalo,
                    DiasDesdeUltimo = hoy.DayNumber - ultimo.DayNumber,
                    ProximoEstimado = intervalo is null ? null
                        : ultimo.AddDays((int)Math.Round(intervalo.Value)),
                    CantidadMedia = cantidades.Count == 0 ? null : Math.Round(cantidades.Average(), 1),
                    ProveedorHabitual = proveedor,
                    PendientesActuales = g.Count(l => l.Pendiente),
                };
            });

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            rows = rows.Where(r =>
                r.Material.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                (r.Descripcion?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.ProveedorHabitual?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        var ordenadas = (orden switch
        {
            "frecuencia" => rows.OrderBy(r => r.IntervaloMedioDias ?? double.MaxValue),
            "reciente" => rows.OrderByDescending(r => r.UltimoPedido),
            _ => rows.OrderByDescending(r => r.NumPedidos),
        }).ToList();

        var vm = new EstadisticasViewModel
        {
            Search = search,
            Orden = orden,
            TotalMateriales = ordenadas.Count,
            Truncado = ordenadas.Count > MaxFilas,
            Rows = ordenadas.Take(MaxFilas).ToList(),
        };
        return View(vm);
    }
}
