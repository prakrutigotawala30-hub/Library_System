using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddBorrowRecordIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_BorrowRecords_DueDate",
                table: "BorrowRecords",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_BorrowRecords_IssuedOn",
                table: "BorrowRecords",
                column: "IssuedOn");

            migrationBuilder.CreateIndex(
                name: "IX_BorrowRecords_ReturnedOn",
                table: "BorrowRecords",
                column: "ReturnedOn");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BorrowRecords_DueDate",
                table: "BorrowRecords");

            migrationBuilder.DropIndex(
                name: "IX_BorrowRecords_IssuedOn",
                table: "BorrowRecords");

            migrationBuilder.DropIndex(
                name: "IX_BorrowRecords_ReturnedOn",
                table: "BorrowRecords");
        }
    }
}
