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
            "recibidos" => await HistoricoAsync(o => o.Recibido, search, p, ct),
            "anulados" => await HistoricoAsync(o => o.Anulado, search, p, ct),
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
        string? search, ReclamacionParametros p, CancellationToken ct)
    {
        var query = _db.Orders.AsNoTracking().Where(filtro);

        // El buscador se aplica en la consulta, antes de acotar a 500: si no, un
        // pedido recibido antiguo nunca aparecería aunque exista.
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(o =>
                o.DocumentoCompras.ToLower().Contains(s) ||
                o.Material.ToLower().Contains(s) ||
                (o.ProveedorNombre != null && o.ProveedorNombre.ToLower().Contains(s)) ||
                (o.TextoBreve != null && o.TextoBreve.ToLower().Contains(s)) ||
                (o.Almacen != null && o.Almacen.ToLower().Contains(s)) ||
                (o.CentroCoste != null && o.CentroCoste.ToLower().Contains(s)));
        }

        var orders = await query
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

        // Si el SMTP institucional no envió (no configurado, sin email o error),
        // se abre el cliente de correo del puesto con el borrador precargado
        // para que la persona lo revise y lo envíe a mano.
        if (!resultado.Enviado)
        {
            var lineas = await _db.Orders.AsNoTracking()
                .Where(o => o.DocumentoCompras == order.DocumentoCompras && !o.Recibido && !o.Anulado)
                .OrderBy(o => o.Material)
                .ToListAsync(ct);
            if (lineas.Count == 0) lineas = [order];

            var emailProveedor = order.ProveedorCodigo is null ? null
                : (await _db.Proveedores.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Codigo == order.ProveedorCodigo, ct))?.Email?.Trim();

            TempData["Mailto"] = ReclamacionMailto.Build(order, lineas, emailProveedor);
        }

        order.Reclamado = true;
        order.ReclamadoAt = DateTimeOffset.UtcNow;
        order.ReclamadoCount++;
        await _db.SaveChangesAsync(ct);

        if (resultado.Enviado)
            TempData["Success"] = $"Pedido marcado como reclamado. {resultado.Detalle}";
        else if (resultado.Motivo is "deshabilitado" or "sin-email")
            TempData["Success"] = "Pedido marcado como reclamado. Se ha abierto el borrador de la reclamación en tu correo (Outlook): revísalo y envíalo.";
        else
            TempData["Error"] = $"Pedido marcado como reclamado, pero el correo automático no salió ({resultado.Detalle}). Se ha abierto el borrador en tu correo para enviarlo a mano.";

        return RedirectToAction(nameof(Index), new { estado, search });
    }

    /// <summary>
    /// Recepción parcial: apunta la cantidad entregada; el resto sigue pendiente
    /// y vuelve al circuito de reclamación (se anula la marca de reclamado para
    /// que el plazo de la clase vuelva a evaluarse sobre lo que falta).
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RecibirParcial(int id, string? cantidad, string? estado, string? search, CancellationToken ct)
    {
        var order = await _db.Orders.FindAsync([id], ct);
        if (order is null)
        {
            TempData["Error"] = "No se encontró la línea de pedido.";
            return RedirectToAction(nameof(Index), new { estado, search });
        }

        // El input type=number envía siempre punto decimal; se acepta también coma
        // para no depender de la cultura del servidor (IIS es-ES vs contenedor).
        if (!decimal.TryParse(cantidad?.Replace(',', '.'),
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out var cant))
        {
            TempData["Error"] = "Indica la cantidad recibida (solo números).";
            return RedirectToAction(nameof(Index), new { estado, search });
        }

        var pendiente = order.PorEntregarCantidad ?? order.CantidadPedido;
        if (cant <= 0)
        {
            TempData["Error"] = "Indica una cantidad recibida mayor que cero.";
            return RedirectToAction(nameof(Index), new { estado, search });
        }
        if (pendiente is not null && cant > pendiente.Value)
        {
            TempData["Error"] = $"La cantidad recibida ({cant:0.##}) supera la pendiente ({pendiente.Value:0.##}). Si llegó todo, usa «Marcar como recibido».";
            return RedirectToAction(nameof(Index), new { estado, search });
        }

        order.CantidadRecibida = (order.CantidadRecibida ?? 0) + cant;
        var nota = $"Recepción parcial {DateTime.Today:dd/MM/yyyy}: {cant:0.##} uds.";

        if (pendiente is not null && cant >= pendiente.Value)
        {
            // Con la entrega queda completo: se cierra como recibido.
            order.PorEntregarCantidad = 0;
            order.Recibido = true;
            order.RecibidoAt = DateTimeOffset.UtcNow;
            order.EnFalta = false;
            TempData["Success"] = "Cantidad completada: la línea queda marcada como recibida.";
        }
        else
        {
            var restante = pendiente is null ? (decimal?)null : pendiente.Value - cant;
            if (restante is not null)
            {
                order.PorEntregarCantidad = restante;
                nota += $" Quedan {restante.Value:0.##} uds. pendientes.";
            }
            // Vuelve al circuito de reclamación por lo que falta.
            order.Reclamado = false;
            TempData["Success"] = restante is null
                ? "Recepción parcial anotada. La línea sigue pendiente y reclamable."
                : $"Recepción parcial anotada: quedan {restante.Value:0.##} uds. pendientes, que vuelven al circuito de reclamación.";
        }

        order.Comentarios = string.IsNullOrWhiteSpace(order.Comentarios)
            ? nota
            : $"{order.Comentarios}\n{nota}";
        await _db.SaveChangesAsync(ct);

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
