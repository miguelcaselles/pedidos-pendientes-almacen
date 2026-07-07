using Microsoft.EntityFrameworkCore;
using PedidosPendientes.Core;
using PedidosPendientes.Core.Abstractions;
using PedidosPendientes.Core.Entities;
using PedidosPendientes.Infrastructure.Data;
using PedidosPendientes.Web.Models;

namespace PedidosPendientes.Web.Services;

/// <summary>
/// Enriquecimiento de líneas de pedido para las vistas: cruza cada línea con la
/// clasificación A/B/C del material, la criticidad de su almacén/centro de coste,
/// las respuestas del proveedor y los plazos de acuerdo marco, y calcula la alerta
/// de reclamación y el riesgo de retraso.
/// </summary>
public class PedidosOverviewService
{
    private readonly AppDbContext _db;
    private readonly ISettingsService _settings;

    public PedidosOverviewService(AppDbContext db, ISettingsService settings)
    {
        _db = db;
        _settings = settings;
    }

    public Task<ReclamacionParametros> GetParametrosAsync(CancellationToken ct)
        => _settings.GetParametrosAsync(ct);

    /// <summary>Todas las líneas pendientes (ni recibidas, ni con historial, ni anuladas).</summary>
    public Task<List<Order>> GetPendientesAsync(CancellationToken ct)
        => _db.Orders.AsNoTracking()
            .Where(o => !o.Recibido && !o.TieneHistorialEntrega && !o.Anulado)
            .ToListAsync(ct);

    public async Task<List<OrderRow>> EnrichAsync(IReadOnlyList<Order> orders,
        ReclamacionParametros p, CancellationToken ct)
    {
        if (orders.Count == 0) return [];

        var hoy = DateOnly.FromDateTime(DateTime.Today);

        // Clasificación A/B/C de los materiales implicados.
        var materiales = orders.Select(o => o.Material).Distinct().ToList();
        var clases = await _db.MaterialClasificaciones.AsNoTracking()
            .Where(m => materiales.Contains(m.Material))
            .ToDictionaryAsync(m => m.Material, m => m.Clase, ct);

        // Criticidad por almacén / centro de coste (tabla pequeña: se carga entera).
        var criticidades = await _db.CriticidadUbicaciones.AsNoTracking().ToListAsync(ct);
        var critAlmacen = criticidades.Where(c => c.Tipo == "almacen")
            .GroupBy(c => c.Codigo).ToDictionary(g => g.Key, g => g.First());
        var critCentro = criticidades.Where(c => c.Tipo == "centroCoste")
            .GroupBy(c => c.Codigo).ToDictionary(g => g.Key, g => g.First());

        // Respuestas del proveedor por documento (para saber si contestó tras la reclamación).
        var docs = orders.Select(o => o.DocumentoCompras).Distinct().ToList();
        var respuestas = await _db.ProviderResponses.AsNoTracking()
            .Where(r => docs.Contains(r.DocumentoCompras))
            .Select(r => new { r.DocumentoCompras, r.Material, r.CreatedAt })
            .ToListAsync(ct);
        var respuestasPorDoc = respuestas.GroupBy(r => r.DocumentoCompras)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Plazos de acuerdo marco por proveedor.
        var codigos = orders.Where(o => o.ProveedorCodigo != null)
            .Select(o => o.ProveedorCodigo!).Distinct().ToList();
        var proveedores = await _db.Proveedores.AsNoTracking()
            .Where(pr => codigos.Contains(pr.Codigo))
            .Select(pr => new { pr.Codigo, pr.AcuerdoMarco, pr.PlazoEntregaDias })
            .ToDictionaryAsync(pr => pr.Codigo, ct);

        var rows = new List<OrderRow>(orders.Count);
        foreach (var o in orders)
        {
            clases.TryGetValue(o.Material, out var clase);

            CriticidadUbicacion? crit = null;
            if (o.CentroCoste != null && critCentro.TryGetValue(o.CentroCoste, out var cc)) crit = cc;
            if (o.Almacen != null && critAlmacen.TryGetValue(o.Almacen, out var ca)
                && (crit is null || (crit.Nivel != "critico" && ca.Nivel == "critico"))) crit = ca;

            var tieneRespuesta = o.ReclamadoAt is not null
                && respuestasPorDoc.TryGetValue(o.DocumentoCompras, out var rs)
                && rs.Any(r => (r.Material == null || r.Material == o.Material) && r.CreatedAt > o.ReclamadoAt);

            var acuerdoMarco = false;
            int? plazoProveedor = null;
            if (o.ProveedorCodigo != null && proveedores.TryGetValue(o.ProveedorCodigo, out var prov) && prov.AcuerdoMarco)
            {
                acuerdoMarco = true;
                plazoProveedor = prov.PlazoEntregaDias ?? p.PlazoAcuerdoMarcoDias;
            }

            rows.Add(new OrderRow
            {
                Order = o,
                Clase = clase,
                CriticidadNivel = crit?.Nivel,
                CriticidadDescripcion = crit?.Descripcion ?? (crit is null ? null : crit.Codigo),
                Alerta = ReclamacionRules.Alerta(o, clase, tieneRespuesta, p, hoy),
                Riesgo = ReclamacionRules.RiesgoRetraso(o, plazoProveedor, p, hoy),
                DiasHabiles = BusinessDays.CountBusinessDays(o.FechaDocumento, hoy),
                AcuerdoMarco = acuerdoMarco,
            });
        }

        return rows;
    }

    /// <summary>Orden de prioridad para las vistas: alerta &gt; criticidad &gt; riesgo.</summary>
    public static IEnumerable<OrderRow> Priorizar(IEnumerable<OrderRow> rows) => rows
        .OrderByDescending(r => (int)r.Alerta)
        .ThenByDescending(r => r.CriticidadNivel == "critico" ? 2 : r.CriticidadNivel == "alto" ? 1 : 0)
        .ThenByDescending(r => r.Riesgo);
}
