using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace Library_Management_System.Migrations
{
    public partial class EventTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),

                    Title = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: false),

                    Description = table.Column<string>(
                        type: "nvarchar(1000)",
                        maxLength: 1000,
                        nullable: false),

                    Date = table.Column<DateTime>(
                        type: "datetime2",
                        nullable: false),

                    Location = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: false),

                    ImagePath = table.Column<string>(
                        type: "nvarchar(max)",
                        nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Events");
        }
    }
}