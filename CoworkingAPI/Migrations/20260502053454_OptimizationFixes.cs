using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoworkingAPI.Migrations
{
    /// <inheritdoc />
    public partial class OptimizationFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Floors_Buildings_BuildingId",
                table: "Floors");

            migrationBuilder.DropForeignKey(
                name: "FK_RestroomAssets_Restrooms_RestroomId",
                table: "RestroomAssets");

            migrationBuilder.DropForeignKey(
                name: "FK_Restrooms_Floors_FloorId",
                table: "Restrooms");

            migrationBuilder.DropIndex(
                name: "IX_IssueReports_RestroomAssetId",
                table: "IssueReports");

            migrationBuilder.AlterColumn<short>(
                name: "GridWidth",
                table: "Restrooms",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<short>(
                name: "GridHeight",
                table: "Restrooms",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Restrooms",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<byte>(
                name: "Type",
                table: "RestroomAssets",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<short>(
                name: "PositionY",
                table: "RestroomAssets",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<short>(
                name: "PositionX",
                table: "RestroomAssets",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "RestroomAssets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<byte>(
                name: "Status",
                table: "IssueReports",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ResolutionDate",
                table: "IssueReports",
                type: "datetimeoffset",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "GenerationDate",
                table: "IssueReports",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "AttentionDate",
                table: "IssueReports",
                type: "datetimeoffset",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "IssueReports",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Floors",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Buildings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_IssueReports_RestroomAssetId_Status",
                table: "IssueReports",
                columns: new[] { "RestroomAssetId", "Status" },
                unique: true,
                filter: "[Status] < 2 AND [IsDeleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_Floors_Buildings_BuildingId",
                table: "Floors",
                column: "BuildingId",
                principalTable: "Buildings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RestroomAssets_Restrooms_RestroomId",
                table: "RestroomAssets",
                column: "RestroomId",
                principalTable: "Restrooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Restrooms_Floors_FloorId",
                table: "Restrooms",
                column: "FloorId",
                principalTable: "Floors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Floors_Buildings_BuildingId",
                table: "Floors");

            migrationBuilder.DropForeignKey(
                name: "FK_RestroomAssets_Restrooms_RestroomId",
                table: "RestroomAssets");

            migrationBuilder.DropForeignKey(
                name: "FK_Restrooms_Floors_FloorId",
                table: "Restrooms");

            migrationBuilder.DropIndex(
                name: "IX_IssueReports_RestroomAssetId_Status",
                table: "IssueReports");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Restrooms");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "RestroomAssets");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "IssueReports");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Floors");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Buildings");

            migrationBuilder.AlterColumn<int>(
                name: "GridWidth",
                table: "Restrooms",
                type: "int",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint");

            migrationBuilder.AlterColumn<int>(
                name: "GridHeight",
                table: "Restrooms",
                type: "int",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint");

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "RestroomAssets",
                type: "int",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint");

            migrationBuilder.AlterColumn<int>(
                name: "PositionY",
                table: "RestroomAssets",
                type: "int",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint");

            migrationBuilder.AlterColumn<int>(
                name: "PositionX",
                table: "RestroomAssets",
                type: "int",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "IssueReports",
                type: "int",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ResolutionDate",
                table: "IssueReports",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "GenerationDate",
                table: "IssueReports",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AttentionDate",
                table: "IssueReports",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_IssueReports_RestroomAssetId",
                table: "IssueReports",
                column: "RestroomAssetId");

            migrationBuilder.AddForeignKey(
                name: "FK_Floors_Buildings_BuildingId",
                table: "Floors",
                column: "BuildingId",
                principalTable: "Buildings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RestroomAssets_Restrooms_RestroomId",
                table: "RestroomAssets",
                column: "RestroomId",
                principalTable: "Restrooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Restrooms_Floors_FloorId",
                table: "Restrooms",
                column: "FloorId",
                principalTable: "Floors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
