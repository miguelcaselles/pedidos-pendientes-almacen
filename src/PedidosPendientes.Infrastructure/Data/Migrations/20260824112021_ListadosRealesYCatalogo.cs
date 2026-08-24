using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PedidosPendientes.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ListadosRealesYCatalogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockMd04_Semaforo",
                table: "StockMd04");

            migrationBuilder.AddColumn<string>(
                name: "Ambito",
                table: "StockMd04",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: false,
                defaultValue: "almacenaje");

            migrationBuilder.AddColumn<string>(
                name: "AreaPlanificacion",
                table: "StockMd04",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BajoStockSeguridad",
                table: "StockMd04",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PedidosFueraPlazo",
                table: "StockMd04",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatusNexus",
                table: "StockMd04",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CantidadPedido",
                table: "Orders",
                type: "decimal(12,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContratoMarco",
                table: "Orders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AlmacenCecos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Centro = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Almacen = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DenominacionAlmacen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CentroCoste = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DenominacionCentroCoste = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CargadoAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlmacenCecos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductoFichas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Material = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Denominacion = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DescripcionAmpliada = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MaterialesAlternativos = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PresupuestoUnitario = table.Column<decimal>(type: "decimal(12,4)", nullable: true),
                    Notas = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductoFichas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductoAdjuntos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductoFichaId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    Contenido = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    SubidoAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductoAdjuntos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductoAdjuntos_ProductoFichas_ProductoFichaId",
                        column: x => x.ProductoFichaId,
                        principalTable: "ProductoFichas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockMd04_Ambito_Semaforo",
                table: "StockMd04",
                columns: new[] { "Ambito", "Semaforo" });

            migrationBuilder.CreateIndex(
                name: "IX_AlmacenCecos_CentroCoste",
                table: "AlmacenCecos",
                column: "CentroCoste");

            migrationBuilder.CreateIndex(
                name: "UQ_AlmacenCecos_Centro_Almacen",
                table: "AlmacenCecos",
                columns: new[] { "Centro", "Almacen" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductoAdjuntos_Ficha",
                table: "ProductoAdjuntos",
                column: "ProductoFichaId");

            migrationBuilder.CreateIndex(
                name: "UQ_ProductoFichas_Material",
                table: "ProductoFichas",
                column: "Material",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlmacenCecos");

            migrationBuilder.DropTable(
                name: "ProductoAdjuntos");

            migrationBuilder.DropTable(
                name: "ProductoFichas");

            migrationBuilder.DropIndex(
                name: "IX_StockMd04_Ambito_Semaforo",
                table: "StockMd04");

            migrationBuilder.DropColumn(
                name: "Ambito",
                table: "StockMd04");

            migrationBuilder.DropColumn(
                name: "AreaPlanificacion",
                table: "StockMd04");

            migrationBuilder.DropColumn(
                name: "BajoStockSeguridad",
                table: "StockMd04");

            migrationBuilder.DropColumn(
                name: "PedidosFueraPlazo",
                table: "StockMd04");

            migrationBuilder.DropColumn(
                name: "StatusNexus",
                table: "StockMd04");

            migrationBuilder.DropColumn(
                name: "CantidadPedido",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ContratoMarco",
                table: "Orders");

            migrationBuilder.CreateIndex(
                name: "IX_StockMd04_Semaforo",
                table: "StockMd04",
                column: "Semaforo");
        }
    }
}
