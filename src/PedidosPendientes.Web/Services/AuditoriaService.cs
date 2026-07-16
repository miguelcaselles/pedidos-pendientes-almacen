using Microsoft.EntityFrameworkCore;
using PedidosPendientes.Core;
using PedidosPendientes.Core.Entities;
using PedidosPendientes.Infrastructure.Data;
using PedidosPendientes.Web.Models;

namespace PedidosPendientes.Web.Services;

/// <summary>
/// Auditoría de proveedores: calcula, a partir de todo el histórico de pedidos,
/// respuestas y correos, las métricas de cumplimiento de cada proveedor
/// (tiempos de entrega, reclamaciones, tasa de respuesta, desviaciones de plazo)
/// tanto de forma global como en detalle individual.
/// </summary>
public class AuditoriaService
{
    private readonly AppDbContext _db;
    private readonly PedidosOverviewService _overview;

    public AuditoriaService(AppDbContext db, PedidosOverviewService overview)
    {
        _db = db;
        _overview = overview;
    }

    private static int PlazoDe(Proveedor pr, ReclamacionParametros p) =>
        pr.PlazoEntregaDias ?? (pr.AcuerdoMarco ? p.PlazoAcuerdoMarcoDias : p.PlazoObjetivoEntregaDias);

    private static double? Mediana(IReadOnlyList<int> xs)
    {
        if (xs.Count == 0) return null;
        var s = xs.OrderBy(x => x).ToList();
        int m = s.Count / 2;
        return s.Count % 2 == 1 ? s[m] : (s[m - 1] + s[m]) / 2.0;
    }

    private static (double score, string calif) Calificar(int total, double? pctATiempo, int pendientes,
        int pendFueraPlazo, int docsReclam, int docsRespondidos)
    {
        if (total == 0) return (0, "Sin datos");
        double entregaScore = pctATiempo.HasValue
            ? pctATiempo.Value / 100.0
            : (pendientes > 0 ? 1 - (double)pendFueraPlazo / pendientes : 1);
        double pendScore = pendientes > 0 ? 1 - (double)pendFueraPlazo / pendientes : 1;
        double respScore = docsReclam > 0 ? (double)docsRespondidos / docsReclam : 1;
        double score = 100 * (0.5 * entregaScore + 0.3 * pendScore + 0.2 * respScore);
        score = Math.Clamp(score, 0, 100);
        string calif = score >= 85 ? "Excelente" : score >= 70 ? "Bueno" : score >= 50 ? "Mejorable" : "Deficiente";
        return (score, calif);
    }

    // --- Carga única del snapshot para no golpear la BD por proveedor ---
    private async Task<Snapshot> LoadAsync(CancellationToken ct)
    {
        var p = await _overview.GetParametrosAsync(ct);
        var proveedores = await _db.Proveedores.AsNoTracking().Where(pr => pr.Activo).ToListAsync(ct);
        var orders = await _db.Orders.AsNoTracking().ToListAsync(ct);
        var respuestas = await _db.ProviderResponses.AsNoTracking().ToListAsync(ct);
        var emails = await _db.EmailLogs.AsNoTracking().ToListAsync(ct);
        return new Snapshot(p, proveedores, orders, respuestas, emails);
    }

    private sealed record Snapshot(ReclamacionParametros P, List<Proveedor> Proveedores,
        List<Order> Orders, List<ProviderResponse> Respuestas, List<EmailLog> Emails);

