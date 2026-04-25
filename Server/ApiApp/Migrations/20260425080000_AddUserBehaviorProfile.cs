using System;
using ApiApp.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiApp.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260425080000_AddUserBehaviorProfile")]
    public partial class AddUserBehaviorProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_behavior_profiles",
                columns: table => new
                {
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    avg_transfer_amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    avg_daily_spend = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    monthly_budget = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    transfer_count = table.Column<int>(type: "integer", nullable: false),
                    spend_count = table.Column<int>(type: "integer", nullable: false),
                    typical_transfer_hour = table.Column<int>(type: "integer", nullable: true),
                    risk_tolerance = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    risk_score_baseline = table.Column<int>(type: "integer", nullable: false),
                    common_transfer_to_json = table.Column<string>(type: "jsonb", nullable: false),
                    common_receive_from_json = table.Column<string>(type: "jsonb", nullable: false),
                    common_categories_json = table.Column<string>(type: "jsonb", nullable: false),
                    last_updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_behavior_profiles", x => x.profile_id);
                    table.ForeignKey(
                        name: "FK_user_behavior_profiles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_behavior_profiles_user_id",
                table: "user_behavior_profiles",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_behavior_profiles");
        }
    }
}
