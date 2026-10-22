using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalHub.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class CuadrantesTurnosAsignaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cuadrantes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServicioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Desde = table.Column<DateOnly>(type: "date", nullable: false),
                    Hasta = table.Column<DateOnly>(type: "date", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    FechaPublicacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PublicadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    OrganizacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cuadrantes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cuadrantes_Servicios_ServicioId",
                        column: x => x.ServicioId,
                        principalTable: "Servicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlantillasCuadrante",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServicioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DiasPeriodo = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    OrganizacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantillasCuadrante", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlantillasCuadrante_Servicios_ServicioId",
                        column: x => x.ServicioId,
                        principalTable: "Servicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Turnos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CuadranteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServicioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoTurnoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    InicioUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FinUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    OrganizacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Turnos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Turnos_Cuadrantes_CuadranteId",
                        column: x => x.CuadranteId,
                        principalTable: "Cuadrantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Turnos_Servicios_ServicioId",
                        column: x => x.ServicioId,
                        principalTable: "Servicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Turnos_TiposTurno_TipoTurnoId",
                        column: x => x.TipoTurnoId,
                        principalTable: "TiposTurno",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VersionesCuadrante",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CuadranteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    FechaPublicacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TotalTurnos = table.Column<int>(type: "int", nullable: false),
                    PlazasTotales = table.Column<int>(type: "int", nullable: false),
                    PlazasCubiertas = table.Column<int>(type: "int", nullable: false),
                    ContenidoJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    OrganizacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VersionesCuadrante", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VersionesCuadrante_Cuadrantes_CuadranteId",
                        column: x => x.CuadranteId,
                        principalTable: "Cuadrantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlantillaLineas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DiaOffset = table.Column<int>(type: "int", nullable: false),
                    TipoTurnoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroPlazas = table.Column<int>(type: "int", nullable: false),
                    CategoriaProfesionalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PlantillaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantillaLineas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlantillaLineas_CategoriasProfesionales_CategoriaProfesionalId",
                        column: x => x.CategoriaProfesionalId,
                        principalTable: "CategoriasProfesionales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlantillaLineas_PlantillasCuadrante_PlantillaId",
                        column: x => x.PlantillaId,
                        principalTable: "PlantillasCuadrante",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlantillaLineas_TiposTurno_TipoTurnoId",
                        column: x => x.TipoTurnoId,
                        principalTable: "TiposTurno",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NecesidadesCobertura",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TurnoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroPlazas = table.Column<int>(type: "int", nullable: false),
                    CategoriaProfesionalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    OrganizacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NecesidadesCobertura", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NecesidadesCobertura_CategoriasProfesionales_CategoriaProfesionalId",
                        column: x => x.CategoriaProfesionalId,
                        principalTable: "CategoriasProfesionales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NecesidadesCobertura_Turnos_TurnoId",
                        column: x => x.TurnoId,
                        principalTable: "Turnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Asignaciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NecesidadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TurnoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfesionalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    InicioUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FinUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FechaAsignacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AsignadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaCancelacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    OrganizacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Asignaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Asignaciones_NecesidadesCobertura_NecesidadId",
                        column: x => x.NecesidadId,
                        principalTable: "NecesidadesCobertura",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Asignaciones_Profesionales_ProfesionalId",
                        column: x => x.ProfesionalId,
                        principalTable: "Profesionales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Asignaciones_Turnos_TurnoId",
                        column: x => x.TurnoId,
                        principalTable: "Turnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Asignaciones_NecesidadId_ProfesionalId",
                table: "Asignaciones",
                columns: new[] { "NecesidadId", "ProfesionalId" },
                unique: true,
                filter: "[Estado] <> 'Cancelada'");

            migrationBuilder.CreateIndex(
                name: "IX_Asignaciones_ProfesionalId_InicioUtc_FinUtc",
                table: "Asignaciones",
                columns: new[] { "ProfesionalId", "InicioUtc", "FinUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Asignaciones_TurnoId",
                table: "Asignaciones",
                column: "TurnoId");

            migrationBuilder.CreateIndex(
                name: "IX_Cuadrantes_ServicioId_Desde_Hasta",
                table: "Cuadrantes",
                columns: new[] { "ServicioId", "Desde", "Hasta" });

            migrationBuilder.CreateIndex(
                name: "IX_NecesidadesCobertura_CategoriaProfesionalId",
                table: "NecesidadesCobertura",
                column: "CategoriaProfesionalId");

            migrationBuilder.CreateIndex(
                name: "IX_NecesidadesCobertura_TurnoId_CategoriaProfesionalId",
                table: "NecesidadesCobertura",
                columns: new[] { "TurnoId", "CategoriaProfesionalId" },
                unique: true,
                filter: "[CategoriaProfesionalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PlantillaLineas_CategoriaProfesionalId",
                table: "PlantillaLineas",
                column: "CategoriaProfesionalId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantillaLineas_PlantillaId",
                table: "PlantillaLineas",
                column: "PlantillaId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantillaLineas_TipoTurnoId",
                table: "PlantillaLineas",
                column: "TipoTurnoId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantillasCuadrante_ServicioId_Nombre",
                table: "PlantillasCuadrante",
                columns: new[] { "ServicioId", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_CuadranteId_TipoTurnoId_Fecha",
                table: "Turnos",
                columns: new[] { "CuadranteId", "TipoTurnoId", "Fecha" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_OrganizacionId_Fecha",
                table: "Turnos",
                columns: new[] { "OrganizacionId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_ServicioId_InicioUtc",
                table: "Turnos",
                columns: new[] { "ServicioId", "InicioUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_TipoTurnoId",
                table: "Turnos",
                column: "TipoTurnoId");

            migrationBuilder.CreateIndex(
                name: "IX_VersionesCuadrante_CuadranteId_Numero",
                table: "VersionesCuadrante",
                columns: new[] { "CuadranteId", "Numero" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Asignaciones");

            migrationBuilder.DropTable(
                name: "PlantillaLineas");

            migrationBuilder.DropTable(
                name: "VersionesCuadrante");

            migrationBuilder.DropTable(
                name: "NecesidadesCobertura");

            migrationBuilder.DropTable(
                name: "PlantillasCuadrante");

            migrationBuilder.DropTable(
                name: "Turnos");

            migrationBuilder.DropTable(
                name: "Cuadrantes");
        }
    }
}
