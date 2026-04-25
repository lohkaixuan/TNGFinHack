using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiApp.Migrations
{
    /// <inheritdoc />
    public partial class ApplySoftDeleteFilter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_providers_users_owner_user_id",
                table: "providers");

            migrationBuilder.DropIndex(
                name: "IX_providers_owner_user_id",
                table: "providers");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "providers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_providers_UserId",
                table: "providers",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_providers_users_UserId",
                table: "providers",
                column: "UserId",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_providers_users_UserId",
                table: "providers");

            migrationBuilder.DropIndex(
                name: "IX_providers_UserId",
                table: "providers");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "providers");

            migrationBuilder.CreateIndex(
                name: "IX_providers_owner_user_id",
                table: "providers",
                column: "owner_user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_providers_users_owner_user_id",
                table: "providers",
                column: "owner_user_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
