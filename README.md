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
- Base de datos SQL Server con EF Core (migración inicial).
- Importación de Excel **no destructiva** (conserva el estado de gestión) con
  detección dinámica de columnas y deduplicación de filas idénticas.
- Listado de pedidos pendientes con indicadores y búsqueda.
- Estética institucional (logo HUF + paleta corporativa, CSS plano).
- Cabeceras de seguridad.

Pendiente (siguientes fases):

- Acciones de estado (recibir, anular, en falta, comentarios) y pestañas
  Recibidos / Anulados / Historial.
- Gestión de proveedores e importación del catálogo (L025).
- Reclamación por email (MailKit) + formulario de respuesta del proveedor firmado.
- Indicadores / estadísticas y exportación.
- Autenticación Active Directory / SSO corporativo.
- Tareas programadas (BackgroundService).
