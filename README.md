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

### Escenario de demostración

Cuando `Demo:Enabled` es `true` y la aplicación usa la base en memoria, se crea
automáticamente un escenario ficticio completo: pedidos en todos los estados,
proveedores, acuerdos marco, clasificación A/B/C, criticidades y semáforo MD04.
La interfaz lo identifica permanentemente como **DEMO**.

El seeder está bloqueado para bases relacionales: aunque la opción se dejase
activada por error, nunca inserta datos ficticios en SQL Server. Para desactivar
la demo local, usar `Demo__Enabled=false`.

## Demo desplegable en Render

El repositorio incluye `Dockerfile`, `.dockerignore` y `render.yaml`. El Blueprint
levanta una única instancia gratuita en Frankfurt con datos exclusivamente en
memoria. Los cambios hechos durante una presentación son temporales y el escenario
se restaura al reiniciar el servicio.

Para el despliegue corporativo definitivo no debe usarse este Blueprint: se
publica en IIS, se configura `ConnectionStrings__Default` y se establece
`Demo__Enabled=false`.

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

## Despliegue corporativo

Guía completa para el equipo de desarrollo del hospital (IIS, SQL Server,
correo institucional, Active Directory, formatos de los ficheros y checklist
de puesta en marcha en pruebas): **[DESPLIEGUE-HOSPITAL.md](DESPLIEGUE-HOSPITAL.md)**.

## Estado del desarrollo

Implementado:

- Dominio adaptado a almacén (clave única **Documento + Material**).
- Base de datos SQL Server con EF Core (migraciones `InitialCreate`,
  `MejorasOperativas` y `ListadosRealesYCatalogo`).
- **Panel de control**: alertas de reclamación, riesgo de retraso (% pendientes fuera
  de plazo + probabilidad histórica), incidencias por mes, estado de subida de las
  cinco fuentes y resumen del semáforo MD04.
- **Cargas de Excel** validadas con los ficheros reales de Suministros:
  - **ME2N pendientes de entrega** — no destructiva; incluye cantidad de pedido y
    **contrato marco por línea** (marca el acuerdo marco del proveedor).
  - **MD04 de NEXUS en dos ámbitos** (almacenaje / no almacenaje) — formato real:
    Status `XOO/OXO/OOX`, Área pl.nec. y excepciones **ME3** (pedido fuera de
    plazo) y **ME6** (bajo stock de seguridad). El color sale del Status, no del stock.
  - **Clasificación A/B/C por código NEXUS** — parametriza la urgencia de reclamación.
  - **Relación CECOs - Almacenes** — denominaciones legibles y base para marcar
    criticidad (almacén + centro de coste con un clic).
- **Semáforo MD04**: pestañas por ámbito, filtros por rojos sin pedido lanzado,
  ME3, ME6, y cruce con los pedidos pendientes.
- **Pedidos**: pestañas por estado, acciones por línea, clase A/B/C, criticidad,
  acuerdo marco, ordenación por prioridad (alerta > criticidad > riesgo).
- **Reclamación por correo (SMTP institucional, MailKit)**: al reclamar se envía
  el correo al proveedor (agrupa líneas del documento, cita el contrato marco),
  con registro en `EmailLogs`, modo pruebas y estado visible en Configuración.
- **Alertas de seguimiento**: reclamado sin respuesta a los N días (configurable)
  y, pasados N días más, "para llamar por teléfono".
- **Proveedores**: contacto, acuerdo marco, auditoría global y detallada con
  export a Excel, base para penalizaciones.
- **Estadísticas**: periodicidad de pedido por artículo (intervalo medio, próximo
  pedido estimado, aviso "toca pedir").
- **Catálogo de producto**: buscador por código/descripción y ficha por material
  (denominación, alternativos, presupuesto, ficha técnica adjunta).
- **Autenticación institucional opcional** (`Auth:Mode=Windows`, Negotiate/AD con
  restricción por grupos).
- Estética institucional, cabeceras de seguridad, demo efímera para presentaciones.

Pendiente (siguientes fases):

- Tareas programadas (BackgroundService) para la reclamación automática diaria.
- Formulario de respuesta del proveedor con enlace firmado (las alertas de
  "sin respuesta" ya leen la tabla `ProviderResponses`).
- Importación del catálogo de proveedores (L025) con emails.
- Consumos por material/almacén (a falta de definir la fuente de datos).
- Exportación a Excel de pedidos y semáforo.
- RFID / OFT con cruce a Selene (fase posterior).
