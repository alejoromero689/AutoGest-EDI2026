using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoGest.Migrations
{
    /// <inheritdoc />
    public partial class AgregarKilometrajeVehiculo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Kilometraje",
                table: "Vehiculos",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Kilometraje",
                table: "Vehiculos");
        }
    }
}
