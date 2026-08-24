using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PedidosPendientes.Core.Abstractions;
using PedidosPendientes.Core.Dtos;
using PedidosPendientes.Core.Entities;
using PedidosPendientes.Infrastructure.Data;

namespace PedidosPendientes.Infrastructure.Services;

/// <summary>
/// Importación no destructiva: actualiza los datos que vienen del Excel pero
/// conserva el estado de gestión (recibido/reclamado/anulado/comentarios...).
/// </summary>
public class OrderImportService : IOrderImportService
{
    private readonly AppDbContext _db;
    private readonly IExcelOrderParser _parser;
    private readonly ILogger<OrderImportService> _log;

    public OrderImportService(AppDbContext db, IExcelOrderParser parser, ILogger<OrderImportService> log)
    {
        _db = db;
        _parser = parser;
        _log = log;
    }

    public async Task<UploadResult> ImportAsync(Stream xlsx, string fileName, CancellationToken ct = default)
    {
        List<ParsedOrder> parsed;
        try
        {
            parsed = _parser.Parse(xlsx).ToList();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error al parsear el Excel de pedidos");
            return await LogAsync(fileName, new UploadResult { Success = false, Message = "El archivo no se pudo leer. Verifica que es el Excel de pedidos pendientes correcto." }, ct);
        }

        if (parsed.Count == 0)
            return await LogAsync(fileName, new UploadResult { Success = false, Message = "El archivo no contiene filas de pedidos válidas." }, ct);

        // Deduplicar filas con la misma clave (Documento+Material): SAP puede exportar
        // líneas idénticas repetidas. Nos quedamos con la primera ocurrencia.
        var deduped = parsed
            .GroupBy(p => (p.DocumentoCompras, p.Material))
            .Select(g => g.First())
            .ToList();
        var descartadasPorDuplicado = parsed.Count - deduped.Count;

        var now = DateTimeOffset.UtcNow;

        // Asegurar proveedores antes de tocar pedidos.
        await EnsureProveedoresAsync(deduped, ct);

        // Cargar las líneas existentes que coinciden con la clave (en lotes razonables).
        var claves = deduped.Select(p => p.DocumentoCompras).Distinct().ToList();
        var existentes = await _db.Orders
            .Where(o => claves.Contains(o.DocumentoCompras))
            .ToListAsync(ct);

        var indice = existentes.ToDictionary(o => (o.DocumentoCompras, o.Material));

        int insertados = 0, actualizados = 0;

        foreach (var p in deduped)
        {
            if (indice.TryGetValue((p.DocumentoCompras, p.Material), out var existente))
            {
                // Actualización NO destructiva: solo los campos del Excel.
                existente.ExpedienteAdministrativo = p.ExpedienteAdministrativo;
                existente.CentroCoste = p.CentroCoste;
                existente.TipoImputacion = p.TipoImputacion;
                existente.TextoBreve = p.TextoBreve;
                existente.NumMaterialProveedor = p.NumMaterialProveedor;
                existente.FechaDocumento = p.FechaDocumento;
                existente.TieneHistorialEntrega = p.TieneHistorialEntrega;
                existente.ProveedorCodigo = p.ProveedorCodigo;
                existente.ProveedorNombre = p.ProveedorNombre;
                existente.ReferenciaProveedor = p.ReferenciaProveedor;
                existente.Almacen = p.Almacen;
                existente.CantidadPedido = p.CantidadPedido;
                existente.PorEntregarCantidad = p.PorEntregarCantidad;
                existente.ContratoMarco = p.ContratoMarco;
                existente.LastUploadAt = now;
                // NO se tocan: Recibido, Reclamado, Anulado, EnFalta, Comentarios, Incidencia, etc.
                actualizados++;
            }
            else
            {
                _db.Orders.Add(new Order
                {
                    DocumentoCompras = p.DocumentoCompras,
                    ExpedienteAdministrativo = p.ExpedienteAdministrativo,
                    CentroCoste = p.CentroCoste,
                    Material = p.Material,
                    TipoImputacion = p.TipoImputacion,
                    TextoBreve = p.TextoBreve,
                    NumMaterialProveedor = p.NumMaterialProveedor,
                    FechaDocumento = p.FechaDocumento,
                    TieneHistorialEntrega = p.TieneHistorialEntrega,
                    ProveedorCodigo = p.ProveedorCodigo,
                    ProveedorNombre = p.ProveedorNombre,
                    ReferenciaProveedor = p.ReferenciaProveedor,
                    Almacen = p.Almacen,
                    CantidadPedido = p.CantidadPedido,
                    PorEntregarCantidad = p.PorEntregarCantidad,
                    ContratoMarco = p.ContratoMarco,
                    LastUploadAt = now,
                });
                insertados++;
            }
        }

        await _db.SaveChangesAsync(ct);

        var msg = $"Importación correcta: {insertados} nuevos, {actualizados} actualizados.";
        if (descartadasPorDuplicado > 0)
            msg += $" Se descartaron {descartadasPorDuplicado} líneas duplicadas del fichero.";

        _log.LogInformation("Importación de pedidos: {Insertados} nuevos, {Actualizados} actualizados, {Dups} duplicados descartados",
            insertados, actualizados, descartadasPorDuplicado);

        return await LogAsync(fileName, new UploadResult
        {
            Success = true,
            TotalParsed = parsed.Count,
            Insertados = insertados,
            Actualizados = actualizados,
            Message = msg,
        }, ct);
    }

