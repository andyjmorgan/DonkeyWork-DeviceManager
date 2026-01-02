#!/bin/bash
# DonkeyWork Device Manager Client - Linux Uninstallation Script
# Removes the device client systemd service

set -e

# Fixed values
SERVICE_NAME="donkeywork-device-client"
INSTALL_PATH="/opt/donkeywork/device-client"
SERVICE_USER="donkeywork"
KEEP_CONFIG=false

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

show_help() {
    cat << EOF
DonkeyWork Device Manager Client - Linux Uninstallation

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

echo -e "${CYAN}========================================${NC}"
echo -e "${CYAN}DonkeyWork Device Manager Client${NC}"
echo -e "${CYAN}Linux Uninstallation Script${NC}"
echo -e "${CYAN}========================================${NC}"
echo ""

# Step 1: Stop and remove systemd service
echo -e "${YELLOW}[1/3] Stopping and removing systemd service...${NC}"
if systemctl list-units --full -all | grep -Fq "$SERVICE_NAME.service"; then
    if systemctl is-active --quiet "$SERVICE_NAME"; then
        echo -e "${YELLOW}Stopping service...${NC}"
        systemctl stop "$SERVICE_NAME"
    fi

    echo -e "${YELLOW}Disabling and removing service...${NC}"
    systemctl disable "$SERVICE_NAME" 2>/dev/null || true
    rm -f "/etc/systemd/system/$SERVICE_NAME.service"
    systemctl daemon-reload
    systemctl reset-failed
    echo -e "${GREEN}Service removed successfully.${NC}"
else
    echo -e "${YELLOW}Service not found. Skipping...${NC}"
fi

# Step 2: Remove installation directory
echo -e "${YELLOW}[2/3] Removing installation files...${NC}"
if [ -d "$INSTALL_PATH" ]; then
    if [ "$KEEP_CONFIG" = true ]; then
        echo -e "${YELLOW}Keeping configuration files as requested.${NC}"

        # Backup config files
        BACKUP_DIR="/tmp/donkeywork-device-client-backup-$(date +%Y%m%d-%H%M%S)"
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

# Step 3: Remove service user
echo -e "${YELLOW}[3/3] Removing service user...${NC}"
if id "$SERVICE_USER" &>/dev/null; then
    userdel "$SERVICE_USER" 2>/dev/null || true
    echo -e "${GREEN}User '$SERVICE_USER' removed.${NC}"
else
    echo -e "${YELLOW}User '$SERVICE_USER' not found. Skipping...${NC}"
fi

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
