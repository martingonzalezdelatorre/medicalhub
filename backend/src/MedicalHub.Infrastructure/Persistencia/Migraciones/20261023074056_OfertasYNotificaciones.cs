using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalHub.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class OfertasYNotificaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notificaciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Mensaje = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EntidadId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Fecha = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FechaLectura = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    OrganizacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificaciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OfertasSustitucion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VacanteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfesionalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OfrecidaPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaOferta = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Caduca = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FechaRespuesta = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    MotivoRespuesta = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    FechaDecision = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DecididaPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MotivoDecision = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    AsignacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    OrganizacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfertasSustitucion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OfertasSustitucion_Asignaciones_AsignacionId",
                        column: x => x.AsignacionId,
                        principalTable: "Asignaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfertasSustitucion_Profesionales_ProfesionalId",
                        column: x => x.ProfesionalId,
                        principalTable: "Profesionales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfertasSustitucion_Vacantes_VacanteId",
                        column: x => x.VacanteId,
                        principalTable: "Vacantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_UsuarioId_FechaLectura_Fecha",
                table: "Notificaciones",
                columns: new[] { "UsuarioId", "FechaLectura", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_OfertasSustitucion_AsignacionId",
                table: "OfertasSustitucion",
                column: "AsignacionId");

            migrationBuilder.CreateIndex(
                name: "IX_OfertasSustitucion_OrganizacionId_Estado",
                table: "OfertasSustitucion",
                columns: new[] { "OrganizacionId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_OfertasSustitucion_ProfesionalId_Estado",
                table: "OfertasSustitucion",
                columns: new[] { "ProfesionalId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_OfertasSustitucion_VacanteId_ProfesionalId",
                table: "OfertasSustitucion",
                columns: new[] { "VacanteId", "ProfesionalId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notificaciones");

            migrationBuilder.DropTable(
                name: "OfertasSustitucion");
        }
    }
}
