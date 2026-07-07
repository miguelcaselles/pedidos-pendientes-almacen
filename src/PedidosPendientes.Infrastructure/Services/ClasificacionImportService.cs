using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PedidosPendientes.Core.Abstractions;
using PedidosPendientes.Core.Dtos;
using PedidosPendientes.Core.Entities;
using PedidosPendientes.Infrastructure.Data;
using PedidosPendientes.Infrastructure.Excel;

namespace PedidosPendientes.Infrastructure.Services;

/// <summary>Importación (upsert por material) de la clasificación A/B/C por código NEXUS.</summary>
public class ClasificacionImportService : IClasificacionImportService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ClasificacionImportService> _log;

    public ClasificacionImportService(AppDbContext db, ILogger<ClasificacionImportService> log)
    {
        _db = db;
        _log = log;
    }

    public async Task<UploadResult> ImportAsync(Stream xlsx, string fileName, CancellationToken ct = default)
    {
        IReadOnlyList<ParsedClasificacion> parsed;
        try
        {
            parsed = ClasificacionExcelParser.Parse(xlsx);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error al parsear el Excel de clasificación ABC");
            return await LogAsync(fileName, new UploadResult
            {
                Success = false,
                Message = "El archivo de clasificación no se pudo leer. Debe tener una columna de material y otra con la clase (A/B/C).",
            }, ct);
        }

        if (parsed.Count == 0)
            return await LogAsync(fileName, new UploadResult
            {
                Success = false,
                Message = "El archivo no contiene filas con clase A/B/C válidas.",
            }, ct);

        // Última ocurrencia de cada material gana.
        var porMaterial = parsed
            .GroupBy(p => p.Material)
            .ToDictionary(g => g.Key, g => g.Last().Clase);

        var materiales = porMaterial.Keys.ToList();
        var existentes = await _db.MaterialClasificaciones
            .Where(m => materiales.Contains(m.Material))
            .ToListAsync(ct);
        var indice = existentes.ToDictionary(m => m.Material);

        int insertados = 0, actualizados = 0;
        foreach (var (material, clase) in porMaterial)
        {
            if (indice.TryGetValue(material, out var existente))
            {
                if (existente.Clase != clase) { existente.Clase = clase; actualizados++; }
            }
            else
            {
                _db.MaterialClasificaciones.Add(new MaterialClasificacion { Material = material, Clase = clase });
                insertados++;
            }
        }

        await _db.SaveChangesAsync(ct);

        return await LogAsync(fileName, new UploadResult
        {
            Success = true,
            TotalParsed = parsed.Count,
            Insertados = insertados,
            Actualizados = actualizados,
            Message = $"Clasificación ABC cargada: {insertados} materiales nuevos, {actualizados} actualizados.",
        }, ct);
    }

    private async Task<UploadResult> LogAsync(string fileName, UploadResult r, CancellationToken ct)
    {
        _db.UploadLogs.Add(new UploadLog
        {
            Tipo = "abc",
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
