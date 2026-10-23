using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalHub.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class HorasEInformes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RegistrosHoras",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AsignacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfesionalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InicioRealUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FinRealUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RegistradoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaRegistro = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    OrganizacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosHoras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrosHoras_Asignaciones_AsignacionId",
                        column: x => x.AsignacionId,
                        principalTable: "Asignaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RegistrosHoras_Profesionales_ProfesionalId",
                        column: x => x.ProfesionalId,
                        principalTable: "Profesionales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AjustesHoras",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegistroHorasId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Minutos = table.Column<int>(type: "int", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Fecha = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    OrganizacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AjustesHoras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AjustesHoras_RegistrosHoras_RegistroHorasId",
                        column: x => x.RegistroHorasId,
                        principalTable: "RegistrosHoras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AjustesHoras_RegistroHorasId",
                table: "AjustesHoras",
                column: "RegistroHorasId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosHoras_AsignacionId",
                table: "RegistrosHoras",
                column: "AsignacionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosHoras_OrganizacionId_ProfesionalId",
                table: "RegistrosHoras",
                columns: new[] { "OrganizacionId", "ProfesionalId" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosHoras_ProfesionalId",
                table: "RegistrosHoras",
                column: "ProfesionalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AjustesHoras");

            migrationBuilder.DropTable(
                name: "RegistrosHoras");
        }
    }
}
