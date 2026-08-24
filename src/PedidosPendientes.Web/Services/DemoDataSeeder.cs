using Microsoft.EntityFrameworkCore;
using PedidosPendientes.Core.Entities;
using PedidosPendientes.Infrastructure.Data;

namespace PedidosPendientes.Web.Services;

/// <summary>
/// Crea un escenario demostrativo completo y efímero. Program.cs sólo llama a
/// este seeder cuando la base de datos NO es relacional, por lo que nunca puede
/// insertar datos ficticios en el SQL Server corporativo.
/// </summary>
public static class DemoDataSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (await db.Orders.AnyAsync(ct)) return;

        var now = DateTimeOffset.Now;
        var today = DateOnly.FromDateTime(DateTime.Today);
        DateOnly Ago(int days) => today.AddDays(-days);

        var proveedores = new[]
        {
            new Proveedor { Codigo = "D10001", Nombre = "Demo Medical Centro, S.L.", Email = "pedidos@medical-centro.example", Telefono = "910 000 101", NifCif = "B00000001", Poblacion = "Madrid", Centro = "HUF", Activo = true, AcuerdoMarco = true, PlazoEntregaDias = 7, NotasPenalizacion = "DEMO · Seguimiento mensual de cumplimiento." },
            new Proveedor { Codigo = "D10002", Nombre = "Suministros Sanitarios Demo, S.A.", Email = "logistica@sani-demo.example", Telefono = "910 000 202", NifCif = "A00000002", Poblacion = "Fuenlabrada", Centro = "HUF", Activo = true, AcuerdoMarco = true, PlazoEntregaDias = 5 },
            new Proveedor { Codigo = "D10003", Nombre = "Textil Clínico Ejemplo, S.L.", Email = "hospitales@textil-ejemplo.example", Telefono = "910 000 303", NifCif = "B00000003", Poblacion = "Getafe", Centro = "HUF", Activo = true },
            new Proveedor { Codigo = "D10004", Nombre = "Iberia Técnica Hospitalaria Demo", Email = "suministros@iberia-demo.example", Telefono = "910 000 404", NifCif = "A00000004", Poblacion = "Leganés", Centro = "HUF", Activo = true, AcuerdoMarco = true },
            new Proveedor { Codigo = "D10005", Nombre = "Proveedor Pendiente de Contacto (Demo)", Telefono = "910 000 505", NifCif = "B00000005", Poblacion = "Madrid", Centro = "HUF", Activo = true },
        };
        db.Proveedores.AddRange(proveedores);

        Order Order(string doc, string material, string descripcion, int daysAgo,
            string proveedor, string proveedorNombre, string almacen, string centro,
            decimal cantidad) => new()
            {
                DocumentoCompras = doc,
                ExpedienteAdministrativo = $"DEMO-{doc[^3..]}/2026",
                CentroCoste = centro,
                Material = material,
                TipoImputacion = "K",
                TextoBreve = descripcion,
                NumMaterialProveedor = $"REF-{material}",
                FechaDocumento = Ago(daysAgo),
                ProveedorCodigo = proveedor,
                ProveedorNombre = proveedorNombre,
                ReferenciaProveedor = $"PED-DEMO-{doc[^3..]}",
                Almacen = almacen,
                PorEntregarCantidad = cantidad,
                LastUploadAt = now.AddMinutes(-38),
            };

        var orders = new List<Order>
        {
            Order("4500098101", "MAT-10001", "Guantes de nitrilo sin polvo · talla M", 18, "D10001", proveedores[0].Nombre, "ALM-CENTRAL", "UCI", 120),
            Order("4500098102", "MAT-10002", "Mascarilla quirúrgica tipo IIR", 14, "D10002", proveedores[1].Nombre, "ALM-CENTRAL", "QUIR", 600),
            Order("4500098103", "MAT-10003", "Bata quirúrgica reforzada estéril", 22, "D10002", proveedores[1].Nombre, "ALM-ESTERIL", "QUIR", 80),
            Order("4500098104", "MAT-10004", "Sábana desechable ajustable", 3, "D10003", proveedores[2].Nombre, "ALM-CENTRAL", "HOSP", 200),
            Order("4500098105", "MAT-10005", "Contenedor de residuos biosanitarios 10 L", 12, "D10001", proveedores[0].Nombre, "ALM-CENTRAL", "URG", 45),
            Order("4500098106", "MAT-10006", "Jeringa de seguridad 10 ml", 16, "D10004", proveedores[3].Nombre, "ALM-CENTRAL", "UCI", 350),
            Order("4500098107", "MAT-10007", "Empapador absorbente 60 × 90 cm", 7, "D10003", proveedores[2].Nombre, "ALM-PLANTA", "HOSP", 150),
            Order("4500098108", "MAT-10008", "Gorro quirúrgico desechable", 11, "D10005", proveedores[4].Nombre, "ALM-ESTERIL", "QUIR", 500),
            Order("4500098109", "MAT-10009", "Equipo de protección facial", 9, "D10001", proveedores[0].Nombre, "ALM-CENTRAL", "URG", 30),
            Order("4500098110", "MAT-10010", "Campo quirúrgico fenestrado estéril", 6, "D10002", proveedores[1].Nombre, "ALM-ESTERIL", "QUIR", 60),
            Order("4500098111", "MAT-10011", "Bolsa autocierre para muestras", 2, "D10004", proveedores[3].Nombre, "ALM-LAB", "LAB", 1000),
            Order("4500098112", "MAT-10012", "Papel secamanos industrial", 25, "D10003", proveedores[2].Nombre, "ALM-GENERAL", "SERV", 96),
        };

        // Contratos marco por línea, como vienen en el ME2N real (proveedores con acuerdo marco).
        foreach (var o in orders)
        {
            o.CantidadPedido = o.PorEntregarCantidad;
            if (o.ProveedorCodigo is "D10001" or "D10002" or "D10004")
                o.ContratoMarco = $"48000{Math.Abs(o.DocumentoCompras.GetHashCode()) % 90000 + 10000}";
        }

        orders[1].Reclamado = true;
        orders[1].ReclamadoAt = now.AddDays(-5);
        orders[1].ReclamadoCount = 1;
        orders[2].Reclamado = true;
        orders[2].ReclamadoAt = now.AddDays(-10);
        orders[2].ReclamadoCount = 2;
        orders[5].Reclamado = true;
        orders[5].ReclamadoAt = now.AddDays(-12);
        orders[5].ReclamadoCount = 2;
        orders[5].EnFalta = true;
        orders[5].EnFaltaAt = now.AddDays(-4);
        orders[6].Reclamado = true;
        orders[6].ReclamadoAt = now.AddDays(-1);
        orders[6].ReclamadoCount = 1;
        orders[8].Reclamado = true;
        orders[8].ReclamadoAt = now.AddDays(-7);
        orders[8].ReclamadoCount = 1;

        var recibidos = new[]
        {
            Order("4500098091", "MAT-10013", "Venda elástica cohesiva", 9, "D10001", proveedores[0].Nombre, "ALM-CENTRAL", "URG", 0),
            Order("4500098092", "MAT-10014", "Paño de limpieza hospitalaria", 24, "D10003", proveedores[2].Nombre, "ALM-GENERAL", "SERV", 0),
            Order("4500098093", "MAT-10015", "Apósito adhesivo estéril", 48, "D10004", proveedores[3].Nombre, "ALM-CENTRAL", "UCI", 0),
            Order("4500098094", "MAT-10016", "Calza antideslizante desechable", 75, "D10002", proveedores[1].Nombre, "ALM-ESTERIL", "QUIR", 0),
        };
        recibidos[0].Recibido = true; recibidos[0].RecibidoAt = now.AddHours(-2); recibidos[0].CantidadRecibida = 100;
        recibidos[1].Recibido = true; recibidos[1].RecibidoAt = now.AddDays(-9); recibidos[1].CantidadRecibida = 240;
        recibidos[2].Recibido = true; recibidos[2].RecibidoAt = now.AddDays(-31); recibidos[2].CantidadRecibida = 80;
        recibidos[3].Recibido = true; recibidos[3].RecibidoAt = now.AddDays(-62); recibidos[3].CantidadRecibida = 300;

        var anulado = Order("4500098090", "MAT-10017", "Carro auxiliar de transporte", 32, "D10004", proveedores[3].Nombre, "ALM-GENERAL", "SERV", 1);
        anulado.Anulado = true;
        anulado.AnuladoAt = now.AddDays(-7);
        anulado.Incidencia = "DEMO · Pedido duplicado en origen.";

        db.Orders.AddRange(orders);
        db.Orders.AddRange(recibidos);
        db.Orders.Add(anulado);

        // --- Histórico ampliado: recepciones pasadas con tiempos de entrega
        // variados por proveedor (para una auditoría rigurosa y con volumen). ---
        var histResponses = new List<ProviderResponse>();
        var histEmails = new List<EmailLog>();
        var rnd = new Random(20260716); // determinista: misma demo en cada arranque
        var descPool = new[]
        {
            "Compresa de gasa estéril", "Solución antiséptica 500 ml", "Electrodo de monitorización",
            "Sonda de aspiración", "Guante estéril quirúrgico", "Esparadrapo hipoalergénico",
            "Filtro antibacteriano", "Bolsa de diuresis", "Set de sueroterapia", "Torunda de algodón",
            "Mascarilla FFP2", "Bata TNT reforzada", "Aguja de seguridad 21G", "Catéter periférico 18G",
        };
        var centros = new[] { "UCI", "QUIR", "URG", "HOSP", "LAB", "SERV" };
        // Calidad simulada por proveedor (ratio de entregas dentro de plazo).
        var calidad = new Dictionary<string, double>
        {
            ["D10001"] = 0.90, ["D10002"] = 0.72, ["D10003"] = 0.55, ["D10004"] = 0.66, ["D10005"] = 0.34,
        };
        long seq = 4500090000;
        foreach (var pr in proveedores)
        {
            int plazo = pr.PlazoEntregaDias ?? 10;
            double onTimeRatio = calidad.TryGetValue(pr.Codigo, out var q) ? q : 0.6;

            // Recepciones históricas.
            int nRec = rnd.Next(9, 14);
            for (int i = 0; i < nRec; i++)
            {
                int daysAgoDoc = rnd.Next(35, 320);
                bool onTime = rnd.NextDouble() < onTimeRatio;
                int deliveryBiz = onTime ? rnd.Next(1, plazo + 1) : rnd.Next(plazo + 1, plazo + 9);
                var doc = (seq++).ToString();
                var mat = $"MAT-30{seq % 1000:000}";
                var o = Order(doc, mat, descPool[rnd.Next(descPool.Length)], daysAgoDoc,
                    pr.Codigo, pr.Nombre, "ALM-CENTRAL", centros[rnd.Next(centros.Length)], 0);
                var recDate = PedidosPendientes.Core.BusinessDays.AddBusinessDays(o.FechaDocumento, deliveryBiz);
                o.Recibido = true;
                o.RecibidoAt = new DateTimeOffset(recDate.ToDateTime(new TimeOnly(11, 0)), now.Offset);
                o.CantidadRecibida = rnd.Next(10, 500);

                if (rnd.NextDouble() < 0.4) // reclamado antes de recibirse
                {
                    var recl = PedidosPendientes.Core.BusinessDays.AddBusinessDays(o.FechaDocumento, Math.Max(1, plazo - 1));
                    o.Reclamado = true;
                    o.ReclamadoAt = new DateTimeOffset(recl.ToDateTime(new TimeOnly(10, 0)), now.Offset);
                    o.ReclamadoCount = rnd.NextDouble() < 0.3 ? 2 : 1;
                    histEmails.Add(new EmailLog { DocumentoCompras = doc, ProveedorEmail = pr.Email ?? "sin-email@demo.example", Estado = "enviado", Tipo = "automatico", SentAt = o.ReclamadoAt.Value });
                    if (rnd.NextDouble() < 0.6)
                        histResponses.Add(new ProviderResponse { ProveedorNombre = pr.Nombre, DocumentoCompras = doc, Material = mat, Estado = "registrado_entregado", Comentario = "DEMO · Entrega confirmada por el proveedor.", RevisionEstado = "aplicada", CreatedAt = o.ReclamadoAt.Value.AddDays(1) });
                }
                db.Orders.Add(o);
            }

            // Pendientes históricos con antigüedad variada (algunos fuera de plazo).
            int nPend = rnd.Next(2, 5);
            for (int i = 0; i < nPend; i++)
            {
                int daysAgoDoc = rnd.Next(4, 40);
                var doc = (seq++).ToString();
                var mat = $"MAT-30{seq % 1000:000}";
                var o = Order(doc, mat, descPool[rnd.Next(descPool.Length)], daysAgoDoc,
                    pr.Codigo, pr.Nombre, "ALM-CENTRAL", centros[rnd.Next(centros.Length)], rnd.Next(10, 400));
                if (daysAgoDoc > 15 && rnd.NextDouble() < 0.7)
                {
                    o.Reclamado = true;
                    o.ReclamadoAt = now.AddDays(-rnd.Next(2, 8));
                    o.ReclamadoCount = 1;
                }
                db.Orders.Add(o);
            }
        }
        db.ProviderResponses.AddRange(histResponses);
        db.EmailLogs.AddRange(histEmails);

        var clasificaciones =
            new[] { "MAT-10001", "MAT-10002", "MAT-10003", "MAT-10006", "MAT-10009", "MAT-10013" }.Select(m => new MaterialClasificacion { Material = m, Clase = "A" })
            .Concat(new[] { "MAT-10004", "MAT-10005", "MAT-10007", "MAT-10010", "MAT-10014", "MAT-10015" }.Select(m => new MaterialClasificacion { Material = m, Clase = "B" }))
            .Concat(new[] { "MAT-10008", "MAT-10011", "MAT-10012", "MAT-10016", "MAT-10017" }.Select(m => new MaterialClasificacion { Material = m, Clase = "C" }));
        db.MaterialClasificaciones.AddRange(clasificaciones);

        db.CriticidadUbicaciones.AddRange(
            new CriticidadUbicacion { Tipo = "centroCoste", Codigo = "UCI", Descripcion = "UCI", Nivel = "critico" },
            new CriticidadUbicacion { Tipo = "centroCoste", Codigo = "QUIR", Descripcion = "Bloque quirúrgico", Nivel = "critico" },
            new CriticidadUbicacion { Tipo = "centroCoste", Codigo = "URG", Descripcion = "Urgencias", Nivel = "alto" },
            new CriticidadUbicacion { Tipo = "almacen", Codigo = "ALM-ESTERIL", Descripcion = "Material estéril", Nivel = "alto" });

        // MD04 con el formato real de NEXUS: Status ("XOO" rojo / "OOX" verde),
        // Área pl.nec. y excepciones ME3 (pedido fuera de plazo) / ME6 (bajo stock de seguridad).
        var stock = new (string Material, string Texto, string Estado, int? Me3, int? Me6)[]
        {
            ("MAT-10001", "Guantes de nitrilo sin polvo · talla M", "XOO", 1, 1),
            ("MAT-10002", "Mascarilla quirúrgica tipo IIR", "XOO", null, 1),
            ("MAT-10018", "Tubular de malla elástica", "XOO", 1, null),
            ("MAT-10019", "Funda estéril para equipos", "XOO", 2, 1),
            ("MAT-10020", "Kit de curas universal", "XOO", null, 1),
            ("MAT-10005", "Contenedor biosanitario 10 L", "OXO", 1, null),
            ("MAT-10007", "Empapador absorbente", "OXO", null, null),
            ("MAT-10010", "Campo quirúrgico fenestrado", "OXO", 1, null),
            ("MAT-10011", "Bolsa autocierre para muestras", "OOX", null, null),
            ("MAT-10013", "Venda elástica cohesiva", "OOX", null, null),
        };
        db.StockMd04.AddRange(stock.Select(s => new StockMd04Item
        {
            Ambito = "almacenaje",
            Material = s.Material,
            TextoBreve = s.Texto,
            Semaforo = s.Estado[0] is 'X' ? "rojo" : s.Estado[1] is 'X' ? "amarillo" : "verde",
            StatusNexus = s.Estado,
            AreaPlanificacion = "L025-1000",
            PedidosFueraPlazo = s.Me3,
            BajoStockSeguridad = s.Me6,
            CargadoAt = now.AddMinutes(-24),
        }));

        var stockNa = new (string Material, string Texto, string Estado, int? Me3, int? Me6)[]
        {
            ("MAT-20001", "Prótesis de cadera no cementada", "XOO", 1, null),
            ("MAT-20002", "Lente intraocular plegable +21D", "XOO", null, 1),
            ("MAT-20003", "Malla quirúrgica de polipropileno", "OOX", null, null),
            ("MAT-20004", "Grapadora circular 29 mm", "OOX", null, null),
        };
        db.StockMd04.AddRange(stockNa.Select(s => new StockMd04Item
        {
            Ambito = "noalmacenaje",
            Material = s.Material,
            TextoBreve = s.Texto,
            Semaforo = s.Estado[0] is 'X' ? "rojo" : "verde",
            StatusNexus = s.Estado,
            AreaPlanificacion = "L025-2000",
            PedidosFueraPlazo = s.Me3,
            BajoStockSeguridad = s.Me6,
            CargadoAt = now.AddMinutes(-22),
        }));

        // Relación CECOs - Almacenes (con las ubicaciones que usa el escenario).
        var cecos = new (string Almacen, string DenAlm, string Cc, string DenCc, string Desc)[]
        {
            ("ALM-CENTRAL", "Almacén general", "SERV", "SERVICIOS GENERALES", "ALMACÉN GENERAL DE SUMINISTROS"),
            ("ALM-ESTERIL", "Almacén estéril", "QUIR", "BLOQUE QUIRÚRGICO", "BLOQUE QUIRÚRGICO Y ESTERILIZACIÓN"),
            ("ALM-PLANTA", "Almacén de planta 2A", "HOSP", "U DE HOSPITALIZ 2A", "UNIDAD DE HOSPITALIZACIÓN 2A"),
            ("ALM-LAB", "Almacén laboratorio", "LAB", "LABORATORIO CLÍNICO", "LABORATORIO DE ANÁLISIS CLÍNICOS"),
            ("ALM-UCI", "Almacén UCI", "UCI", "MEDICINA INTENSIVA", "UNIDAD DE CUIDADOS INTENSIVOS"),
            ("ALM-URG", "Almacén urgencias", "URG", "URGENCIAS", "URGENCIAS GENERALES"),
        };
        db.AlmacenCecos.AddRange(cecos.Select(c => new AlmacenCeco
        {
            Centro = "L025",
            Almacen = c.Almacen,
            DenominacionAlmacen = c.DenAlm,
            CentroCoste = c.Cc,
            DenominacionCentroCoste = c.DenCc,
            Descripcion = c.Desc,
            CargadoAt = now.AddMinutes(-20),
        }));

        db.ProviderResponses.AddRange(
            new ProviderResponse { ProveedorNombre = proveedores[0].Nombre, DocumentoCompras = "4500098109", Material = "MAT-10009", Estado = "fecha_estimada", FechaEstimada = today.AddDays(3), Comentario = "DEMO · Entrega prevista en la próxima ruta.", RevisionEstado = "pendiente", CreatedAt = now.AddDays(-2) },
            new ProviderResponse { ProveedorNombre = proveedores[3].Nombre, DocumentoCompras = "4500098106", Material = "MAT-10006", Estado = "en_falta", Comentario = "DEMO · Rotura temporal de stock del fabricante.", RevisionEstado = "revisada", CreatedAt = now.AddDays(-4) },
            new ProviderResponse { ProveedorNombre = proveedores[1].Nombre, DocumentoCompras = "4500098094", Material = "MAT-10016", Estado = "no_localizado", Comentario = "DEMO · Incidencia histórica resuelta.", RevisionEstado = "aplicada", CreatedAt = now.AddMonths(-2) });

        db.UploadLogs.AddRange(
            new UploadLog { Tipo = "pedidos", FileName = "DEMO_ME2N_pendientes_entrega.xlsx", Success = true, Filas = 17, Insertados = 17, Message = "Escenario de demostración cargado correctamente.", At = now.AddMinutes(-38) },
            new UploadLog { Tipo = "md04", FileName = "DEMO_MD04_almacenaje.xlsx", Success = true, Filas = stock.Length, Insertados = stock.Length, Message = "Instantánea MD04 de almacenaje cargada.", At = now.AddMinutes(-24) },
            new UploadLog { Tipo = "md04na", FileName = "DEMO_MD04_no_almacenaje.xlsx", Success = true, Filas = stockNa.Length, Insertados = stockNa.Length, Message = "Instantánea MD04 de no almacenaje cargada.", At = now.AddMinutes(-22) },
            new UploadLog { Tipo = "cecos", FileName = "DEMO_relacion_cecos_almacenes.xlsx", Success = true, Filas = cecos.Length, Insertados = cecos.Length, Message = "Relación CECOs - Almacenes de demostración cargada.", At = now.AddMinutes(-20) },
            new UploadLog { Tipo = "abc", FileName = "DEMO_clasificacion_ABC.xlsx", Success = true, Filas = 17, Insertados = 17, Message = "Clasificación de demostración actualizada.", At = now.AddMinutes(-16) });

        db.EmailLogs.AddRange(
            new EmailLog { DocumentoCompras = "4500098102", ProveedorEmail = proveedores[1].Email!, Estado = "enviado", Tipo = "manual", SentAt = now.AddDays(-5) },
            new EmailLog { DocumentoCompras = "4500098103", ProveedorEmail = proveedores[1].Email!, Estado = "enviado", Tipo = "automatico", SentAt = now.AddDays(-10) });

        await db.SaveChangesAsync(ct);
    }
}
