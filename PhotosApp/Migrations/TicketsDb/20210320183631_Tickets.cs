using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PhotosApp.Migrations.TicketsDb
{
    public partial class Tickets : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                "Tickets",
                table => new
                {
                    Id = table.Column<Guid>("TEXT", nullable: false),
                    UserId = table.Column<Guid>("TEXT", nullable: false),
                    Value = table.Column<byte[]>("BLOB", nullable: true),
                    LastActivity = table.Column<DateTimeOffset>("TEXT", nullable: true),
                    Expires = table.Column<DateTimeOffset>("TEXT", nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_Tickets", x => x.Id); });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                "Tickets");
        }
    }
}