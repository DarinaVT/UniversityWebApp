using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniversityProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new nullable string columns
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Universities",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "Universities",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Universities",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Universities",
                type: "nvarchar(max)",
                nullable: true);

            // Add GPARequirement column
            migrationBuilder.AddColumn<decimal>(
                name: "GPARequirement",
                table: "Universities",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            // Alter Rating from double to decimal
            migrationBuilder.AlterColumn<decimal>(
                name: "Rating",
                table: "Universities",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            // Set GPARequirement to AverageGpa value for existing records
            migrationBuilder.Sql(@"
                UPDATE Universities 
                SET GPARequirement = CAST(AverageGpa AS decimal(18,2))
                WHERE GPARequirement = 0
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove new columns
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Universities");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "Universities");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Universities");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Universities");

            migrationBuilder.DropColumn(
                name: "GPARequirement",
                table: "Universities");

            // Revert Rating back to double
            migrationBuilder.AlterColumn<double>(
                name: "Rating",
                table: "Universities",
                type: "float",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");
        }
    }
}

