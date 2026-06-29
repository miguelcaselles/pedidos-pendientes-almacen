using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PedidosPendientes.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "EmailLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProveedorId = table.Column<int>(type: "int", nullable: true),
                    DocumentoCompras = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ProveedorEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ErrorDetalle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentoCompras = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExpedienteAdministrativo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    CentroCoste = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Material = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TipoImputacion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TextoBreve = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    NumMaterialProveedor = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    FechaDocumento = table.Column<DateOnly>(type: "date", nullable: false),
                    TieneHistorialEntrega = table.Column<bool>(type: "bit", nullable: false),
                    ProveedorCodigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ProveedorNombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ReferenciaProveedor = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Almacen = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PorEntregarCantidad = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    Recibido = table.Column<bool>(type: "bit", nullable: false),
                    RecibidoAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CantidadRecibida = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    Reclamado = table.Column<bool>(type: "bit", nullable: false),
                    ReclamadoAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReclamadoCount = table.Column<int>(type: "int", nullable: false),
                    EnFalta = table.Column<bool>(type: "bit", nullable: false),
                    PausaReclamacion = table.Column<bool>(type: "bit", nullable: false),
                    Anulado = table.Column<bool>(type: "bit", nullable: false),
                    AnuladoAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Comentarios = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Incidencia = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUploadAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Proveedores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Fax = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NifCif = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Direccion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodigoPostal = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Poblacion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Centro = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proveedores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProviderResponses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProveedorId = table.Column<int>(type: "int", nullable: true),
                    ProveedorNombre = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocumentoCompras = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Material = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    FechaEstimada = table.Column<DateOnly>(type: "date", nullable: true),
                    Comentario = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RevisionEstado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Ip = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderResponses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_Documento",
                table: "EmailLogs",
                column: "DocumentoCompras");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_SentAt",
                table: "EmailLogs",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Anulado",
                table: "Orders",
                column: "Anulado");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Estado",
                table: "Orders",
                columns: new[] { "Recibido", "TieneHistorialEntrega", "Anulado" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_FechaDocumento",
                table: "Orders",
                column: "FechaDocumento");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ProveedorCodigo",
                table: "Orders",
                column: "ProveedorCodigo");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ProveedorNombre",
                table: "Orders",
                column: "ProveedorNombre");

            migrationBuilder.CreateIndex(
                name: "UQ_Orders_Documento_Material",
                table: "Orders",
                columns: new[] { "DocumentoCompras", "Material" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_Nombre",
                table: "Proveedores",
                column: "Nombre");

            migrationBuilder.CreateIndex(
                name: "UQ_Proveedores_Codigo",
                table: "Proveedores",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProviderResponses_CreatedAt",
                table: "ProviderResponses",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderResponses_Documento",
                table: "ProviderResponses",
                column: "DocumentoCompras");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderResponses_Revision",
                table: "ProviderResponses",
                column: "RevisionEstado");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.DropTable(
                name: "EmailLogs");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Proveedores");

            migrationBuilder.DropTable(
                name: "ProviderResponses");
        }
    }
}
