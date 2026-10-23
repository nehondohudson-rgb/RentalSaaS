using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalSaaS.Api.Migrations
{
    /// <inheritdoc />
    public partial class HardenLeaseConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Leases_RentalUnits_RentalUnitId",
                table: "Leases");

            migrationBuilder.DropForeignKey(
                name: "FK_Leases_Tenants_TenantId",
                table: "Leases");

            migrationBuilder.DropIndex(
                name: "IX_Leases_RentalUnitId",
                table: "Leases");

            migrationBuilder.AlterColumn<decimal>(
                name: "MonthlyRent",
                table: "Leases",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.CreateIndex(
                name: "IX_Leases_RentalUnitId",
                table: "Leases",
                column: "RentalUnitId",
                unique: true,
                filter: "\"Status\" = 'Active'");

            migrationBuilder.AddForeignKey(
                name: "FK_Leases_RentalUnits_RentalUnitId",
                table: "Leases",
                column: "RentalUnitId",
                principalTable: "RentalUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Leases_Tenants_TenantId",
                table: "Leases",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Leases_RentalUnits_RentalUnitId",
                table: "Leases");

            migrationBuilder.DropForeignKey(
                name: "FK_Leases_Tenants_TenantId",
                table: "Leases");

            migrationBuilder.DropIndex(
                name: "IX_Leases_RentalUnitId",
                table: "Leases");

            migrationBuilder.AlterColumn<decimal>(
                name: "MonthlyRent",
                table: "Leases",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldPrecision: 12,
                oldScale: 2);

            migrationBuilder.CreateIndex(
                name: "IX_Leases_RentalUnitId",
                table: "Leases",
                column: "RentalUnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_Leases_RentalUnits_RentalUnitId",
                table: "Leases",
                column: "RentalUnitId",
                principalTable: "RentalUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Leases_Tenants_TenantId",
                table: "Leases",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
