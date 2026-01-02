#!/bin/bash
# DonkeyWork Device Manager Client - macOS Uninstallation Script
# Removes the device client launchd service

set -e

# Fixed values
SERVICE_LABEL="com.donkeywork.device-client"
INSTALL_PATH="/usr/local/opt/donkeywork/device-client"
KEEP_CONFIG=false

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

show_help() {
    cat << EOF
DonkeyWork Device Manager Client - macOS Uninstallation

USAGE:
    sudo ./uninstall.sh [OPTIONS]

OPTIONS:
    --keep-config           Keep configuration and token files
    --help                  Show this help message

EXAMPLES:
    sudo ./uninstall.sh
    sudo ./uninstall.sh --keep-config

EOF
    exit 0
}

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --keep-config)
            KEEP_CONFIG=true
            shift
            ;;
        --help)
            show_help
            ;;
        *)
            echo -e "${RED}ERROR: Unknown option: $1${NC}"
            echo "Run './uninstall.sh --help' for usage information"
            exit 1
            ;;
    esac
done

# Check if running as root
if [[ $EUID -ne 0 ]]; then
   echo -e "${RED}ERROR: This script must be run as root (use sudo)${NC}"
   exit 1
fi

PLIST_PATH="/Library/LaunchDaemons/$SERVICE_LABEL.plist"

echo -e "${CYAN}========================================${NC}"
echo -e "${CYAN}DonkeyWork Device Manager Client${NC}"
echo -e "${CYAN}macOS Uninstallation Script${NC}"
echo -e "${CYAN}========================================${NC}"
echo ""

# Step 1: Stop and remove launchd service
echo -e "${YELLOW}[1/2] Stopping and removing launchd service...${NC}"
if [ -f "$PLIST_PATH" ]; then
    echo -e "${YELLOW}Unloading service...${NC}"
    launchctl bootout system "$PLIST_PATH" 2>/dev/null || true

    echo -e "${YELLOW}Removing service file...${NC}"
    rm -f "$PLIST_PATH"
    echo -e "${GREEN}Service removed successfully.${NC}"
else
    echo -e "${YELLOW}Service not found. Skipping...${NC}"
fi

# Step 2: Remove installation directory
echo -e "${YELLOW}[2/2] Removing installation files...${NC}"
if [ -d "$INSTALL_PATH" ]; then
    if [ "$KEEP_CONFIG" = true ]; then
        echo -e "${YELLOW}Keeping configuration files as requested.${NC}"

        # Backup config files
        BACKUP_DIR="$HOME/donkeywork-device-client-backup-$(date +%Y%m%d-%H%M%S)"
        mkdir -p "$BACKUP_DIR"

        for config_file in "appsettings.json" "device-tokens.json"; do
            if [ -f "$INSTALL_PATH/$config_file" ]; then
                cp "$INSTALL_PATH/$config_file" "$BACKUP_DIR/"
                echo -e "${GREEN}Backed up: $config_file to $BACKUP_DIR${NC}"
            fi
        done
    fi

    rm -rf "$INSTALL_PATH"
    echo -e "${GREEN}Installation directory removed.${NC}"
else
    echo -e "${YELLOW}Installation directory not found. Skipping...${NC}"
fi

# Remove log files
echo -e "${YELLOW}Removing log files...${NC}"
rm -f /var/log/donkeywork-device-client.log
rm -f /var/log/donkeywork-device-client-error.log
echo -e "${GREEN}Log files removed.${NC}"

echo ""
echo -e "${CYAN}========================================${NC}"
echo -e "${GREEN}Uninstallation Complete!${NC}"
echo -e "${CYAN}========================================${NC}"
echo ""

if [ "$KEEP_CONFIG" = true ]; then
    echo -e "${YELLOW}Configuration files backed up to:${NC}"
    echo -e "${BACKUP_DIR}"
    echo ""
fi

echo "The DonkeyWork Device Manager Client has been removed from your system."
