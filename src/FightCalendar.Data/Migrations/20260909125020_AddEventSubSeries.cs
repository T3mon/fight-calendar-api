using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FightCalendar.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEventSubSeries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SubSeries",
                table: "Events",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubSeries",
                table: "Events");
        }
    }
}