    /// <summary>Deja constancia de la carga en UploadLogs (estado de subida visible en la app).</summary>
    private async Task<UploadResult> LogAsync(string fileName, UploadResult r, CancellationToken ct)
    {
        _db.UploadLogs.Add(new UploadLog
        {
            Tipo = "pedidos",
            FileName = fileName,
            Success = r.Success,
            Filas = r.TotalParsed,
            Insertados = r.Insertados,
            Actualizados = r.Actualizados,
            Message = r.Message,
            At = DateTimeOffset.UtcNow,
        });
        await _db.SaveChangesAsync(ct);
        return r;
    }

    /// <summary>
    /// Crea proveedores que aparecen en el Excel pero aún no existen (sin email;
    /// quedan "incompletos") y marca el acuerdo marco de los que traen contrato
    /// marco en alguna línea del listado (nunca lo desmarca: eso es manual).
    /// </summary>
    private async Task EnsureProveedoresAsync(List<ParsedOrder> parsed, CancellationToken ct)
    {
        var codigos = parsed
            .Where(p => !string.IsNullOrWhiteSpace(p.ProveedorCodigo))
            .Select(p => p.ProveedorCodigo!)
            .Distinct()
            .ToList();
        if (codigos.Count == 0) return;

        var conContrato = parsed
            .Where(p => !string.IsNullOrWhiteSpace(p.ProveedorCodigo)
                        && !string.IsNullOrWhiteSpace(p.ContratoMarco))
            .Select(p => p.ProveedorCodigo!)
            .ToHashSet();

        var existentes = await _db.Proveedores
            .Where(pr => codigos.Contains(pr.Codigo))
            .ToListAsync(ct);
        var existentesSet = existentes.Select(pr => pr.Codigo).ToHashSet();

        foreach (var pr in existentes)
        {
            if (!pr.AcuerdoMarco && conContrato.Contains(pr.Codigo))
                pr.AcuerdoMarco = true;
        }

        foreach (var p in parsed)
        {
            if (string.IsNullOrWhiteSpace(p.ProveedorCodigo)) continue;
            if (existentesSet.Contains(p.ProveedorCodigo)) continue;

            _db.Proveedores.Add(new Proveedor
            {
                Codigo = p.ProveedorCodigo!,
                Nombre = p.ProveedorNombre ?? p.ProveedorCodigo!,
                Activo = true,
                AcuerdoMarco = conContrato.Contains(p.ProveedorCodigo),
            });
            existentesSet.Add(p.ProveedorCodigo);
        }
    }
}
