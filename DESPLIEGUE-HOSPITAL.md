# Guía de despliegue · Equipo de desarrollo del hospital

Documento de traspaso para desplegar **Pedidos Pendientes · Almacén (Suministros)**
en la infraestructura corporativa del Hospital Universitario de Fuenlabrada
(entornos de pruebas y producción), configurar el correo institucional y la
autenticación de Active Directory.

## 1. Arquitectura

ASP.NET Core MVC (**.NET 9**) + Entity Framework Core + **SQL Server**. Sin
dependencias JavaScript externas; CSS plano. Tres proyectos:

```
src/PedidosPendientes.Web/             UI + controllers (MVC)
src/PedidosPendientes.Core/            Dominio (entidades, reglas, interfaces)
src/PedidosPendientes.Infrastructure/  EF Core, parsers de Excel, correo (MailKit)
```

Toda la configuración sensible entra por **variables de entorno** (formato
`Seccion__Clave`); `appsettings.json` solo lleva valores por defecto inocuos.

## 2. Requisitos

- Windows Server con IIS y el **.NET 9 Hosting Bundle** (o Kestrel detrás del
  proxy corporativo).
- **SQL Server** (2017+). Base de datos dedicada, p. ej. `PedidosPendientesAlmacen`.
- Cuenta de servicio del *application pool* con permisos `db_owner` durante el
  primer arranque (aplica migraciones) — después basta `db_datareader/db_datawriter`.
- Buzón institucional para reclamaciones y acceso al relay SMTP corporativo.

## 3. Base de datos

La app aplica las migraciones automáticamente al arrancar (`Database.Migrate()`).
Para aplicarlas manualmente:

```bash
dotnet ef database update \
  --project src/PedidosPendientes.Infrastructure \
  --startup-project src/PedidosPendientes.Infrastructure
```

(la cadena de conexión de diseño se toma de la variable `PEDIDOS_DB`).

Migraciones actuales: `InitialCreate`, `MejorasOperativas`, `ListadosRealesYCatalogo`.

Tablas principales: `Orders` (líneas ME2N + estado de gestión), `Proveedores`,
`StockMd04` (instantánea MD04 por ámbito), `AlmacenCecos` (relación CECOs-almacenes),
`MaterialClasificaciones` (A/B/C), `CriticidadUbicaciones`, `ProductoFichas` +
`ProductoAdjuntos` (catálogo, adjuntos en `varbinary(max)`), `EmailLogs`,
`ProviderResponses`, `UploadLogs`, `AppSettings`.

## 4. Variables de entorno (producción)

| Variable | Valor |
|---|---|
| `ConnectionStrings__Default` | `Server=SERVIDOR-SQL;Database=PedidosPendientesAlmacen;Trusted_Connection=True;TrustServerCertificate=True;` |
| `Demo__Enabled` | `false` (**obligatorio** — desactiva el escenario ficticio y la verja de demo) |
| `Auth__Mode` | `Windows` |
| `Auth__AllowedGroups__0` | `SERMAS\GG_HUF_Suministros` (grupo AD con acceso; añadir `__1`, `__2`… para más) |
| `Smtp__Enabled` | `true` |
| `Smtp__Host` | relay SMTP corporativo |
| `Smtp__Port` | `587` (`Smtp__UseStartTls=true`) o `25` interno (`false`) |
| `Smtp__User` / `Smtp__Password` | credenciales del buzón (vacío si el relay no autentica) |
| `Smtp__FromAddress` | buzón institucional de Suministros |
| `Smtp__ReplyTo` / `Smtp__Bcc` | opcionales (respuesta / copia de control) |
| `Smtp__ModoPruebas` | `true` en pruebas (desvía TODO a `Smtp__DestinatarioPruebas`), `false` en producción |

Nota: el seeder de demo está bloqueado para bases relacionales — aunque
`Demo__Enabled` quedara en `true` por error, jamás inserta datos ficticios en
SQL Server. Aun así, en corporativo debe ponerse a `false` (también desactiva
la verja de contraseña de la demo).

## 5. IIS

1. `dotnet publish src/PedidosPendientes.Web -c Release -o <carpeta>`.
2. Sitio/aplicación IIS apuntando a la carpeta; *app pool* «No Managed Code».
3. **Autenticación**: habilitar *Windows Authentication*, deshabilitar *Anonymous*
   (con `Auth__Mode=Windows` la app exige usuario autenticado y valida los grupos).
4. Variables de entorno del apartado 4 en el *app pool* (o `environmentVariables`
   de `web.config`).
