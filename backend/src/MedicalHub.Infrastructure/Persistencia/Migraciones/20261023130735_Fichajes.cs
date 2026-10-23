using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalHub.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class Fichajes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitud",
                table: "Centros",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitud",
                table: "Centros",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RadioMetros",
                table: "Centros",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Fichajes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AsignacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfesionalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    InicioUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FinUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EntradaLatitud = table.Column<double>(type: "float", nullable: true),
                    EntradaLongitud = table.Column<double>(type: "float", nullable: true),
                    EntradaPrecisionMetros = table.Column<double>(type: "float", nullable: true),
                    EntradaFueraDeZona = table.Column<bool>(type: "bit", nullable: true),
                    SalidaLatitud = table.Column<double>(type: "float", nullable: true),
                    SalidaLongitud = table.Column<double>(type: "float", nullable: true),
                    SalidaPrecisionMetros = table.Column<double>(type: "float", nullable: true),
                    SalidaFueraDeZona = table.Column<bool>(type: "bit", nullable: true),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    OrganizacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fichajes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Fichajes_Asignaciones_AsignacionId",
                        column: x => x.AsignacionId,
                        principalTable: "Asignaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Fichajes_Profesionales_ProfesionalId",
                        column: x => x.ProfesionalId,
                        principalTable: "Profesionales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PausasFichaje",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InicioUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FinUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FichajeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PausasFichaje", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PausasFichaje_Fichajes_FichajeId",
                        column: x => x.FichajeId,
                        principalTable: "Fichajes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Fichajes_AsignacionId",
                table: "Fichajes",
                column: "AsignacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Fichajes_OrganizacionId_ProfesionalId_InicioUtc",
                table: "Fichajes",
                columns: new[] { "OrganizacionId", "ProfesionalId", "InicioUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Fichajes_ProfesionalId",
                table: "Fichajes",
                column: "ProfesionalId",
                unique: true,
                filter: "[Estado] IN ('EnCurso', 'EnPausa')");

            migrationBuilder.CreateIndex(
                name: "IX_PausasFichaje_FichajeId",
                table: "PausasFichaje",
                column: "FichajeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PausasFichaje");

            migrationBuilder.DropTable(
                name: "Fichajes");

            migrationBuilder.DropColumn(
                name: "Latitud",
                table: "Centros");

            migrationBuilder.DropColumn(
                name: "Longitud",
                table: "Centros");

            migrationBuilder.DropColumn(
                name: "RadioMetros",
                table: "Centros");
        }
    }
}
