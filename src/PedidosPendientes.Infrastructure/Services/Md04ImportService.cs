using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PedidosPendientes.Core.Abstractions;
using PedidosPendientes.Core.Dtos;
using PedidosPendientes.Core.Entities;
using PedidosPendientes.Infrastructure.Data;
using PedidosPendientes.Infrastructure.Excel;

namespace PedidosPendientes.Infrastructure.Services;

/// <summary>
/// Importación del MD04: cada carga es una foto completa del día de su ámbito
/// (almacenaje o no almacenaje), así que se reemplaza íntegramente la carga
/// anterior del mismo ámbito sin tocar la del otro.
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

    public async Task<UploadResult> ImportAsync(Stream xlsx, string fileName, string ambito, CancellationToken ct = default)
    {
        ambito = ambito == "noalmacenaje" ? "noalmacenaje" : "almacenaje";
        var tipoLog = ambito == "noalmacenaje" ? "md04na" : "md04";
        var etiqueta = ambito == "noalmacenaje" ? "MD04 no almacenaje" : "MD04 almacenaje";

        IReadOnlyList<ParsedMd04Item> parsed;
        Md04ExcelParser.Md04ParseInfo? info = null;
        try
        {
            parsed = Md04ExcelParser.Parse(xlsx, out info);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error al parsear el Excel MD04 ({Ambito})", ambito);
            return await LogAsync(tipoLog, fileName, new UploadResult
            {
                Success = false,
                Message = $"El archivo {etiqueta} no se pudo leer. Verifica que es el listado de NEXUS correcto.",
            }, ct);
        }

        if (parsed.Count == 0)
            return await LogAsync(tipoLog, fileName, new UploadResult
            {
                Success = false,
                Message = $"El archivo {etiqueta} no contiene filas válidas.",
            }, ct);

        var now = DateTimeOffset.UtcNow;

        // Instantánea por ámbito: eliminar la carga anterior del mismo ámbito.
        var anteriores = await _db.StockMd04.Where(s => s.Ambito == ambito).ToListAsync(ct);
        _db.StockMd04.RemoveRange(anteriores);

        // Deduplicar por material + área/almacén (nos quedamos con la primera fila).
        var deduped = parsed
            .GroupBy(p => (p.Material, p.AreaPlanificacion, p.Almacen))
            .Select(g => g.First());

        int filas = 0;
        foreach (var p in deduped)
        {
            _db.StockMd04.Add(new StockMd04Item
            {
                Ambito = ambito,
                Material = p.Material,
                TextoBreve = p.TextoBreve,
                Almacen = p.Almacen,
                Semaforo = p.Semaforo,
                StatusNexus = p.StatusNexus,
                AreaPlanificacion = p.AreaPlanificacion,
                PedidosFueraPlazo = p.PedidosFueraPlazo,
                BajoStockSeguridad = p.BajoStockSeguridad,
                Stock = p.Stock,
                PuntoPedido = p.PuntoPedido,
                ConsumoMedio = p.ConsumoMedio,
                CargadoAt = now,
            });
            filas++;
        }

        await _db.SaveChangesAsync(ct);

        var rojos = parsed.Count(p => p.Semaforo == "rojo");
        var fueraPlazo = parsed.Count(p => p.PedidosFueraPlazo > 0);
        var bajoSeguridad = parsed.Count(p => p.BajoStockSeguridad > 0);
        var msg = $"{etiqueta} cargado: {filas} materiales ({rojos} en rojo, " +
                  $"{fueraPlazo} con pedido fuera de plazo, {bajoSeguridad} bajo stock de seguridad).";

        // Autodiagnóstico: si el fichero no trae las columnas clave con un nombre
        // reconocible, se avisa y se listan las cabeceras reales para poder
        // adaptar el parser sin necesidad de acceder al fichero.
        if (info is { ColumnasNoReconocidas.Count: > 0 })
        {
            msg += $" AVISO: no se reconocieron las columnas {string.Join(", ", info.ColumnasNoReconocidas)}" +
                   $" — sin ellas el semáforo y las excepciones pueden quedar incompletos." +
                   $" Cabeceras del fichero: {string.Join(" · ", info.Cabeceras.Take(14))}.";
        }

        return await LogAsync(tipoLog, fileName, new UploadResult
        {
            Success = true,
            TotalParsed = parsed.Count,
            Insertados = filas,
            Message = msg,
        }, ct);
    }

    private async Task<UploadResult> LogAsync(string tipo, string fileName, UploadResult r, CancellationToken ct)
    {
        _db.UploadLogs.Add(new UploadLog
        {
            Tipo = tipo,
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
