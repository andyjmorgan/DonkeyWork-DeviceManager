using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.DeviceManager.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceOnlineStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Online",
                schema: "DeviceManager",
                table: "Devices",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Online",
                schema: "DeviceManager",
                table: "Devices");
        }
    }
}
