using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalHub.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class IdentidadYMultiempresa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MfaObligatorio",
                table: "Organizaciones",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "EventosSeguridad",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    Ip = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Detalle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Fecha = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosSeguridad", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegistrosAuditoria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Entidad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntidadId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Accion = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Cambios = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IdCorrelacion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Fecha = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosAuditoria", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TokensRefresco",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FamiliaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HashToken = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Expira = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RevocadoEn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokensRefresco", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    NombreCompleto = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HashContrasena = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IntentosFallidos = table.Column<int>(type: "int", nullable: false),
                    BloqueadoHasta = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    MfaActivado = table.Column<bool>(type: "bit", nullable: false),
                    MfaSecretoProtegido = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    MfaUltimoPaso = table.Column<long>(type: "bigint", nullable: false),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UsuarioRoles",
                columns: table => new
                {
                    Rol = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioRoles", x => new { x.UsuarioId, x.Rol });
                    table.ForeignKey(
                        name: "FK_UsuarioRoles_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventosSeguridad_Fecha",
                table: "EventosSeguridad",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_EventosSeguridad_UsuarioId",
                table: "EventosSeguridad",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAuditoria_Entidad_EntidadId",
                table: "RegistrosAuditoria",
                columns: new[] { "Entidad", "EntidadId" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAuditoria_OrganizacionId_Fecha",
                table: "RegistrosAuditoria",
                columns: new[] { "OrganizacionId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_TokensRefresco_FamiliaId",
                table: "TokensRefresco",
                column: "FamiliaId");

            migrationBuilder.CreateIndex(
                name: "IX_TokensRefresco_HashToken",
                table: "TokensRefresco",
                column: "HashToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TokensRefresco_UsuarioId",
                table: "TokensRefresco",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_OrganizacionId",
                table: "Usuarios",
                column: "OrganizacionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventosSeguridad");

            migrationBuilder.DropTable(
                name: "RegistrosAuditoria");

            migrationBuilder.DropTable(
                name: "TokensRefresco");

            migrationBuilder.DropTable(
                name: "UsuarioRoles");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropColumn(
                name: "MfaObligatorio",
                table: "Organizaciones");
        }
    }
}
