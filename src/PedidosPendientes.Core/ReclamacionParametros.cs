namespace PedidosPendientes.Core;

/// <summary>
/// Parámetros operativos de reclamación. Se guardan en AppSettings (editables desde
/// la pantalla de Configuración) y aquí se definen sus valores por defecto.
/// Todos los plazos son en días hábiles.
/// </summary>
public class ReclamacionParametros
{
    /// <summary>Días desde la fecha del pedido para reclamar un material de clase A.</summary>
    public int PlazoReclamoA { get; set; } = 2;

    /// <summary>Días desde la fecha del pedido para reclamar un material de clase B.</summary>
    public int PlazoReclamoB { get; set; } = 4;

    /// <summary>Días desde la fecha del pedido para reclamar un material de clase C (y sin clasificar).</summary>
    public int PlazoReclamoC { get; set; } = 6;

    /// <summary>Días tras la reclamación sin respuesta del proveedor para alertar.</summary>
    public int PlazoRespuestaDias { get; set; } = 3;

    /// <summary>Días adicionales sin respuesta para pasar a "llamar por teléfono".</summary>
    public int PlazoLlamadaDias { get; set; } = 3;

    /// <summary>Plazo de entrega por defecto (días hábiles) para proveedores con acuerdo marco sin plazo propio.</summary>
    public int PlazoAcuerdoMarcoDias { get; set; } = 10;

    /// <summary>Plazo objetivo de entrega global (días hábiles) usado para medir el riesgo de retraso.</summary>
    public int PlazoObjetivoEntregaDias { get; set; } = 10;

    /// <summary>Días naturales sin cargar el Excel de pedidos a partir de los cuales la app avisa de datos obsoletos.</summary>
    public int DiasAvisoCargaObsoleta { get; set; } = 2;

    public int PlazoReclamo(string? clase) => clase switch
    {
        "A" => PlazoReclamoA,
        "B" => PlazoReclamoB,
        _ => PlazoReclamoC,
    };

    // Claves en AppSettings.
    public const string KeyPlazoReclamoA = "plazo.reclamo.A";
    public const string KeyPlazoReclamoB = "plazo.reclamo.B";
    public const string KeyPlazoReclamoC = "plazo.reclamo.C";
    public const string KeyPlazoRespuesta = "plazo.respuesta";
    public const string KeyPlazoLlamada = "plazo.llamada";
    public const string KeyPlazoAcuerdoMarco = "plazo.acuerdoMarco";
    public const string KeyPlazoObjetivoEntrega = "plazo.objetivoEntrega";
    public const string KeyDiasAvisoCarga = "carga.diasAviso";
}
