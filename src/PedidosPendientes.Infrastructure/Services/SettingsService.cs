using Microsoft.EntityFrameworkCore;
using PedidosPendientes.Core;
using PedidosPendientes.Core.Abstractions;
using PedidosPendientes.Core.Entities;
using PedidosPendientes.Infrastructure.Data;

namespace PedidosPendientes.Infrastructure.Services;

/// <summary>
/// Parámetros operativos guardados en la tabla AppSettings. Si una clave no existe,
/// se usa el valor por defecto de <see cref="ReclamacionParametros"/>.
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly AppDbContext _db;

    public SettingsService(AppDbContext db) => _db = db;

    public async Task<ReclamacionParametros> GetParametrosAsync(CancellationToken ct = default)
    {
        var keys = await _db.AppSettings.AsNoTracking().ToDictionaryAsync(s => s.Key, s => s.Value, ct);
        var p = new ReclamacionParametros();

        int Get(string key, int fallback)
            => keys.TryGetValue(key, out var v) && int.TryParse(v, out var n) && n > 0 ? n : fallback;

        p.PlazoReclamoA = Get(ReclamacionParametros.KeyPlazoReclamoA, p.PlazoReclamoA);
        p.PlazoReclamoB = Get(ReclamacionParametros.KeyPlazoReclamoB, p.PlazoReclamoB);
        p.PlazoReclamoC = Get(ReclamacionParametros.KeyPlazoReclamoC, p.PlazoReclamoC);
        p.PlazoRespuestaDias = Get(ReclamacionParametros.KeyPlazoRespuesta, p.PlazoRespuestaDias);
        p.PlazoLlamadaDias = Get(ReclamacionParametros.KeyPlazoLlamada, p.PlazoLlamadaDias);
        p.PlazoAcuerdoMarcoDias = Get(ReclamacionParametros.KeyPlazoAcuerdoMarco, p.PlazoAcuerdoMarcoDias);
        p.PlazoObjetivoEntregaDias = Get(ReclamacionParametros.KeyPlazoObjetivoEntrega, p.PlazoObjetivoEntregaDias);
        p.DiasAvisoCargaObsoleta = Get(ReclamacionParametros.KeyDiasAvisoCarga, p.DiasAvisoCargaObsoleta);
        return p;
    }

    public async Task SaveParametrosAsync(ReclamacionParametros p, CancellationToken ct = default)
    {
        var valores = new Dictionary<string, int>
        {
            [ReclamacionParametros.KeyPlazoReclamoA] = p.PlazoReclamoA,
            [ReclamacionParametros.KeyPlazoReclamoB] = p.PlazoReclamoB,
            [ReclamacionParametros.KeyPlazoReclamoC] = p.PlazoReclamoC,
            [ReclamacionParametros.KeyPlazoRespuesta] = p.PlazoRespuestaDias,
            [ReclamacionParametros.KeyPlazoLlamada] = p.PlazoLlamadaDias,
            [ReclamacionParametros.KeyPlazoAcuerdoMarco] = p.PlazoAcuerdoMarcoDias,
            [ReclamacionParametros.KeyPlazoObjetivoEntrega] = p.PlazoObjetivoEntregaDias,
            [ReclamacionParametros.KeyDiasAvisoCarga] = p.DiasAvisoCargaObsoleta,
        };

        var claves = valores.Keys.ToList();
        var existentes = await _db.AppSettings.Where(s => claves.Contains(s.Key)).ToListAsync(ct);
        var indice = existentes.ToDictionary(s => s.Key);

        foreach (var (key, value) in valores)
        {
            if (indice.TryGetValue(key, out var setting))
                setting.Value = value.ToString();
            else
                _db.AppSettings.Add(new AppSetting { Key = key, Value = value.ToString() });
        }

        await _db.SaveChangesAsync(ct);
    }
}
