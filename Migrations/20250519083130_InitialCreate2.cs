using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspnetCoreMvcFull.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Cranes_CraneId",
                table: "Bookings");

            migrationBuilder.AlterColumn<int>(
                name: "CraneId",
                table: "Bookings",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "CraneCapacity",
                table: "Bookings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CraneCode",
                table: "Bookings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CraneOwnership",
                table: "Bookings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Cranes_CraneId",
                table: "Bookings",
                column: "CraneId",
                principalTable: "Cranes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Cranes_CraneId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CraneCapacity",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CraneCode",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CraneOwnership",
                table: "Bookings");

            migrationBuilder.AlterColumn<int>(
                name: "CraneId",
                table: "Bookings",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Cranes_CraneId",
                table: "Bookings",
                column: "CraneId",
                principalTable: "Cranes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
