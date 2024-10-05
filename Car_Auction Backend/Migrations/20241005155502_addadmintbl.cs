using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Car_Auction_Backend.Migrations
{
    /// <inheritdoc />
    public partial class addadmintbl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Admins",
                columns: table => new
                {
                    AId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    APassword = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsMainAdmin = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AStatus = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "Pending")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Admins", x => x.AId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Admins");
        }
    }
}
