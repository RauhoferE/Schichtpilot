using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedNormalizedName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "ConcurrencyStamp", "NormalizedName" },
                values: new object[] { "840ea4a6-20fe-488c-a8c0-63fa8b46fc0b", "ADMIN" });

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: 2L,
                columns: new[] { "ConcurrencyStamp", "NormalizedName" },
                values: new object[] { "735f0ce2-b347-438c-b4bf-5364e52eac43", "USER" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "ConcurrencyStamp", "NormalizedName" },
                values: new object[] { "0bea3e06-b90e-4d36-8e92-69e05dec6cb2", null });

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: 2L,
                columns: new[] { "ConcurrencyStamp", "NormalizedName" },
                values: new object[] { "e1101b84-bab0-45e9-b1f5-6a46cbfe8b52", null });
        }
    }
}
