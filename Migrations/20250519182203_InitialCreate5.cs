using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspnetCoreMvcFull.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Breakdowns_Cranes_CraneId",
                table: "Breakdowns");

            migrationBuilder.AlterColumn<int>(
                name: "CraneId",
                table: "Breakdowns",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "CraneCapacity",
                table: "Breakdowns",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CraneCode",
                table: "Breakdowns",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Breakdowns_Cranes_CraneId",
                table: "Breakdowns",
                column: "CraneId",
                principalTable: "Cranes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Breakdowns_Cranes_CraneId",
                table: "Breakdowns");

            migrationBuilder.DropColumn(
                name: "CraneCapacity",
                table: "Breakdowns");

            migrationBuilder.DropColumn(
                name: "CraneCode",
                table: "Breakdowns");

            migrationBuilder.AlterColumn<int>(
                name: "CraneId",
                table: "Breakdowns",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Breakdowns_Cranes_CraneId",
                table: "Breakdowns",
                column: "CraneId",
                principalTable: "Cranes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
