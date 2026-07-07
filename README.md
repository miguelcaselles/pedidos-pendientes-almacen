# Pedidos Pendientes · Almacén (Suministros)

Aplicación interna de reclamación de pedidos pendientes del **Servicio de Suministros
(Almacén)** del Hospital Universitario de Fuenlabrada (SERMAS), construida sobre
**ASP.NET Core MVC + Entity Framework Core + SQL Server** para desplegarse en la
infraestructura corporativa del hospital.

Es el rediseño de la app original de Farmacia (Next.js + Neon + Vercel), adaptado al
dominio de almacén y a tecnología .NET / SQL Server. Ver el documento de diseño en
`../DISEÑO-MIGRACION-NET-SQLSERVER.md`.

## Estructura de la solución

```
src/
├── PedidosPendientes.Web/             ASP.NET Core MVC (UI + controllers)
├── PedidosPendientes.Core/            Dominio (entidades, DTOs, interfaces, reglas)
└── PedidosPendientes.Infrastructure/  Datos (EF Core), parser de Excel, email
```

## Requisitos

- .NET SDK 9
- SQL Server (en producción). En desarrollo se puede ejecutar sin SQL Server:
  si no hay cadena de conexión, usa una base de datos **en memoria**.

## Ejecutar en desarrollo (sin SQL Server)

```bash
cd src/PedidosPendientes.Web
dotnet run
```

Abrir la URL que indique la consola. Al no haber `ConnectionStrings:Default`,
la app crea una base en memoria (los datos se pierden al reiniciar).

## Ejecutar contra SQL Server

1. Configurar la cadena de conexión en `appsettings.json`
   (`ConnectionStrings:Default`) o como variable de entorno
   `ConnectionStrings__Default`.
2. Aplicar las migraciones (la app las aplica sola al arrancar; o manualmente):

   ```bash
   dotnet ef database update \
     --project src/PedidosPendientes.Infrastructure \
     --startup-project src/PedidosPendientes.Web
   ```

## Estado del desarrollo

Implementado:

- Dominio adaptado a almacén (clave única **Documento + Material**; campos nuevos:
  Expediente administrativo, Tipo de imputación, Almacén).
- Base de datos SQL Server con EF Core (migraciones `InitialCreate` y `MejorasOperativas`).
- **Panel de control**: alertas de reclamación, riesgo de retraso (% pendientes fuera
  de plazo + probabilidad histórica), incidencias por mes, estado de subida de los
  tres ficheros y resumen del semáforo MD04.
- **Cargas de Excel** (con registro y estado de subida visible):
  - Pedidos pendientes de SAP — **no destructiva** (conserva el estado de gestión),
    detección dinámica de columnas y deduplicación.
  - **Semáforo MD04 de NEXUS** — instantánea del día; cruza materiales en rojo con
    los pedidos para destacar los que **no tienen pedido lanzado**.
  - **Clasificación A/B/C por código NEXUS** — parametriza la urgencia de reclamación.
- **Pedidos**: pestañas (pendientes / para reclamar / alertas / reclamados / en falta /
  recibidos / anulados), acciones por línea (reclamar, recibir, en falta, anular,
  reactivar), badges de clase A/B/C, criticidad de ubicación y acuerdo marco,
  ordenación por prioridad (alerta > criticidad > riesgo).
- **Alertas de seguimiento**: reclamado sin respuesta del proveedor a los N días
  (configurable) y, pasados N días más, "para llamar por teléfono".
- **Proveedores**: contacto, **acuerdo marco** con plazo contractual, pendientes fuera
  de plazo (base para penalizaciones) y notas de penalización.
- **Configuración**: plazos por clase A/B/C, plazos de respuesta/llamada, plazo de
  acuerdo marco, aviso de carga obsoleta y **ubicaciones críticas** (almacén /
  centro de coste, p. ej. UCI) que priorizan sus pedidos.
- Estética institucional (logo HUF + paleta corporativa, CSS plano, sin dependencias JS).
- Cabeceras de seguridad.

Pendiente (siguientes fases):

- Reclamación por email (MailKit) + formulario de respuesta del proveedor firmado
  (las alertas de "sin respuesta" ya leen la tabla `ProviderResponses`).
- Importación del catálogo de proveedores (L025) con emails.
- Consumos por material/almacén (a falta de definir la fuente de datos; el MD04 ya
  admite una columna de consumo medio que se muestra en el semáforo).
- Exportación a Excel de los listados.
- Autenticación Active Directory / SSO corporativo.
- Tareas programadas (BackgroundService).
