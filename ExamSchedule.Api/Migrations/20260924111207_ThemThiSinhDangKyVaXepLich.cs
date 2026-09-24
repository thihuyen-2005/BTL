using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamSchedule.Api.Migrations
{
    /// <inheritdoc />
    public partial class ThemThiSinhDangKyVaXepLich : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "hinh_thuc_thi",
                table: "ca_thi",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "TrenMay")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "thisinh",
                columns: table => new
                {
                    thisinh_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ma_thisinh = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ho_ten = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ngay_sinh = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    gioi_tinh = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    dan_toc = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    noi_sinh = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    quoc_tich = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    so_cccd_ho_chieu = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    so_dien_thoai = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    lop = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    nganh_hoc = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    khoa = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    so_tien = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    email_ca_nhan = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ngay_tao = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_thisinh", x => x.thisinh_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "dangkythi",
                columns: table => new
                {
                    dangkythi_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    thisinh_id = table.Column<int>(type: "int", nullable: false),
                    ky_thi_id = table.Column<int>(type: "int", nullable: false),
                    ca_thi_id = table.Column<int>(type: "int", nullable: true),
                    trang_thai = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ly_do_chua_xep = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ngay_dang_ky = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ngay_cap_nhat = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dangkythi", x => x.dangkythi_id);
                    table.ForeignKey(
                        name: "FK_dangkythi_ca_thi_ca_thi_id",
                        column: x => x.ca_thi_id,
                        principalTable: "ca_thi",
                        principalColumn: "ca_thi_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_dangkythi_ky_thi_ky_thi_id",
                        column: x => x.ky_thi_id,
                        principalTable: "ky_thi",
                        principalColumn: "ky_thi_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dangkythi_thisinh_thisinh_id",
                        column: x => x.thisinh_id,
                        principalTable: "thisinh",
                        principalColumn: "thisinh_id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_dangkythi_ca_thi_id",
                table: "dangkythi",
                column: "ca_thi_id");

            migrationBuilder.CreateIndex(
                name: "IX_dangkythi_ky_thi_id",
                table: "dangkythi",
                column: "ky_thi_id");

            migrationBuilder.CreateIndex(
                name: "IX_dangkythi_thisinh_id_ky_thi_id",
                table: "dangkythi",
                columns: new[] { "thisinh_id", "ky_thi_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_thisinh_ma_thisinh",
                table: "thisinh",
                column: "ma_thisinh",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_thisinh_so_cccd_ho_chieu",
                table: "thisinh",
                column: "so_cccd_ho_chieu",
                unique: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_giam_thi_phan_cong_ca_thi_ca_thi_id",
                table: "giam_thi_phan_cong");

            migrationBuilder.DropForeignKey(
                name: "FK_giam_thi_phan_cong_giam_thi_giam_thi_id",
                table: "giam_thi_phan_cong");

            migrationBuilder.DropTable(
                name: "dangkythi");

            migrationBuilder.DropTable(
                name: "thisinh");

            migrationBuilder.DropPrimaryKey(
                name: "PK_giam_thi_phan_cong",
                table: "giam_thi_phan_cong");

            migrationBuilder.DropPrimaryKey(
                name: "PK_giam_thi",
                table: "giam_thi");

            migrationBuilder.DropColumn(
                name: "hinh_thuc_thi",
                table: "ca_thi");

            migrationBuilder.DropColumn(
                name: "email",
                table: "giam_thi");

            migrationBuilder.DropColumn(
                name: "ho_ten",
                table: "giam_thi");

            migrationBuilder.RenameTable(
                name: "giam_thi_phan_cong",
                newName: "proctor_assignments");

            migrationBuilder.RenameTable(
                name: "giam_thi",
                newName: "proctor_profiles");

            migrationBuilder.RenameColumn(
                name: "vai_tro",
                table: "proctor_assignments",
                newName: "role");

            migrationBuilder.RenameColumn(
                name: "trang_thai",
                table: "proctor_assignments",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "ngay_phan_cong",
                table: "proctor_assignments",
                newName: "assigned_at");

            migrationBuilder.RenameColumn(
                name: "ngay_huy",
                table: "proctor_assignments",
                newName: "cancelled_at");

            migrationBuilder.RenameColumn(
                name: "giam_thi_id",
                table: "proctor_assignments",
                newName: "proctor_profile_id");

            migrationBuilder.RenameColumn(
                name: "phan_cong_id",
                table: "proctor_assignments",
                newName: "proctor_assignment_id");

            migrationBuilder.RenameIndex(
                name: "IX_giam_thi_phan_cong_giam_thi_id",
                table: "proctor_assignments",
                newName: "IX_proctor_assignments_proctor_profile_id");

            migrationBuilder.RenameIndex(
                name: "IX_giam_thi_phan_cong_ca_thi_id_vai_tro",
                table: "proctor_assignments",
                newName: "IX_proctor_assignments_ca_thi_id_role");

            migrationBuilder.RenameIndex(
                name: "IX_giam_thi_phan_cong_ca_thi_id_giam_thi_id",
                table: "proctor_assignments",
                newName: "IX_proctor_assignments_ca_thi_id_proctor_profile_id");

            migrationBuilder.RenameColumn(
                name: "trang_thai",
                table: "proctor_profiles",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "so_dien_thoai",
                table: "proctor_profiles",
                newName: "phone");

            migrationBuilder.RenameColumn(
                name: "ngay_tao",
                table: "proctor_profiles",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "ma_giam_thi",
                table: "proctor_profiles",
                newName: "staff_code");

            migrationBuilder.RenameColumn(
                name: "don_vi",
                table: "proctor_profiles",
                newName: "department");

            migrationBuilder.RenameColumn(
                name: "giam_thi_id",
                table: "proctor_profiles",
                newName: "proctor_profile_id");

            migrationBuilder.RenameIndex(
                name: "IX_giam_thi_ma_giam_thi",
                table: "proctor_profiles",
                newName: "IX_proctor_profiles_staff_code");

            migrationBuilder.AlterColumn<string>(
                name: "ghi_chu",
                table: "ca_thi",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "phone",
                table: "proctor_profiles",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "user_id",
                table: "proctor_profiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_proctor_assignments",
                table: "proctor_assignments",
                column: "proctor_assignment_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_proctor_profiles",
                table: "proctor_profiles",
                column: "proctor_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_proctor_profiles_user_id",
                table: "proctor_profiles",
                column: "user_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_proctor_assignments_ca_thi_ca_thi_id",
                table: "proctor_assignments",
                column: "ca_thi_id",
                principalTable: "ca_thi",
                principalColumn: "ca_thi_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_proctor_assignments_proctor_profiles_proctor_profile_id",
                table: "proctor_assignments",
                column: "proctor_profile_id",
                principalTable: "proctor_profiles",
                principalColumn: "proctor_profile_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_proctor_profiles_app_users_user_id",
                table: "proctor_profiles",
                column: "user_id",
                principalTable: "app_users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
