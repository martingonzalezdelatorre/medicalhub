using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalHub.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class ReglasYExcepciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExcepcionesRegla",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AsignacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TurnoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfesionalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaveRegla = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    ReglaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Mensaje = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Justificacion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    AutorizadaPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Fecha = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    OrganizacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExcepcionesRegla", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExcepcionesRegla_Asignaciones_AsignacionId",
                        column: x => x.AsignacionId,
                        principalTable: "Asignaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExcepcionesRegla_Profesionales_ProfesionalId",
                        column: x => x.ProfesionalId,
                        principalTable: "Profesionales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExcepcionesRegla_Turnos_TurnoId",
                        column: x => x.TurnoId,
                        principalTable: "Turnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReglasPlanificacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ServicioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Severidad = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Activa = table.Column<bool>(type: "bit", nullable: false),
                    PermiteExcepcion = table.Column<bool>(type: "bit", nullable: false),
                    ExcepcionRequiereAdministrador = table.Column<bool>(type: "bit", nullable: false),
                    ParametrosJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Fundamento = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RevisadaEl = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RevisadaPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    OrganizacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReglasPlanificacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReglasPlanificacion_Servicios_ServicioId",
                        column: x => x.ServicioId,
                        principalTable: "Servicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExcepcionesRegla_AsignacionId_ClaveRegla",
                table: "ExcepcionesRegla",
                columns: new[] { "AsignacionId", "ClaveRegla" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExcepcionesRegla_OrganizacionId_Fecha",
                table: "ExcepcionesRegla",
                columns: new[] { "OrganizacionId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_ExcepcionesRegla_ProfesionalId",
                table: "ExcepcionesRegla",
                column: "ProfesionalId");

            migrationBuilder.CreateIndex(
                name: "IX_ExcepcionesRegla_TurnoId",
                table: "ExcepcionesRegla",
                column: "TurnoId");

            migrationBuilder.CreateIndex(
                name: "IX_ReglasPlanificacion_OrganizacionId_Tipo",
                table: "ReglasPlanificacion",
                columns: new[] { "OrganizacionId", "Tipo" });

            migrationBuilder.CreateIndex(
                name: "IX_ReglasPlanificacion_ServicioId",
                table: "ReglasPlanificacion",
                column: "ServicioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExcepcionesRegla");

            migrationBuilder.DropTable(
                name: "ReglasPlanificacion");
        }
    }
}