    private AuditoriaProveedorRow Calcular(Proveedor pr, IReadOnlyList<Order> suyos, Snapshot snap, DateOnly hoy)
    {
        int plazo = PlazoDe(pr, snap.P);
        var docs = suyos.Select(o => o.DocumentoCompras).ToHashSet();

        int total = suyos.Count;
        int anuladas = suyos.Count(o => o.Anulado);
        var recibidas = suyos.Where(o => o.Recibido).ToList();
        var pendientes = suyos.Where(o => !o.Recibido && !o.Anulado).ToList();
        int enFalta = pendientes.Count(o => o.EnFalta);

        var diasEntrega = recibidas.Where(o => o.RecibidoAt is not null)
            .Select(o => BusinessDays.CountBusinessDays(o.FechaDocumento,
                DateOnly.FromDateTime(o.RecibidoAt!.Value.LocalDateTime)))
            .ToList();
        int aTiempo = diasEntrega.Count(d => d <= plazo);
        int tarde = diasEntrega.Count(d => d > plazo);
        double? pctATiempo = diasEntrega.Count > 0 ? 100.0 * aTiempo / diasEntrega.Count : null;

        int pendFueraPlazo = pendientes.Count(o => BusinessDays.CountBusinessDays(o.FechaDocumento, hoy) > plazo);

        var reclamadas = suyos.Where(o => o.Reclamado).ToList();
        var tiemposReclamo = reclamadas.Where(o => o.ReclamadoAt is not null)
            .Select(o => BusinessDays.CountBusinessDays(o.FechaDocumento,
                DateOnly.FromDateTime(o.ReclamadoAt!.Value.LocalDateTime)))
            .ToList();

        int emailsEnv = snap.Emails.Count(e => e.Estado == "enviado"
            && (docs.Contains(e.DocumentoCompras) || (pr.Email != null && e.ProveedorEmail == pr.Email)));

        var respProv = snap.Respuestas.Where(r => docs.Contains(r.DocumentoCompras)
            || (r.ProveedorId != null && r.ProveedorId == pr.Id)
            || (r.ProveedorNombre != null && r.ProveedorNombre == pr.Nombre)).ToList();
        var docsReclam = reclamadas.Select(o => o.DocumentoCompras).Distinct().ToHashSet();
        int docsRespondidos = respProv.Select(r => r.DocumentoCompras).Distinct().Count(d => docsReclam.Contains(d));
        double? tasaResp = docsReclam.Count > 0 ? 100.0 * docsRespondidos / docsReclam.Count : null;

        var (score, calif) = Calificar(total, pctATiempo, pendientes.Count, pendFueraPlazo, docsReclam.Count, docsRespondidos);

        return new AuditoriaProveedorRow
        {
            Proveedor = pr,
            PlazoAplicable = plazo,
            TotalLineas = total,
            Pendientes = pendientes.Count,
            Recibidas = recibidas.Count,
            Anuladas = anuladas,
            EnFalta = enFalta,
            PendientesFueraPlazo = pendFueraPlazo,
            EntregadasATiempo = aTiempo,
            EntregadasTarde = tarde,
            PorcentajeATiempo = pctATiempo,
            TiempoMedioEntrega = diasEntrega.Count > 0 ? diasEntrega.Average() : null,
            TiempoMedianoEntrega = Mediana(diasEntrega),
            TiempoMaxEntrega = diasEntrega.Count > 0 ? diasEntrega.Max() : null,
            LineasReclamadas = reclamadas.Count,
            TotalReclamaciones = suyos.Sum(o => o.ReclamadoCount),
            Rereclamadas = suyos.Count(o => o.ReclamadoCount >= 2),
            TiempoMedioHastaReclamar = tiemposReclamo.Count > 0 ? tiemposReclamo.Average() : null,
            EmailsEnviados = emailsEnv,
            RespuestasRecibidas = respProv.Count,
            TasaRespuesta = tasaResp,
            Puntuacion = score,
            Calificacion = calif,
            SumaDiasEntrega = diasEntrega.Sum(),
        };
    }

