using Microsoft.EntityFrameworkCore;
using PedidosPendientes.Core.Entities;

namespace PedidosPendientes.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<ProviderResponse> ProviderResponses => Set<ProviderResponse>();
    public DbSet<UploadLog> UploadLogs => Set<UploadLog>();
    public DbSet<StockMd04Item> StockMd04 => Set<StockMd04Item>();
    public DbSet<MaterialClasificacion> MaterialClasificaciones => Set<MaterialClasificacion>();
    public DbSet<CriticidadUbicacion> CriticidadUbicaciones => Set<CriticidadUbicacion>();
    public DbSet<AlmacenCeco> AlmacenCecos => Set<AlmacenCeco>();
    public DbSet<ProductoFicha> ProductoFichas => Set<ProductoFicha>();
    public DbSet<ProductoAdjunto> ProductoAdjuntos => Set<ProductoAdjunto>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Order>(e =>
        {
            e.ToTable("Orders");
            e.HasKey(x => x.Id);

            e.Property(x => x.DocumentoCompras).HasMaxLength(20).IsRequired();
            e.Property(x => x.ExpedienteAdministrativo).HasMaxLength(40);
            e.Property(x => x.CentroCoste).HasMaxLength(20);
            e.Property(x => x.Material).HasMaxLength(30).IsRequired();
            e.Property(x => x.TipoImputacion).HasMaxLength(20);
            e.Property(x => x.TextoBreve).HasMaxLength(255);
            e.Property(x => x.NumMaterialProveedor).HasMaxLength(30);
            e.Property(x => x.ProveedorCodigo).HasMaxLength(20);
            e.Property(x => x.ProveedorNombre).HasMaxLength(200);
            e.Property(x => x.ReferenciaProveedor).HasMaxLength(40);
            e.Property(x => x.Almacen).HasMaxLength(20);
            e.Property(x => x.CantidadPedido).HasColumnType("decimal(12,2)");
            e.Property(x => x.PorEntregarCantidad).HasColumnType("decimal(12,2)");
            e.Property(x => x.ContratoMarco).HasMaxLength(20);
            e.Property(x => x.CantidadRecibida).HasColumnType("decimal(12,2)");

            // Clave única de negocio: el almacén no tiene "Posición".
            e.HasIndex(x => new { x.DocumentoCompras, x.Material })
             .IsUnique()
             .HasDatabaseName("UQ_Orders_Documento_Material");

            e.HasIndex(x => new { x.Recibido, x.TieneHistorialEntrega, x.Anulado })
             .HasDatabaseName("IX_Orders_Estado");
            e.HasIndex(x => x.FechaDocumento).HasDatabaseName("IX_Orders_FechaDocumento");
            e.HasIndex(x => x.ProveedorCodigo).HasDatabaseName("IX_Orders_ProveedorCodigo");
            e.HasIndex(x => x.ProveedorNombre).HasDatabaseName("IX_Orders_ProveedorNombre");
            e.HasIndex(x => x.Anulado).HasDatabaseName("IX_Orders_Anulado");
        });

        b.Entity<Proveedor>(e =>
        {
            e.ToTable("Proveedores");
            e.HasKey(x => x.Id);
            e.Property(x => x.Codigo).HasMaxLength(20).IsRequired();
            e.Property(x => x.Nombre).HasMaxLength(200).IsRequired();
            e.Property(x => x.Email).HasMaxLength(255);
            e.Property(x => x.Telefono).HasMaxLength(50);
            e.Property(x => x.Fax).HasMaxLength(50);
            e.Property(x => x.NifCif).HasMaxLength(30);
            e.Property(x => x.CodigoPostal).HasMaxLength(10);
            e.Property(x => x.Poblacion).HasMaxLength(100);
            e.Property(x => x.Centro).HasMaxLength(10);
            e.HasIndex(x => x.Codigo).IsUnique().HasDatabaseName("UQ_Proveedores_Codigo");
            e.HasIndex(x => x.Nombre).HasDatabaseName("IX_Proveedores_Nombre");
        });

        b.Entity<EmailLog>(e =>
        {
            e.ToTable("EmailLogs");
            e.HasKey(x => x.Id);
            e.Property(x => x.DocumentoCompras).HasMaxLength(20).IsRequired();
            e.Property(x => x.ProveedorEmail).HasMaxLength(255).IsRequired();
            e.Property(x => x.Estado).HasMaxLength(20).IsRequired();
            e.Property(x => x.Tipo).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.DocumentoCompras).HasDatabaseName("IX_EmailLogs_Documento");
            e.HasIndex(x => x.SentAt).HasDatabaseName("IX_EmailLogs_SentAt");
        });

        b.Entity<AppSetting>(e =>
        {
            e.ToTable("AppSettings");
            e.HasKey(x => x.Key);
            e.Property(x => x.Key).HasMaxLength(50);
        });

        b.Entity<ProviderResponse>(e =>
        {
            e.ToTable("ProviderResponses");
            e.HasKey(x => x.Id);
            e.Property(x => x.DocumentoCompras).HasMaxLength(20).IsRequired();
            e.Property(x => x.Material).HasMaxLength(30);
            e.Property(x => x.Estado).HasMaxLength(40).IsRequired();
            e.Property(x => x.RevisionEstado).HasMaxLength(20).IsRequired();
            e.Property(x => x.Ip).HasMaxLength(64);
            e.HasIndex(x => x.DocumentoCompras).HasDatabaseName("IX_ProviderResponses_Documento");
            e.HasIndex(x => x.CreatedAt).HasDatabaseName("IX_ProviderResponses_CreatedAt");
            e.HasIndex(x => x.RevisionEstado).HasDatabaseName("IX_ProviderResponses_Revision");
        });

        b.Entity<UploadLog>(e =>
        {
            e.ToTable("UploadLogs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Tipo).HasMaxLength(20).IsRequired();
            e.Property(x => x.FileName).HasMaxLength(255).IsRequired();
            e.Property(x => x.Message).HasMaxLength(500);
            e.HasIndex(x => new { x.Tipo, x.At }).HasDatabaseName("IX_UploadLogs_Tipo_At");
        });

        b.Entity<StockMd04Item>(e =>
        {
            e.ToTable("StockMd04");
            e.HasKey(x => x.Id);
            e.Property(x => x.Ambito).HasMaxLength(15).IsRequired().HasDefaultValue("almacenaje");
            e.Property(x => x.Material).HasMaxLength(30).IsRequired();
            e.Property(x => x.TextoBreve).HasMaxLength(255);
            e.Property(x => x.Almacen).HasMaxLength(20);
            e.Property(x => x.Semaforo).HasMaxLength(10).IsRequired();
            e.Property(x => x.StatusNexus).HasMaxLength(10);
            e.Property(x => x.AreaPlanificacion).HasMaxLength(20);
            e.Property(x => x.Stock).HasColumnType("decimal(12,2)");
            e.Property(x => x.PuntoPedido).HasColumnType("decimal(12,2)");
            e.Property(x => x.ConsumoMedio).HasColumnType("decimal(12,2)");
            e.HasIndex(x => x.Material).HasDatabaseName("IX_StockMd04_Material");
            e.HasIndex(x => new { x.Ambito, x.Semaforo }).HasDatabaseName("IX_StockMd04_Ambito_Semaforo");
        });

        b.Entity<MaterialClasificacion>(e =>
        {
            e.ToTable("MaterialClasificaciones");
            e.HasKey(x => x.Id);
            e.Property(x => x.Material).HasMaxLength(30).IsRequired();
            e.Property(x => x.Clase).HasMaxLength(1).IsRequired();
            e.HasIndex(x => x.Material).IsUnique().HasDatabaseName("UQ_MaterialClasificaciones_Material");
        });

        b.Entity<CriticidadUbicacion>(e =>
        {
            e.ToTable("CriticidadUbicaciones");
            e.HasKey(x => x.Id);
            e.Property(x => x.Tipo).HasMaxLength(20).IsRequired();
            e.Property(x => x.Codigo).HasMaxLength(20).IsRequired();
            e.Property(x => x.Descripcion).HasMaxLength(200);
            e.Property(x => x.Nivel).HasMaxLength(10).IsRequired();
            e.HasIndex(x => new { x.Tipo, x.Codigo }).IsUnique().HasDatabaseName("UQ_CriticidadUbicaciones_Tipo_Codigo");
        });

        b.Entity<AlmacenCeco>(e =>
        {
            e.ToTable("AlmacenCecos");
            e.HasKey(x => x.Id);
            e.Property(x => x.Centro).HasMaxLength(10).IsRequired();
            e.Property(x => x.Almacen).HasMaxLength(20).IsRequired();
            e.Property(x => x.DenominacionAlmacen).HasMaxLength(100);
            e.Property(x => x.CentroCoste).HasMaxLength(20);
            e.Property(x => x.DenominacionCentroCoste).HasMaxLength(100);
            e.Property(x => x.Descripcion).HasMaxLength(200);
            e.HasIndex(x => new { x.Centro, x.Almacen }).IsUnique().HasDatabaseName("UQ_AlmacenCecos_Centro_Almacen");
            e.HasIndex(x => x.CentroCoste).HasDatabaseName("IX_AlmacenCecos_CentroCoste");
        });

        b.Entity<ProductoFicha>(e =>
        {
            e.ToTable("ProductoFichas");
            e.HasKey(x => x.Id);
            e.Property(x => x.Material).HasMaxLength(30).IsRequired();
            e.Property(x => x.Denominacion).HasMaxLength(255);
            e.Property(x => x.DescripcionAmpliada).HasMaxLength(2000);
            e.Property(x => x.MaterialesAlternativos).HasMaxLength(1000);
            e.Property(x => x.PresupuestoUnitario).HasColumnType("decimal(12,4)");
            e.Property(x => x.Notas).HasMaxLength(2000);
            e.HasIndex(x => x.Material).IsUnique().HasDatabaseName("UQ_ProductoFichas_Material");
        });

        b.Entity<ProductoAdjunto>(e =>
        {
            e.ToTable("ProductoAdjuntos");
            e.HasKey(x => x.Id);
            e.Property(x => x.FileName).HasMaxLength(255).IsRequired();
            e.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
            e.HasOne(x => x.ProductoFicha)
             .WithMany(f => f.Adjuntos)
             .HasForeignKey(x => x.ProductoFichaId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.ProductoFichaId).HasDatabaseName("IX_ProductoAdjuntos_Ficha");
        });
    }

    public override int SaveChanges()
    {
        StampTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void StampTimestamps()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries())
        {
            switch (entry.Entity)
            {
                case Order o:
                    if (entry.State == EntityState.Added) o.CreatedAt = now;
                    o.UpdatedAt = now;
                    break;
                case Proveedor p:
                    if (entry.State == EntityState.Added) p.CreatedAt = now;
                    p.UpdatedAt = now;
                    break;
                case ProviderResponse r:
                    if (entry.State == EntityState.Added) r.CreatedAt = now;
                    r.UpdatedAt = now;
                    break;
                case AppSetting s:
                    s.UpdatedAt = now;
                    break;
                case MaterialClasificacion mc:
                    mc.UpdatedAt = now;
                    break;
                case CriticidadUbicacion cu:
                    if (entry.State == EntityState.Added) cu.CreatedAt = now;
                    break;
                case ProductoFicha pf:
                    if (entry.State == EntityState.Added) pf.CreatedAt = now;
                    pf.UpdatedAt = now;
                    break;
                case ProductoAdjunto pa:
                    if (entry.State == EntityState.Added) pa.SubidoAt = now;
                    break;
            }
        }
    }
}
