using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalHub.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class CatalogosEstructuraProfesionales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CategoriasProfesionales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    OrganizacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Nombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Activa = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriasProfesionales", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Centros",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Direccion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ZonaHoraria = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    OrganizacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Centros", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Competencias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    OrganizacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Nombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Activa = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Competencias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Especialidades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    OrganizacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Nombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Activa = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Especialidades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Profesionales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Apellidos = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    NumeroColegiado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CategoriaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    OrganizacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profesionales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Profesionales_CategoriasProfesionales_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "CategoriasProfesionales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CentroResponsables",
                columns: table => new
                {
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CentroId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CentroResponsables", x => new { x.CentroId, x.UsuarioId });
                    table.ForeignKey(
                        name: "FK_CentroResponsables_Centros_CentroId",
                        column: x => x.CentroId,
                        principalTable: "Centros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Servicios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CentroId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CoordinadorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConfiguracionReglas = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    OrganizacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servicios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Servicios_Centros_CentroId",
                        column: x => x.CentroId,
                        principalTable: "Centros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProfesionalCompetencias",
                columns: table => new
                {
                    CompetenciaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfesionalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VigenteHasta = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfesionalCompetencias", x => new { x.ProfesionalId, x.CompetenciaId });
                    table.ForeignKey(
                        name: "FK_ProfesionalCompetencias_Competencias_CompetenciaId",
                        column: x => x.CompetenciaId,
                        principalTable: "Competencias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProfesionalCompetencias_Profesionales_ProfesionalId",
                        column: x => x.ProfesionalId,
                        principalTable: "Profesionales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProfesionalEspecialidades",
                columns: table => new
                {
                    EspecialidadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfesionalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfesionalEspecialidades", x => new { x.ProfesionalId, x.EspecialidadId });
                    table.ForeignKey(
                        name: "FK_ProfesionalEspecialidades_Especialidades_EspecialidadId",
                        column: x => x.EspecialidadId,
                        principalTable: "Especialidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProfesionalEspecialidades_Profesionales_ProfesionalId",
                        column: x => x.ProfesionalId,
                        principalTable: "Profesionales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProfesionalServicios",
                columns: table => new
                {
                    ServicioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfesionalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfesionalServicios", x => new { x.ProfesionalId, x.ServicioId });
                    table.ForeignKey(
                        name: "FK_ProfesionalServicios_Profesionales_ProfesionalId",
                        column: x => x.ProfesionalId,
                        principalTable: "Profesionales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProfesionalServicios_Servicios_ServicioId",
                        column: x => x.ServicioId,
                        principalTable: "Servicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServicioCategorias",
                columns: table => new
                {
                    CategoriaProfesionalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServicioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicioCategorias", x => new { x.ServicioId, x.CategoriaProfesionalId });
                    table.ForeignKey(
                        name: "FK_ServicioCategorias_CategoriasProfesionales_CategoriaProfesionalId",
                        column: x => x.CategoriaProfesionalId,
                        principalTable: "CategoriasProfesionales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServicioCategorias_Servicios_ServicioId",
                        column: x => x.ServicioId,
                        principalTable: "Servicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServicioCompetencias",
                columns: table => new
                {
                    CompetenciaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServicioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicioCompetencias", x => new { x.ServicioId, x.CompetenciaId });
                    table.ForeignKey(
                        name: "FK_ServicioCompetencias_Competencias_CompetenciaId",
                        column: x => x.CompetenciaId,
                        principalTable: "Competencias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServicioCompetencias_Servicios_ServicioId",
                        column: x => x.ServicioId,
                        principalTable: "Servicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TiposTurno",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServicioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    HoraInicio = table.Column<TimeOnly>(type: "time", nullable: false),
                    HoraFin = table.Column<TimeOnly>(type: "time", nullable: false),
                    EsGuardia = table.Column<bool>(type: "bit", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    OrganizacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposTurno", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TiposTurno_Servicios_ServicioId",
                        column: x => x.ServicioId,
                        principalTable: "Servicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RequisitosCobertura",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServicioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoTurnoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroProfesionales = table.Column<int>(type: "int", nullable: false),
                    CategoriaProfesionalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    OrganizacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequisitosCobertura", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequisitosCobertura_CategoriasProfesionales_CategoriaProfesionalId",
                        column: x => x.CategoriaProfesionalId,
                        principalTable: "CategoriasProfesionales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequisitosCobertura_Servicios_ServicioId",
                        column: x => x.ServicioId,
                        principalTable: "Servicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequisitosCobertura_TiposTurno_TipoTurnoId",
                        column: x => x.TipoTurnoId,
                        principalTable: "TiposTurno",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CategoriasProfesionales_OrganizacionId_Nombre",
                table: "CategoriasProfesionales",
                columns: new[] { "OrganizacionId", "Nombre" },
                unique: true,
                filter: "[OrganizacionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CentroResponsables_UsuarioId",
                table: "CentroResponsables",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Centros_OrganizacionId_Nombre",
                table: "Centros",
                columns: new[] { "OrganizacionId", "Nombre" },
                unique: true,
                filter: "[OrganizacionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Competencias_OrganizacionId_Nombre",
                table: "Competencias",
                columns: new[] { "OrganizacionId", "Nombre" },
                unique: true,
                filter: "[OrganizacionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Especialidades_OrganizacionId_Nombre",
                table: "Especialidades",
                columns: new[] { "OrganizacionId", "Nombre" },
                unique: true,
                filter: "[OrganizacionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProfesionalCompetencias_CompetenciaId",
                table: "ProfesionalCompetencias",
                column: "CompetenciaId");

            migrationBuilder.CreateIndex(
                name: "IX_Profesionales_CategoriaId",
                table: "Profesionales",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_Profesionales_OrganizacionId_Apellidos_Nombre",
                table: "Profesionales",
                columns: new[] { "OrganizacionId", "Apellidos", "Nombre" });

            migrationBuilder.CreateIndex(
                name: "IX_Profesionales_OrganizacionId_NumeroColegiado",
                table: "Profesionales",
                columns: new[] { "OrganizacionId", "NumeroColegiado" },
                unique: true,
                filter: "[NumeroColegiado] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Profesionales_UsuarioId",
                table: "Profesionales",
                column: "UsuarioId",
                unique: true,
                filter: "[UsuarioId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProfesionalEspecialidades_EspecialidadId",
                table: "ProfesionalEspecialidades",
                column: "EspecialidadId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfesionalServicios_ServicioId",
                table: "ProfesionalServicios",
                column: "ServicioId");

            migrationBuilder.CreateIndex(
                name: "IX_RequisitosCobertura_CategoriaProfesionalId",
                table: "RequisitosCobertura",
                column: "CategoriaProfesionalId");

            migrationBuilder.CreateIndex(
                name: "IX_RequisitosCobertura_ServicioId",
                table: "RequisitosCobertura",
                column: "ServicioId");

            migrationBuilder.CreateIndex(
                name: "IX_RequisitosCobertura_TipoTurnoId_CategoriaProfesionalId",
                table: "RequisitosCobertura",
                columns: new[] { "TipoTurnoId", "CategoriaProfesionalId" },
                unique: true,
                filter: "[CategoriaProfesionalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ServicioCategorias_CategoriaProfesionalId",
                table: "ServicioCategorias",
                column: "CategoriaProfesionalId");

            migrationBuilder.CreateIndex(
                name: "IX_ServicioCompetencias_CompetenciaId",
                table: "ServicioCompetencias",
                column: "CompetenciaId");

            migrationBuilder.CreateIndex(
                name: "IX_Servicios_CentroId_Nombre",
                table: "Servicios",
                columns: new[] { "CentroId", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TiposTurno_ServicioId_Nombre",
                table: "TiposTurno",
                columns: new[] { "ServicioId", "Nombre" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CentroResponsables");

            migrationBuilder.DropTable(
                name: "ProfesionalCompetencias");

            migrationBuilder.DropTable(
                name: "ProfesionalEspecialidades");

            migrationBuilder.DropTable(
                name: "ProfesionalServicios");

            migrationBuilder.DropTable(
                name: "RequisitosCobertura");

            migrationBuilder.DropTable(
                name: "ServicioCategorias");

            migrationBuilder.DropTable(
                name: "ServicioCompetencias");

            migrationBuilder.DropTable(
                name: "Especialidades");

            migrationBuilder.DropTable(
                name: "Profesionales");

            migrationBuilder.DropTable(
                name: "TiposTurno");

            migrationBuilder.DropTable(
                name: "Competencias");

            migrationBuilder.DropTable(
                name: "CategoriasProfesionales");

            migrationBuilder.DropTable(
                name: "Servicios");

            migrationBuilder.DropTable(
                name: "Centros");
        }
    }
}
