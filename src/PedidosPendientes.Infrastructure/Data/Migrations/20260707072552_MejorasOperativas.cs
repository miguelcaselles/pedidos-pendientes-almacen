using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PedidosPendientes.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MejorasOperativas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AcuerdoMarco",
                table: "Proveedores",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NotasPenalizacion",
                table: "Proveedores",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlazoEntregaDias",
                table: "Proveedores",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EnFaltaAt",
                table: "Orders",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CriticidadUbicaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Nivel = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CriticidadUbicaciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaterialClasificaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Material = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Clase = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialClasificaciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockMd04",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Material = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TextoBreve = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Almacen = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Semaforo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Stock = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    PuntoPedido = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    ConsumoMedio = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    CargadoAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMd04", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UploadLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    Filas = table.Column<int>(type: "int", nullable: false),
                    Insertados = table.Column<int>(type: "int", nullable: false),
                    Actualizados = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    At = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UQ_CriticidadUbicaciones_Tipo_Codigo",
                table: "CriticidadUbicaciones",
                columns: new[] { "Tipo", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_MaterialClasificaciones_Material",
                table: "MaterialClasificaciones",
                column: "Material",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockMd04_Material",
                table: "StockMd04",
                column: "Material");

            migrationBuilder.CreateIndex(
                name: "IX_StockMd04_Semaforo",
                table: "StockMd04",
                column: "Semaforo");

            migrationBuilder.CreateIndex(
                name: "IX_UploadLogs_Tipo_At",
                table: "UploadLogs",
                columns: new[] { "Tipo", "At" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CriticidadUbicaciones");

            migrationBuilder.DropTable(
                name: "MaterialClasificaciones");

            migrationBuilder.DropTable(
                name: "StockMd04");

            migrationBuilder.DropTable(
                name: "UploadLogs");

            migrationBuilder.DropColumn(
                name: "AcuerdoMarco",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "NotasPenalizacion",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "PlazoEntregaDias",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "EnFaltaAt",
                table: "Orders");
        }
    }
}
