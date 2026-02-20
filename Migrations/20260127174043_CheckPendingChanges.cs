using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangaReader.Migrations
{
    
    public partial class CheckPendingChanges : Migration
    {
        
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "PfpDecorations",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 27, 17, 40, 42, 920, DateTimeKind.Utc).AddTicks(2594));

            migrationBuilder.UpdateData(
                table: "PfpDecorations",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 27, 17, 40, 42, 920, DateTimeKind.Utc).AddTicks(4204));
        }

        
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "PfpDecorations",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 27, 17, 36, 26, 816, DateTimeKind.Utc).AddTicks(9208));

            migrationBuilder.UpdateData(
                table: "PfpDecorations",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 27, 17, 36, 26, 817, DateTimeKind.Utc).AddTicks(802));
        }
    }
}
