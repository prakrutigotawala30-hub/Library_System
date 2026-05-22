using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryManagementSystem.ClassLibrary.Migrations
{
    /// <inheritdoc />
    public partial class eventidWishlist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EventId",
                table: "Wishlists",
                type: "int",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Wishlists_Events_BookId",
                table: "Wishlists",
                column: "BookId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Wishlists_Events_BookId",
                table: "Wishlists");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "Wishlists");
        }
    }
}
