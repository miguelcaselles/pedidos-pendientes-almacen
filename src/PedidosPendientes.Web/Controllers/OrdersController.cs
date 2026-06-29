using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PedidosPendientes.Core;
using PedidosPendientes.Infrastructure.Data;
using PedidosPendientes.Web.Models;

namespace PedidosPendientes.Web.Controllers;

public class OrdersController : Controller
{
    private readonly AppDbContext _db;

    // Días hábiles a partir de los cuales un pedido pendiente entra en "para reclamar".
    private const int DiasHabilesParaReclamar = 3;

    public OrdersController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? search, CancellationToken ct)
    {
        // Pendientes = ni recibido, ni con historial de entrega, ni anulado.
        var query = _db.Orders.Where(o => !o.Recibido && !o.TieneHistorialEntrega && !o.Anulado);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(o =>
                EF.Functions.Like(o.DocumentoCompras, $"%{s}%") ||
                (o.ProveedorNombre != null && EF.Functions.Like(o.ProveedorNombre, $"%{s}%")) ||
                (o.TextoBreve != null && EF.Functions.Like(o.TextoBreve, $"%{s}%")) ||
                EF.Functions.Like(o.Material, $"%{s}%"));
        }

        var orders = await query
            .OrderBy(o => o.FechaDocumento)
            .Take(500)
            .ToListAsync(ct);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var fechaCorte = BusinessDays.SubtractBusinessDays(today, DiasHabilesParaReclamar);

        var vm = new OrdersViewModel
        {
            Orders = orders,
            Search = search,
            TotalPendientes = await _db.Orders.CountAsync(o => !o.Recibido && !o.TieneHistorialEntrega && !o.Anulado, ct),
            ParaReclamar = await _db.Orders.CountAsync(o =>
                !o.Recibido && !o.TieneHistorialEntrega && !o.Anulado && !o.Reclamado &&
                o.FechaDocumento <= fechaCorte, ct),
            Reclamados = await _db.Orders.CountAsync(o =>
                !o.Recibido && !o.TieneHistorialEntrega && !o.Anulado && o.Reclamado, ct),
            RecibidosHoy = await _db.Orders.CountAsync(o =>
                o.Recibido && o.RecibidoAt != null && o.RecibidoAt.Value.Date == DateTimeOffset.UtcNow.Date, ct),
            UltimaCarga = await _db.Orders.MaxAsync(o => (DateTimeOffset?)o.LastUploadAt, ct),
        };

        return View(vm);
    }
}
