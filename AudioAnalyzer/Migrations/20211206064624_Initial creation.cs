using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AudioAnalyzer.Migrations
{
    public partial class Initialcreation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Songs",
                columns: table => new
                {
                    FilePath = table.Column<string>(type: "TEXT", nullable: false),
                    Label = table.Column<string>(type: "TEXT", nullable: false),
                    TotalTime = table.Column<double>(type: "REAL", nullable: true),
                    BeatsPerMinute = table.Column<int>(type: "INTEGER", nullable: true),
                    AverageZeroCrossingRate = table.Column<double>(type: "REAL", nullable: true),
                    AverageRootMeanSquare = table.Column<double>(type: "REAL", nullable: true),
                    AverageEnvelope = table.Column<float>(type: "REAL", nullable: true),
                    AverageBandEnergyRatio = table.Column<double>(type: "REAL", nullable: true),
                    AverageSpectralCentroid = table.Column<double>(type: "REAL", nullable: true),
                    AverageBandwidth = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Songs", x => x.FilePath);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Songs");
        }
    }
}
