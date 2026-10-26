using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalHub.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class DisponibilidadPorHospital : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CentroId",
                table: "Disponibilidades",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Disponibilidades_CentroId",
                table: "Disponibilidades",
                column: "CentroId");

            migrationBuilder.AddForeignKey(
                name: "FK_Disponibilidades_Centros_CentroId",
                table: "Disponibilidades",
                column: "CentroId",
                principalTable: "Centros",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Disponibilidades_Centros_CentroId",
                table: "Disponibilidades");

            migrationBuilder.DropIndex(
                name: "IX_Disponibilidades_CentroId",
                table: "Disponibilidades");

            migrationBuilder.DropColumn(
                name: "CentroId",
                table: "Disponibilidades");
        }
    }
}
