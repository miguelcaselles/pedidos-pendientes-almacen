using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PedidosPendientes.Core.Abstractions;
using PedidosPendientes.Core.Dtos;
using PedidosPendientes.Core.Entities;
using PedidosPendientes.Infrastructure.Data;
using PedidosPendientes.Infrastructure.Excel;

namespace PedidosPendientes.Infrastructure.Services;

/// <summary>
/// Importación de la relación CECOs - Almacenes: instantánea completa que
/// sustituye la carga anterior. No toca la parametrización de criticidad
/// (CriticidadUbicaciones), que es independiente y se conserva.
/// </summary>
public class CecosImportService : ICecosImportService
{
    private readonly AppDbContext _db;
    private readonly ILogger<CecosImportService> _log;

    public CecosImportService(AppDbContext db, ILogger<CecosImportService> log)
    {
        _db = db;
        _log = log;
    }

    public async Task<UploadResult> ImportAsync(Stream xlsx, string fileName, CancellationToken ct = default)
    {
        IReadOnlyList<ParsedAlmacenCeco> parsed;
        try
        {
            parsed = CecosExcelParser.Parse(xlsx);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error al parsear el Excel de relación CECOs - Almacenes");
            return await LogAsync(fileName, new UploadResult
            {
                Success = false,
                Message = "El archivo de relación CECOs - Almacenes no se pudo leer. Verifica que es el listado correcto.",
            }, ct);
        }

        if (parsed.Count == 0)
            return await LogAsync(fileName, new UploadResult
            {
                Success = false,
                Message = "El archivo de relación CECOs - Almacenes no contiene filas válidas.",
            }, ct);

        var now = DateTimeOffset.UtcNow;

        var anteriores = await _db.AlmacenCecos.ToListAsync(ct);
        _db.AlmacenCecos.RemoveRange(anteriores);

        // Deduplicar por centro + almacén (nos quedamos con la primera fila).
        var deduped = parsed
            .GroupBy(p => (p.Centro, p.Almacen))
            .Select(g => g.First());

        int filas = 0;
        foreach (var p in deduped)
        {
            _db.AlmacenCecos.Add(new AlmacenCeco
            {
                Centro = p.Centro,
                Almacen = p.Almacen,
                DenominacionAlmacen = p.DenominacionAlmacen,
                CentroCoste = p.CentroCoste,
                DenominacionCentroCoste = p.DenominacionCentroCoste,
                Descripcion = p.Descripcion,
                CargadoAt = now,
            });
            filas++;
        }

        await _db.SaveChangesAsync(ct);

        return await LogAsync(fileName, new UploadResult
        {
            Success = true,
            TotalParsed = parsed.Count,
            Insertados = filas,
            Message = $"Relación CECOs - Almacenes cargada: {filas} almacenes.",
        }, ct);
    }

    private async Task<UploadResult> LogAsync(string fileName, UploadResult r, CancellationToken ct)
    {
        _db.UploadLogs.Add(new UploadLog
        {
            Tipo = "cecos",
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
}
