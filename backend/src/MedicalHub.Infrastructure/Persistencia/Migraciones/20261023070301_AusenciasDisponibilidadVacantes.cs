using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalHub.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class AusenciasDisponibilidadVacantes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MotivoCancelacion",
                table: "Asignaciones",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Ausencias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfesionalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Desde = table.Column<DateOnly>(type: "date", nullable: false),
                    Hasta = table.Column<DateOnly>(type: "date", nullable: false),
                    Comentario = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SolicitadaPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaSolicitud = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    OrganizacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ausencias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ausencias_Profesionales_ProfesionalId",
                        column: x => x.ProfesionalId,
                        principalTable: "Profesionales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Disponibilidades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfesionalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Desde = table.Column<DateOnly>(type: "date", nullable: false),
                    Hasta = table.Column<DateOnly>(type: "date", nullable: false),
                    Dias = table.Column<int>(type: "int", nullable: false),
                    TipoTurnoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Comentario = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DeclaradaPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaDeclaracion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DecididaPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaDecision = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    MotivoDecision = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    OrganizacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Disponibilidades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Disponibilidades_Profesionales_ProfesionalId",
                        column: x => x.ProfesionalId,
                        principalTable: "Profesionales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Disponibilidades_TiposTurno_TipoTurnoId",
                        column: x => x.TipoTurnoId,
                        principalTable: "TiposTurno",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DecisionesAusencia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AusenciaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Decision = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Fecha = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    OrganizacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecisionesAusencia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DecisionesAusencia_Ausencias_AusenciaId",
                        column: x => x.AusenciaId,
                        principalTable: "Ausencias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Vacantes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NecesidadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TurnoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AsignacionOriginalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfesionalAusenteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AusenciaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Motivo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FechaCierre = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AsignacionCubiertaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    OrganizacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vacantes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vacantes_Asignaciones_AsignacionOriginalId",
                        column: x => x.AsignacionOriginalId,
                        principalTable: "Asignaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vacantes_Ausencias_AusenciaId",
                        column: x => x.AusenciaId,
                        principalTable: "Ausencias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vacantes_NecesidadesCobertura_NecesidadId",
                        column: x => x.NecesidadId,
                        principalTable: "NecesidadesCobertura",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vacantes_Profesionales_ProfesionalAusenteId",
                        column: x => x.ProfesionalAusenteId,
                        principalTable: "Profesionales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vacantes_Turnos_TurnoId",
                        column: x => x.TurnoId,
                        principalTable: "Turnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ausencias_OrganizacionId_Estado",
                table: "Ausencias",
                columns: new[] { "OrganizacionId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Ausencias_ProfesionalId_Desde_Hasta",
                table: "Ausencias",
                columns: new[] { "ProfesionalId", "Desde", "Hasta" });

            migrationBuilder.CreateIndex(
                name: "IX_DecisionesAusencia_AusenciaId",
                table: "DecisionesAusencia",
                column: "AusenciaId");

            migrationBuilder.CreateIndex(
                name: "IX_Disponibilidades_OrganizacionId_Estado",
                table: "Disponibilidades",
                columns: new[] { "OrganizacionId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Disponibilidades_ProfesionalId_Desde_Hasta",
                table: "Disponibilidades",
                columns: new[] { "ProfesionalId", "Desde", "Hasta" });

            migrationBuilder.CreateIndex(
                name: "IX_Disponibilidades_TipoTurnoId",
                table: "Disponibilidades",
                column: "TipoTurnoId");

            migrationBuilder.CreateIndex(
                name: "IX_Vacantes_AsignacionOriginalId",
                table: "Vacantes",
                column: "AsignacionOriginalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vacantes_AusenciaId",
                table: "Vacantes",
                column: "AusenciaId");

            migrationBuilder.CreateIndex(
                name: "IX_Vacantes_NecesidadId",
                table: "Vacantes",
                column: "NecesidadId");

            migrationBuilder.CreateIndex(
                name: "IX_Vacantes_OrganizacionId_Estado",
                table: "Vacantes",
                columns: new[] { "OrganizacionId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Vacantes_ProfesionalAusenteId",
                table: "Vacantes",
                column: "ProfesionalAusenteId");

            migrationBuilder.CreateIndex(
                name: "IX_Vacantes_TurnoId",
                table: "Vacantes",
                column: "TurnoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DecisionesAusencia");

            migrationBuilder.DropTable(
                name: "Disponibilidades");

            migrationBuilder.DropTable(
                name: "Vacantes");

            migrationBuilder.DropTable(
                name: "Ausencias");

            migrationBuilder.DropColumn(
                name: "MotivoCancelacion",
                table: "Asignaciones");
        }
    }
}
