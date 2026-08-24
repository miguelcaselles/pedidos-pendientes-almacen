using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PedidosPendientes.Core;
using PedidosPendientes.Infrastructure.Data;
using PedidosPendientes.Web.Models;
using PedidosPendientes.Web.Services;

namespace PedidosPendientes.Web.Controllers;

public class OrdersController : Controller
{
    private readonly AppDbContext _db;
    private readonly PedidosOverviewService _overview;
    private readonly Core.Abstractions.IReclamacionEmailService _email;

    public OrdersController(AppDbContext db, PedidosOverviewService overview,
        Core.Abstractions.IReclamacionEmailService email)
    {
        _db = db;
        _overview = overview;
        _email = email;
    }

    public async Task<IActionResult> Index(string? search, string? estado, CancellationToken ct)
    {
        estado = (estado ?? "pendientes").ToLowerInvariant();
        var p = await _overview.GetParametrosAsync(ct);

        // Todas las líneas pendientes, enriquecidas (clase, criticidad, alertas, riesgo).
        var pendientes = await _overview.GetPendientesAsync(ct);
        var rows = await _overview.EnrichAsync(pendientes, p, ct);

        var vm = new OrdersViewModel
        {
            Search = search,
            Estado = estado,
            TotalPendientes = rows.Count,
            ParaReclamar = rows.Count(r => r.Alerta == AlertaReclamacion.Reclamar),
            ConAlerta = rows.Count(r => r.Alerta is AlertaReclamacion.SinRespuesta or AlertaReclamacion.Llamar),
            Reclamados = rows.Count(r => r.Order.Reclamado),
            EnFalta = rows.Count(r => r.Order.EnFalta),
            Recibidos = await _db.Orders.CountAsync(o => o.Recibido, ct),
            Anulados = await _db.Orders.CountAsync(o => o.Anulado, ct),
            UltimaCarga = await _db.UploadLogs
                .Where(u => u.Tipo == "pedidos" && u.Success)
                .MaxAsync(u => (DateTimeOffset?)u.At, ct),
            DiasAvisoCarga = p.DiasAvisoCargaObsoleta,
        };
        vm.CargaObsoleta = vm.UltimaCarga is null
            || vm.UltimaCarga < DateTimeOffset.UtcNow.AddDays(-p.DiasAvisoCargaObsoleta);

        // Pestañas sobre el conjunto pendiente (en memoria, ya cargado).
        IEnumerable<OrderRow> filtradas = estado switch
        {
            "reclamar" => rows.Where(r => r.Alerta == AlertaReclamacion.Reclamar),
            "alertas" => rows.Where(r => r.Alerta is AlertaReclamacion.SinRespuesta or AlertaReclamacion.Llamar),
            "reclamados" => rows.Where(r => r.Order.Reclamado),
            "enfalta" => rows.Where(r => r.Order.EnFalta),
            "recibidos" => await HistoricoAsync(o => o.Recibido, p, ct),
            "anulados" => await HistoricoAsync(o => o.Anulado, p, ct),
            _ => rows,
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            filtradas = filtradas.Where(r =>
                r.Order.DocumentoCompras.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                (r.Order.ProveedorNombre?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.Order.TextoBreve?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                r.Order.Material.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                (r.Order.Almacen?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.Order.CentroCoste?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        var lista = PedidosOverviewService.Priorizar(filtradas).ToList();
        vm.Truncado = lista.Count > 500;
        vm.Rows = lista.Take(500).ToList();

        return View(vm);
    }

    /// <summary>Pestañas de histórico (recibidos/anulados): consulta acotada y enriquecida.</summary>
    private async Task<List<OrderRow>> HistoricoAsync(
        System.Linq.Expressions.Expression<Func<Core.Entities.Order, bool>> filtro,
        ReclamacionParametros p, CancellationToken ct)
    {
        var orders = await _db.Orders.AsNoTracking()
            .Where(filtro)
            .OrderByDescending(o => o.UpdatedAt)
            .Take(500)
            .ToListAsync(ct);
        return await _overview.EnrichAsync(orders, p, ct);
    }

    // ----- Acciones de estado -----

    /// <summary>
    /// Marca la línea como reclamada y, si el SMTP institucional está configurado
    /// y el proveedor tiene email, envía la reclamación por correo (el correo
    /// agrupa todas las líneas pendientes del documento).
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reclamar(int id, string? estado, string? search, CancellationToken ct)
    {
        var order = await _db.Orders.FindAsync([id], ct);
        if (order is null)
        {
            TempData["Error"] = "No se encontró la línea de pedido.";
            return RedirectToAction(nameof(Index), new { estado, search });
        }

        // El asunto del correo distingue reenvíos según el contador previo.
        var resultado = await _email.EnviarAsync(order, "manual", ct);

        order.Reclamado = true;
        order.ReclamadoAt = DateTimeOffset.UtcNow;
        order.ReclamadoCount++;
        await _db.SaveChangesAsync(ct);

        if (resultado.Enviado)
            TempData["Success"] = $"Pedido marcado como reclamado. {resultado.Detalle}";
        else if (resultado.Motivo == "deshabilitado")
            TempData["Success"] = "Pedido marcado como reclamado (envío por correo no configurado).";
        else
            TempData["Error"] = $"Pedido marcado como reclamado, pero el correo no salió: {resultado.Detalle}";

        return RedirectToAction(nameof(Index), new { estado, search });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Recibir(int id, string? estado, string? search, CancellationToken ct)
        => Cambiar(id, estado, search, o =>
        {
            o.Recibido = true;
            o.RecibidoAt = DateTimeOffset.UtcNow;
            o.EnFalta = false;
        }, "Pedido marcado como recibido.", ct);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> EnFalta(int id, string? estado, string? search, CancellationToken ct)
        => Cambiar(id, estado, search, o =>
        {
            o.EnFalta = true;
            o.EnFaltaAt = DateTimeOffset.UtcNow;
        }, "Pedido marcado en falta para su seguimiento.", ct);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Anular(int id, string? estado, string? search, CancellationToken ct)
        => Cambiar(id, estado, search, o =>
        {
            o.Anulado = true;
            o.AnuladoAt = DateTimeOffset.UtcNow;
        }, "Línea anulada. Puedes recuperarla desde la pestaña Anulados.", ct);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Reactivar(int id, string? estado, string? search, CancellationToken ct)
        => Cambiar(id, estado, search, o =>
        {
            o.Recibido = false;
            o.RecibidoAt = null;
            o.Anulado = false;
            o.AnuladoAt = null;
            o.EnFalta = false;
        }, "Línea devuelta a pedidos pendientes.", ct);

    private async Task<IActionResult> Cambiar(int id, string? estado, string? search,
        Action<Core.Entities.Order> accion, string mensaje, CancellationToken ct)
    {
        var order = await _db.Orders.FindAsync([id], ct);
        if (order is null)
        {
            TempData["Error"] = "No se encontró la línea de pedido.";
        }
        else
        {
            accion(order);
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = mensaje;
        }
        return RedirectToAction(nameof(Index), new { estado, search });
    }
}
