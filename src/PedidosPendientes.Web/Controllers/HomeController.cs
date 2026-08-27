using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PedidosPendientes.Core;
using PedidosPendientes.Infrastructure.Data;
using PedidosPendientes.Web.Models;
using PedidosPendientes.Web.Services;

namespace PedidosPendientes.Web.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;
    private readonly PedidosOverviewService _overview;

    public HomeController(AppDbContext db, PedidosOverviewService overview)
    {
        _db = db;
        _overview = overview;
    }

    /// <summary>Panel de control: alertas, riesgo de retraso, incidencias, cargas y semáforo MD04.</summary>
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var p = await _overview.GetParametrosAsync(ct);
        var pendientes = await _overview.GetPendientesAsync(ct);
        var rows = await _overview.EnrichAsync(pendientes, p, ct);

        var hoyUtc = DateTimeOffset.UtcNow.Date;
        var vm = new DashboardViewModel
        {
            TotalPendientes = rows.Count,
            ParaReclamar = rows.Count(r => r.Alerta == AlertaReclamacion.Reclamar),
            SinRespuesta = rows.Count(r => r.Alerta == AlertaReclamacion.SinRespuesta),
            ParaLlamar = rows.Count(r => r.Alerta == AlertaReclamacion.Llamar),
            RecibidosHoy = await _db.Orders.CountAsync(o =>
                o.Recibido && o.RecibidoAt != null && o.RecibidoAt.Value.Date == hoyUtc, ct),
            PorcentajeFueraPlazo = rows.Count == 0 ? 0 : 100.0 * rows.Count(r => r.FueraPlazo) / rows.Count,
            DiasAvisoCarga = p.DiasAvisoCargaObsoleta,
            AlertasTop = PedidosOverviewService.Priorizar(
                rows.Where(r => r.Alerta != AlertaReclamacion.Ninguna)).Take(8).ToList(),
        };

        // --- Probabilidad histórica de retraso (recibidos que tardaron más del plazo objetivo) ---
        var recibidos = await _db.Orders.AsNoTracking()
            .Where(o => o.Recibido && o.RecibidoAt != null)
            .OrderByDescending(o => o.RecibidoAt)
            .Take(1000)
            .Select(o => new { o.FechaDocumento, o.RecibidoAt })
            .ToListAsync(ct);
        vm.MuestraHistorica = recibidos.Count;
        if (recibidos.Count > 0)
        {
            var tardios = recibidos.Count(r => BusinessDays.CountBusinessDays(
                r.FechaDocumento, DateOnly.FromDateTime(r.RecibidoAt!.Value.LocalDateTime)) > p.PlazoObjetivoEntregaDias);
            vm.ProbabilidadRetrasoHistorica = 100.0 * tardios / recibidos.Count;
        }

        // --- Estado de las cargas ---
        var ultimas = await _db.UploadLogs.AsNoTracking()
            .Where(u => u.Success)
            .GroupBy(u => u.Tipo)
            .Select(g => g.OrderByDescending(u => u.At).First())
            .ToListAsync(ct);
        vm.UltimaPedidos = ultimas.FirstOrDefault(u => u.Tipo == "pedidos");
        vm.UltimaMd04 = ultimas.FirstOrDefault(u => u.Tipo == "md04");
        vm.UltimaMd04Na = ultimas.FirstOrDefault(u => u.Tipo == "md04na");
        vm.UltimaAbc = ultimas.FirstOrDefault(u => u.Tipo == "abc");
        vm.UltimaCecos = ultimas.FirstOrDefault(u => u.Tipo == "cecos");
        vm.CargaPedidosObsoleta = vm.UltimaPedidos is null
            || vm.UltimaPedidos.At < DateTimeOffset.UtcNow.AddDays(-p.DiasAvisoCargaObsoleta);

        // --- Semáforo MD04: rojos y rojos sin pedido lanzado ---
        // Mismo criterio que la pestaña Semáforo: cuentan como "con pedido" todas
        // las líneas lanzadas (aunque tengan historial de entrega) y los materiales
        // cuyo MD04 ya señala pedido fuera de plazo (ME3 > 0).
        var rojos = await _db.StockMd04.AsNoTracking()
            .Where(s => s.Semaforo == "rojo")
            .Select(s => new { s.Material, s.PedidosFueraPlazo })
            .ToListAsync(ct);
        vm.Md04Rojos = rojos.Count;
        var materialesRojos = rojos.Select(r => r.Material).Distinct().ToList();
        var materialesConPedido = (await _db.Orders.AsNoTracking()
                .Where(o => !o.Recibido && !o.Anulado && materialesRojos.Contains(o.Material))
                .Select(o => o.Material)
                .ToListAsync(ct))
            .ToHashSet();
        vm.Md04RojosSinPedido = rojos.Count(r =>
            !materialesConPedido.Contains(r.Material) && (r.PedidosFueraPlazo ?? 0) == 0);
        vm.Md04CargadoAt = await _db.StockMd04.MaxAsync(s => (DateTimeOffset?)s.CargadoAt, ct);

        // --- Incidencias por mes (últimos 6 meses) ---
        var haceSeisMeses = new DateTimeOffset(DateTime.UtcNow.AddMonths(-5).Year,
            DateTime.UtcNow.AddMonths(-5).Month, 1, 0, 0, 0, TimeSpan.Zero);
        var enFalta = await _db.Orders.AsNoTracking()
            .Where(o => o.EnFaltaAt != null && o.EnFaltaAt >= haceSeisMeses)
            .Select(o => o.EnFaltaAt!.Value)
            .ToListAsync(ct);
        string[] estadosProblema = ["en_falta", "anulado", "no_localizado"];
        var respuestasProblema = await _db.ProviderResponses.AsNoTracking()
            .Where(r => r.CreatedAt >= haceSeisMeses && estadosProblema.Contains(r.Estado))
            .Select(r => r.CreatedAt)
            .ToListAsync(ct);

        var meses = new List<IncidenciaMes>();
        var cursor = new DateTime(haceSeisMeses.Year, haceSeisMeses.Month, 1);
        var culture = new System.Globalization.CultureInfo("es-ES");
        for (int i = 0; i < 6; i++)
        {
            var inicio = new DateTimeOffset(cursor, TimeSpan.Zero);
            var fin = new DateTimeOffset(cursor.AddMonths(1), TimeSpan.Zero);
            meses.Add(new IncidenciaMes
            {
                Etiqueta = cursor.ToString("MMM yy", culture),
                EnFalta = enFalta.Count(d => d >= inicio && d < fin),
                RespuestasProblema = respuestasProblema.Count(d => d >= inicio && d < fin),
            });
            cursor = cursor.AddMonths(1);
        }
        vm.IncidenciasPorMes = meses;

        // --- Proveedores con acuerdo marco incumpliendo plazos ---
        var plazosAm = await _db.Proveedores.AsNoTracking()
            .Where(pr => pr.AcuerdoMarco)
            .Select(pr => new { pr.Codigo, pr.Nombre, pr.PlazoEntregaDias })
            .ToListAsync(ct);
        vm.AcuerdoMarcoIncumplimientos = plazosAm
            .Select(pr =>
            {
                var suyos = rows.Where(r => r.Order.ProveedorCodigo == pr.Codigo).ToList();
                return new ProveedorIncumplimiento
                {
                    Codigo = pr.Codigo,
                    Nombre = pr.Nombre,
                    PlazoDias = pr.PlazoEntregaDias ?? p.PlazoAcuerdoMarcoDias,
                    PendientesTotales = suyos.Count,
                    PendientesFueraPlazo = suyos.Count(r => r.FueraPlazo),
                };
            })
            .Where(x => x.PendientesFueraPlazo > 0)
            .OrderByDescending(x => x.PendientesFueraPlazo)
            .Take(5)
            .ToList();

        return View(vm);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
