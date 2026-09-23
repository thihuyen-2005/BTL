using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamSchedule.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProctorManagementGenerated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "required_proctor_count",
                table: "ca_thi",
                type: "int",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.CreateTable(
                name: "proctor_profiles",
                columns: table => new
                {
                    proctor_profile_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    staff_code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    department = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    phone = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proctor_profiles", x => x.proctor_profile_id);
                    table.ForeignKey(
                        name: "FK_proctor_profiles_app_users_user_id",
                        column: x => x.user_id,
                        principalTable: "app_users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "proctor_assignments",
                columns: table => new
                {
                    proctor_assignment_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ca_thi_id = table.Column<int>(type: "int", nullable: false),
                    proctor_profile_id = table.Column<int>(type: "int", nullable: false),
                    role = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    assigned_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    cancelled_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proctor_assignments", x => x.proctor_assignment_id);
                    table.ForeignKey(
                        name: "FK_proctor_assignments_ca_thi_ca_thi_id",
                        column: x => x.ca_thi_id,
                        principalTable: "ca_thi",
                        principalColumn: "ca_thi_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_proctor_assignments_proctor_profiles_proctor_profile_id",
                        column: x => x.proctor_profile_id,
                        principalTable: "proctor_profiles",
                        principalColumn: "proctor_profile_id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_proctor_assignments_ca_thi_id_proctor_profile_id",
                table: "proctor_assignments",
                columns: new[] { "ca_thi_id", "proctor_profile_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_proctor_assignments_ca_thi_id_role",
                table: "proctor_assignments",
                columns: new[] { "ca_thi_id", "role" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_proctor_assignments_proctor_profile_id",
                table: "proctor_assignments",
                column: "proctor_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_proctor_profiles_staff_code",
                table: "proctor_profiles",
                column: "staff_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_proctor_profiles_user_id",
                table: "proctor_profiles",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "proctor_assignments");

            migrationBuilder.DropTable(
                name: "proctor_profiles");

            migrationBuilder.DropColumn(
                name: "required_proctor_count",
                table: "ca_thi");
        }
    }
}
