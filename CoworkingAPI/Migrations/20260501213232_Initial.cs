using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoworkingAPI.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Buildings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Buildings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Floors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FloorNumber = table.Column<int>(type: "int", nullable: false),
                    BuildingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Floors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Floors_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalTable: "Buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Restrooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Identifier = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FloorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GridWidth = table.Column<int>(type: "int", nullable: false),
                    GridHeight = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Restrooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Restrooms_Floors_FloorId",
                        column: x => x.FloorId,
                        principalTable: "Floors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RestroomAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    PositionX = table.Column<int>(type: "int", nullable: false),
                    PositionY = table.Column<int>(type: "int", nullable: false),
                    RestroomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDamaged = table.Column<bool>(type: "bit", nullable: false),
                    PaperLevelPercentage = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestroomAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RestroomAssets_Restrooms_RestroomId",
                        column: x => x.RestroomId,
                        principalTable: "Restrooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IssueReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IssueDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    GenerationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AttentionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolutionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RestroomAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportingUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AssignedOperatorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IssueReports_RestroomAssets_RestroomAssetId",
                        column: x => x.RestroomAssetId,
                        principalTable: "RestroomAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Floors_BuildingId",
                table: "Floors",
                column: "BuildingId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueReports_RestroomAssetId",
                table: "IssueReports",
                column: "RestroomAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_RestroomAssets_RestroomId",
                table: "RestroomAssets",
                column: "RestroomId");

            migrationBuilder.CreateIndex(
                name: "IX_Restrooms_FloorId",
                table: "Restrooms",
                column: "FloorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IssueReports");

            migrationBuilder.DropTable(
                name: "RestroomAssets");

            migrationBuilder.DropTable(
                name: "Restrooms");

            migrationBuilder.DropTable(
                name: "Floors");

            migrationBuilder.DropTable(
                name: "Buildings");
        }
    }
}