    public async Task<AuditoriaGlobalViewModel> GetGlobalAsync(string? search, bool am, string? orden, CancellationToken ct)
    {
        var snap = await LoadAsync(ct);
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var porCodigo = snap.Orders.Where(o => o.ProveedorCodigo != null).ToLookup(o => o.ProveedorCodigo!);

        var proveedores = snap.Proveedores.AsEnumerable();
        if (am) proveedores = proveedores.Where(pr => pr.AcuerdoMarco);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            proveedores = proveedores.Where(pr =>
                pr.Nombre.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                pr.Codigo.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        var filas = proveedores.Select(pr => Calcular(pr, porCodigo[pr.Codigo].ToList(), snap, hoy)).ToList();

        orden = string.IsNullOrWhiteSpace(orden) ? "cumplimiento" : orden;
        filas = orden switch
        {
            "nombre" => filas.OrderBy(f => f.Proveedor.Nombre).ToList(),
            "entrega" => filas.OrderByDescending(f => f.TiempoMedioEntrega ?? -1).ToList(),
            "reclamaciones" => filas.OrderByDescending(f => f.TotalReclamaciones).ThenByDescending(f => f.LineasReclamadas).ToList(),
            "pendientes" => filas.OrderByDescending(f => f.PendientesFueraPlazo).ThenByDescending(f => f.Pendientes).ToList(),
            _ => filas.OrderByDescending(f => f.Incumple).ThenBy(f => f.Puntuacion).ToList(),
        };

        int totalDiasEntregaCount = filas.Sum(f => f.EntregadasATiempo + f.EntregadasTarde);
        double sumDias = filas.Sum(f => f.SumaDiasEntrega);
        int recibidas = filas.Sum(f => f.Recibidas);
        int aTiempo = filas.Sum(f => f.EntregadasATiempo);
        int reclamadas = filas.Sum(f => f.LineasReclamadas);
        int respondidas = filas.Sum(f => f.RespuestasRecibidas);

        var distrib = new[] { "Excelente", "Bueno", "Mejorable", "Deficiente", "Sin datos" }
            .Select(c => (Calificacion: c, Count: filas.Count(f => f.Calificacion == c)))
            .Where(x => x.Count > 0).ToList();

        return new AuditoriaGlobalViewModel
        {
            Search = search,
            SoloAcuerdoMarco = am,
            Orden = orden,
            GeneradoEl = DateTime.Now,
            TotalProveedores = filas.Count,
            ProveedoresAcuerdoMarco = filas.Count(f => f.Proveedor.AcuerdoMarco),
            ProveedoresIncumplen = filas.Count(f => f.Incumple),
            TotalLineas = filas.Sum(f => f.TotalLineas),
            LineasRecibidas = recibidas,
            LineasPendientes = filas.Sum(f => f.Pendientes),
            LineasAnuladas = filas.Sum(f => f.Anuladas),
            LineasEnFalta = filas.Sum(f => f.EnFalta),
            PendientesFueraPlazo = filas.Sum(f => f.PendientesFueraPlazo),
            EntregadasATiempo = aTiempo,
            EntregadasTarde = filas.Sum(f => f.EntregadasTarde),
            PorcentajeEntregaATiempo = totalDiasEntregaCount > 0 ? 100.0 * aTiempo / totalDiasEntregaCount : null,
            TiempoMedioEntregaGlobal = totalDiasEntregaCount > 0 ? sumDias / totalDiasEntregaCount : null,
            LineasReclamadas = reclamadas,
            TotalReclamaciones = filas.Sum(f => f.TotalReclamaciones),
            RespuestasProveedor = respondidas,
            TasaRespuestaGlobal = reclamadas > 0 ? 100.0 * Math.Min(respondidas, reclamadas) / reclamadas : null,
            Filas = filas,
            DistribucionCalificacion = distrib,
        };
    }

    public async Task<AuditoriaProveedorDetalle?> GetProveedorAsync(string codigo, CancellationToken ct)
    {
        var snap = await LoadAsync(ct);
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var pr = snap.Proveedores.FirstOrDefault(x => x.Codigo == codigo);
        if (pr is null) return null;

        var suyos = snap.Orders.Where(o => o.ProveedorCodigo == codigo).ToList();
        var resumen = Calcular(pr, suyos, snap, hoy);

        var materiales = suyos.Select(o => o.Material).Distinct().ToList();
        var clases = await _db.MaterialClasificaciones.AsNoTracking()
            .Where(m => materiales.Contains(m.Material))
            .ToDictionaryAsync(m => m.Material, m => m.Clase, ct);
        string ClaseDe(Order o) => clases.TryGetValue(o.Material, out var c) ? c : "C";

        int plazo = resumen.PlazoAplicable;
        var docs = suyos.Select(o => o.DocumentoCompras).ToHashSet();
        var respProv = snap.Respuestas.Where(r => docs.Contains(r.DocumentoCompras)
            || (r.ProveedorId != null && r.ProveedorId == pr.Id)
            || (r.ProveedorNombre != null && r.ProveedorNombre == pr.Nombre))
            .OrderByDescending(r => r.CreatedAt).ToList();

        bool TieneRespuesta(Order o) => respProv.Any(r => r.DocumentoCompras == o.DocumentoCompras
            && (r.Material == null || r.Material == o.Material));

        // Líneas de auditoría
        var lineas = suyos
            .OrderByDescending(o => o.FechaDocumento)
            .Select(o =>
            {
                bool recibido = o.Recibido;
                int? diasEntrega = recibido && o.RecibidoAt is not null
                    ? BusinessDays.CountBusinessDays(o.FechaDocumento, DateOnly.FromDateTime(o.RecibidoAt.Value.LocalDateTime))
                    : null;
                string estado = o.Anulado ? "Anulado" : recibido ? "Recibido" : o.EnFalta ? "En falta" : "Pendiente";
                bool fueraPlazo = recibido
                    ? (diasEntrega ?? 0) > plazo
                    : (!o.Anulado && BusinessDays.CountBusinessDays(o.FechaDocumento, hoy) > plazo);
                return new LineaAuditoria
                {
                    Order = o,
                    Clase = clases.TryGetValue(o.Material, out var c) ? c : null,
                    Estado = estado,
                    DiasHabiles = BusinessDays.CountBusinessDays(o.FechaDocumento,
                        recibido && o.RecibidoAt is not null ? DateOnly.FromDateTime(o.RecibidoAt.Value.LocalDateTime) : hoy),
                    DiasEntrega = diasEntrega,
                    FueraPlazo = fueraPlazo,
                    Reclamado = o.Reclamado,
                    ReclamadoCount = o.ReclamadoCount,
                    TieneRespuesta = TieneRespuesta(o),
                };
            }).ToList();

        // Desglose por clase
        var porClase = new[] { "A", "B", "C" }.Select(cl =>
        {
            var g = suyos.Where(o => ClaseDe(o) == cl).ToList();
            return new ClaseAuditoria
            {
                Clase = cl,
                Lineas = g.Count,
                Pendientes = g.Count(o => !o.Recibido && !o.Anulado),
                FueraPlazo = g.Count(o => !o.Recibido && !o.Anulado
                    && BusinessDays.CountBusinessDays(o.FechaDocumento, hoy) > plazo),
            };
        }).Where(c => c.Lineas > 0).ToList();

        // Desglose por centro de coste
        var porCentro = suyos.GroupBy(o => o.CentroCoste ?? "—")
            .Select(g => new UbicacionAuditoria
            {
                Centro = g.Key,
                Lineas = g.Count(),
                FueraPlazo = g.Count(o => !o.Recibido && !o.Anulado
                    && BusinessDays.CountBusinessDays(o.FechaDocumento, hoy) > plazo),
            })
            .OrderByDescending(u => u.Lineas).ToList();

        // Respuestas por estado
        var respEstado = respProv.GroupBy(r => r.Estado)
            .Select(g => (Estado: g.Key, Count: g.Count()))
            .OrderByDescending(x => x.Count).ToList();

        // Evolución mensual (últimos 12 meses)
        var evol = new List<MesAuditoria>();
        var primerMes = new DateOnly(hoy.Year, hoy.Month, 1).AddMonths(-11);
        for (int i = 0; i < 12; i++)
        {
            var mes = primerMes.AddMonths(i);
            var siguiente = mes.AddMonths(1);
            bool EnMes(DateOnly d) => d >= mes && d < siguiente;
            bool EnMesTs(DateTimeOffset? t) => t is not null && EnMes(DateOnly.FromDateTime(t.Value.LocalDateTime));

            var recMes = suyos.Where(o => o.Recibido && EnMesTs(o.RecibidoAt)).ToList();
            var diasMes = recMes.Where(o => o.RecibidoAt is not null)
                .Select(o => BusinessDays.CountBusinessDays(o.FechaDocumento,
                    DateOnly.FromDateTime(o.RecibidoAt!.Value.LocalDateTime))).ToList();

            evol.Add(new MesAuditoria
            {
                Etiqueta = mes.ToString("MMM yy", new System.Globalization.CultureInfo("es-ES")),
                Pedidos = suyos.Count(o => EnMes(o.FechaDocumento)),
                Recibidos = recMes.Count,
                Reclamaciones = suyos.Count(o => EnMesTs(o.ReclamadoAt)),
                EnFalta = suyos.Count(o => EnMesTs(o.EnFaltaAt)),
                TiempoMedioEntrega = diasMes.Count > 0 ? diasMes.Average() : null,
            });
        }

        return new AuditoriaProveedorDetalle
        {
            Resumen = resumen,
            GeneradoEl = DateTime.Now,
            Evolucion = evol,
            PorClase = porClase,
            PorCentro = porCentro,
            RespuestasPorEstado = respEstado,
            Lineas = lineas,
            Respuestas = respProv,
        };
    }
}
