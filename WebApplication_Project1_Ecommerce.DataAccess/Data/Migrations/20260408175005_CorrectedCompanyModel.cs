using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication_Project1_Ecommerce.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class CorrectedCompanyModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StreetAddress",
                table: "Companies",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StreetAddress",
                table: "Companies");
        }
    }
}
