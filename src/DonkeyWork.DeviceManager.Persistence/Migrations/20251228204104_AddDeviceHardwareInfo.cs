using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.DeviceManager.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceHardwareInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Architecture",
                schema: "DeviceManager",
                table: "Devices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CpuCores",
                schema: "DeviceManager",
                table: "Devices",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OSArchitecture",
                schema: "DeviceManager",
                table: "Devices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperatingSystem",
                schema: "DeviceManager",
                table: "Devices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperatingSystemVersion",
                schema: "DeviceManager",
                table: "Devices",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TotalMemoryBytes",
                schema: "DeviceManager",
                table: "Devices",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Architecture",
                schema: "DeviceManager",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "CpuCores",
                schema: "DeviceManager",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "OSArchitecture",
                schema: "DeviceManager",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "OperatingSystem",
                schema: "DeviceManager",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "OperatingSystemVersion",
                schema: "DeviceManager",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "TotalMemoryBytes",
                schema: "DeviceManager",
                table: "Devices");
        }
    }
}
