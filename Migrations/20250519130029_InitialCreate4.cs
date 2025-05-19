using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspnetCoreMvcFull.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaintenanceSchedules_Cranes_CraneId",
                table: "MaintenanceSchedules");

            migrationBuilder.AlterColumn<int>(
                name: "CraneId",
                table: "MaintenanceSchedules",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "CraneCapacity",
                table: "MaintenanceSchedules",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CraneCode",
                table: "MaintenanceSchedules",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenanceSchedules_Cranes_CraneId",
                table: "MaintenanceSchedules",
                column: "CraneId",
                principalTable: "Cranes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaintenanceSchedules_Cranes_CraneId",
                table: "MaintenanceSchedules");

            migrationBuilder.DropColumn(
                name: "CraneCapacity",
                table: "MaintenanceSchedules");

            migrationBuilder.DropColumn(
                name: "CraneCode",
                table: "MaintenanceSchedules");

            migrationBuilder.AlterColumn<int>(
                name: "CraneId",
                table: "MaintenanceSchedules",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenanceSchedules_Cranes_CraneId",
                table: "MaintenanceSchedules",
                column: "CraneId",
                principalTable: "Cranes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
