using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalHub.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class TarifasTurno : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TarifasTurno",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoTurnoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoriaProfesionalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Importe = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    OrganizacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TarifasTurno", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TarifasTurno_CategoriasProfesionales_CategoriaProfesionalId",
                        column: x => x.CategoriaProfesionalId,
                        principalTable: "CategoriasProfesionales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TarifasTurno_TiposTurno_TipoTurnoId",
                        column: x => x.TipoTurnoId,
                        principalTable: "TiposTurno",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TarifasTurno_CategoriaProfesionalId",
                table: "TarifasTurno",
                column: "CategoriaProfesionalId");

            migrationBuilder.CreateIndex(
                name: "IX_TarifasTurno_TipoTurnoId_CategoriaProfesionalId",
                table: "TarifasTurno",
                columns: new[] { "TipoTurnoId", "CategoriaProfesionalId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TarifasTurno");
        }
    }
}
