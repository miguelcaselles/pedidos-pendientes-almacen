using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PedidosPendientes.Core.Abstractions;
using PedidosPendientes.Core.Dtos;
using PedidosPendientes.Core.Entities;
using PedidosPendientes.Infrastructure.Data;
using PedidosPendientes.Infrastructure.Excel;

namespace PedidosPendientes.Infrastructure.Services;

/// <summary>
/// Importación del MD04: cada carga es una foto completa del día, así que se
/// reemplaza íntegramente la carga anterior.
/// </summary>
public class Md04ImportService : IMd04ImportService
{
    private readonly AppDbContext _db;
    private readonly ILogger<Md04ImportService> _log;

    public Md04ImportService(AppDbContext db, ILogger<Md04ImportService> log)
    {
        _db = db;
        _log = log;
    }

    public async Task<UploadResult> ImportAsync(Stream xlsx, string fileName, CancellationToken ct = default)
    {
        IReadOnlyList<ParsedMd04Item> parsed;
        try
        {
            parsed = Md04ExcelParser.Parse(xlsx);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error al parsear el Excel MD04");
            return await LogAsync(fileName, new UploadResult
            {
                Success = false,
                Message = "El archivo MD04 no se pudo leer. Verifica que es el listado de NEXUS correcto.",
            }, ct);
        }

        if (parsed.Count == 0)
            return await LogAsync(fileName, new UploadResult
            {
                Success = false,
                Message = "El archivo MD04 no contiene filas válidas.",
            }, ct);

        var now = DateTimeOffset.UtcNow;

        // Instantánea: eliminar la carga anterior y guardar la nueva.
        var anteriores = await _db.StockMd04.ToListAsync(ct);
        _db.StockMd04.RemoveRange(anteriores);

        // Deduplicar por material+almacén (nos quedamos con la primera fila).
        var deduped = parsed
            .GroupBy(p => (p.Material, p.Almacen))
            .Select(g => g.First());

        int filas = 0;
        foreach (var p in deduped)
        {
            _db.StockMd04.Add(new StockMd04Item
            {
                Material = p.Material,
                TextoBreve = p.TextoBreve,
                Almacen = p.Almacen,
                Semaforo = p.Semaforo,
                Stock = p.Stock,
                PuntoPedido = p.PuntoPedido,
                ConsumoMedio = p.ConsumoMedio,
                CargadoAt = now,
            });
            filas++;
        }

        await _db.SaveChangesAsync(ct);

        var rojos = parsed.Count(p => p.Semaforo == "rojo");
        return await LogAsync(fileName, new UploadResult
        {
            Success = true,
            TotalParsed = parsed.Count,
            Insertados = filas,
            Message = $"MD04 cargado: {filas} materiales ({rojos} en rojo).",
        }, ct);
    }

    private async Task<UploadResult> LogAsync(string fileName, UploadResult r, CancellationToken ct)
    {
        _db.UploadLogs.Add(new UploadLog
        {
            Tipo = "md04",
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