5. HTTPS con el certificado corporativo. La app ya emite cabeceras de seguridad
   y `HSTS` fuera de desarrollo.

## 6. Correo de reclamaciones

- Al pulsar **Reclamar** en Pedidos: se marca la línea, y si `Smtp` está
  configurado y el proveedor tiene email, se envía un correo con **todas las
  líneas pendientes del documento** (asunto distinto en reenvíos, mención del
  contrato marco si lo hay). Todo intento queda en `EmailLogs` (enviado/error).
- Sin SMTP configurado la app funciona igual (solo marca la línea).
- **En los entornos de pruebas del hospital**: `Smtp__ModoPruebas=true` con un
  buzón de pruebas. Los asuntos van prefijados con `[PRUEBAS]`.
- El estado (activado / no configurado / modo pruebas / envíos de 30 días) se ve
  en **Configuración**.

## 7. Ficheros de entrada (cabeceras reales)

Los parsers detectan columnas por cabecera (tolerantes a acentos/mayúsculas) con
respaldo posicional. Formatos validados con los ficheros reales de Suministros:

- **ME2N Pendientes de entrega** (pestaña Cargas → Pedidos): `Fecha documento`,
  `Documento compras`, `Historial pedido/Docu.orden`, `Proveedor/Centro suministrad`
  (código + nombre juntos), `Material`, `Referencia del proveedor.`, `Texto breve`,
  `Cantidad de pedido`, `Por entregar (cantidad)`, `Contrato marco`. Carga **no
  destructiva** (clave Documento+Material). El contrato marco marca el acuerdo
  marco del proveedor automáticamente.
- **MD04 de NEXUS** (dos cargas: almacenaje y no almacenaje): `Status`
  (`XOO`=rojo, `OXO`=amarillo, `OOX`=verde), `Material`, `Área pl.nec.`,
  `Denominación`, `ME3` (nº de pedidos fuera de plazo de entrega), `ME6`
  (por debajo de stock de seguridad). Cada carga sustituye la instantánea
  anterior **de su ámbito**. El color sale del Status, nunca del stock
  (descuadres posibles).
- **Relación CECOs - Almacenes**: `Centro`, `Almacén`, `Denominación-almacén`,
  `Datos 1..4` (ignoradas), `Centro de coste`, `Denominación`, `Descripción`.
  Instantánea completa.
- **Clasificación A/B/C**: dos columnas (material NEXUS y clase). Acumulativa
  (upsert). Pendiente de recibir el listado real de Suministros.

## 8. Autenticación

`Auth__Mode=Windows` activa Negotiate (Kerberos/NTLM). La política global exige
usuario autenticado y, si `Auth__AllowedGroups` tiene valores, pertenencia a esos
grupos AD. El nombre del usuario aparece en la cabecera de la app. Con
`Auth__Mode=None` (por defecto) no hay autenticación: solo desarrollo/demo.

Si se prefiere SSO federado (ADFS/Entra), sustituir el bloque de autenticación de
`Program.cs` por OpenID Connect; el resto de la app no cambia (usa `User.Identity`).

## 9. Puesta en marcha en pruebas (checklist)

1. Base de datos creada + `ConnectionStrings__Default`.
2. `Demo__Enabled=false`, `Auth__Mode=Windows` (o `None` en la primera prueba).
3. `Smtp__ModoPruebas=true` + buzón de pruebas.
4. Arrancar y verificar `/(Panel)`: estado de las 5 cargas en "Nunca".
5. Cargar en orden: ME2N → MD04 almacenaje → MD04 no almacenaje → A/B/C →
   relación CECOs (pestaña **Cargas**; el historial registra cada intento).
6. En **Configuración**: marcar ubicaciones críticas desde la relación CECOs
   (UCI, quirófano…) y ajustar plazos A/B/C.
7. Completar emails de proveedores (pestaña **Proveedores**) y probar una
   reclamación (el correo debe llegar al buzón de pruebas).

## 10. Pendiente / hoja de ruta

- **Tareas programadas** (`BackgroundService`): reclamación automática diaria de
  las líneas en alerta (el envío ya es un servicio inyectable; falta el scheduler
  y decidir la franja horaria con Suministros).
- **Formulario de respuesta del proveedor** con enlace firmado (las alertas de
  "sin respuesta" ya leen `ProviderResponses`).
- Importación del catálogo de proveedores (L025) con emails.
- Consumos por material/almacén (falta definir la fuente de datos).
- Exportación a Excel de pedidos y semáforo (la auditoría de proveedores ya exporta).
- RFID / OFT (lectura de código, cruce código NEXUS y conexión con Selene):
  fase posterior, fuera del alcance actual.
