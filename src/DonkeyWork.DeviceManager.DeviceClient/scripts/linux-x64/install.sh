#!/bin/bash
# DonkeyWork Device Manager Client - Linux Installation Script
# Installs the device client as a systemd service

set -e

# Default values
SERVICE_NAME="donkeywork-device-client"
INSTALL_PATH="/opt/donkeywork/device-client"
API_BASE_URL="https://devicemanager.donkeywork.dev"
SERVICE_USER="donkeywork"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

show_help() {
    cat << EOF
DonkeyWork Device Manager Client - Linux Installation

USAGE:
    sudo ./install.sh [OPTIONS]

OPTIONS:
    --service-name <name>   Systemd service name (default: donkeywork-device-client)
    --service-user <user>   User to run the service (default: donkeywork)
    --help                  Show this help message

EXAMPLES:
    sudo ./install.sh
    sudo ./install.sh --service-name "my-device-client"

NOTES:
    - This script must be run as root (use sudo)
    - Self-contained binary includes all dependencies
    - The service will be configured to start automatically
    - Installs to: /opt/donkeywork/device-client
    - API endpoint: https://devicemanager.donkeywork.dev

EOF
    exit 0
}

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --service-name)
            SERVICE_NAME="$2"
            shift 2
            ;;
        --service-user)
            SERVICE_USER="$2"
            shift 2
            ;;
        --help)
            show_help
            ;;
        *)
            echo -e "${RED}ERROR: Unknown option: $1${NC}"
            echo "Run './install.sh --help' for usage information"
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
echo -e "${CYAN}Linux Installation Script${NC}"
echo -e "${CYAN}========================================${NC}"
echo ""

# Step 1: Stop and remove existing service
echo -e "${YELLOW}[1/7] Checking for existing service...${NC}"
if systemctl list-units --full -all | grep -Fq "$SERVICE_NAME.service"; then
    echo -e "${YELLOW}Service '$SERVICE_NAME' already exists. Stopping and removing...${NC}"
    systemctl stop "$SERVICE_NAME" 2>/dev/null || true
    systemctl disable "$SERVICE_NAME" 2>/dev/null || true
    rm -f "/etc/systemd/system/$SERVICE_NAME.service"
    systemctl daemon-reload
    echo -e "${GREEN}Existing service removed.${NC}"
fi

# Step 2: Create service user
echo -e "${YELLOW}[2/7] Creating service user...${NC}"
if ! id "$SERVICE_USER" &>/dev/null; then
    useradd --system --no-create-home --shell /bin/false "$SERVICE_USER"
    echo -e "${GREEN}User '$SERVICE_USER' created.${NC}"
else
    echo -e "${YELLOW}User '$SERVICE_USER' already exists.${NC}"
fi

# Step 3: Backup device tokens (if upgrading)
echo -e "${YELLOW}[3/8] Backing up device tokens (if present)...${NC}"
TOKENS_FILE="$INSTALL_PATH/device-tokens.json"
TOKENS_BACKUP=""
if [ -f "$TOKENS_FILE" ]; then
    TOKENS_BACKUP=$(mktemp)
    cp "$TOKENS_FILE" "$TOKENS_BACKUP"
    echo -e "${GREEN}Device tokens backed up to temporary location${NC}"
else
    echo -e "${YELLOW}No existing device tokens found (fresh install)${NC}"
fi

# Step 4: Create installation directory
echo -e "${YELLOW}[4/8] Creating installation directory...${NC}"
if [ -d "$INSTALL_PATH" ]; then
    echo -e "${YELLOW}Installation directory already exists. Removing old files...${NC}"
    rm -rf "$INSTALL_PATH"
fi
mkdir -p "$INSTALL_PATH"
echo -e "${GREEN}Installation directory created at: $INSTALL_PATH${NC}"

# Step 5: Install OSQuery (if not already installed)
echo -e "${YELLOW}[5/8] Checking OSQuery installation...${NC}"
if ! command -v osqueryi &> /dev/null; then
    echo -e "${YELLOW}OSQuery not found. Installing...${NC}"

    # Detect Linux distribution
    if [ -f /etc/debian_version ]; then
        # Debian/Ubuntu
        export OSQUERY_KEY=1484120AC4E9F8A1A577AEEE97A80C63C9D8B80B
        apt-key adv --keyserver keyserver.ubuntu.com --recv-keys $OSQUERY_KEY
        add-apt-repository 'deb [arch=amd64] https://pkg.osquery.io/deb deb main'
        apt-get update
        apt-get install -y osquery
    elif [ -f /etc/redhat-release ]; then
        # RHEL/CentOS/Fedora
        echo -e "${YELLOW}Installing OSQuery on RHEL/CentOS/Fedora...${NC}"

        # Determine package manager (dnf or yum)
        if command -v dnf &> /dev/null; then
            PKG_MANAGER="dnf"
        else
            PKG_MANAGER="yum"
        fi

        # Install yum-utils if needed
        if ! command -v yum-config-manager &> /dev/null; then
            echo -e "${YELLOW}Installing yum-utils...${NC}"
            $PKG_MANAGER install -y yum-utils 2>/dev/null || echo -e "${YELLOW}Could not install yum-utils${NC}"
        fi

        # Try to install OSQuery
        if curl -L https://pkg.osquery.io/rpm/GPG 2>/dev/null | tee /etc/pki/rpm-gpg/RPM-GPG-KEY-osquery >/dev/null; then
            if command -v yum-config-manager &> /dev/null; then
                yum-config-manager --add-repo https://pkg.osquery.io/rpm/osquery-s3-rpm.repo 2>/dev/null || true
            fi
            $PKG_MANAGER install -y osquery 2>/dev/null || echo -e "${YELLOW}OSQuery installation via package manager failed${NC}"
        else
            echo -e "${YELLOW}Could not download OSQuery GPG key${NC}"
        fi
    else
        echo -e "${RED}WARNING: Unsupported Linux distribution. Please install OSQuery manually.${NC}"
        echo -e "${YELLOW}Visit: https://osquery.io/downloads/official${NC}"
    fi

    if command -v osqueryi &> /dev/null; then
        echo -e "${GREEN}OSQuery installed successfully.${NC}"
    else
        echo -e "${YELLOW}WARNING: OSQuery installation failed. Query features will not work.${NC}"
    fi
else
    echo -e "${GREEN}OSQuery is already installed.${NC}"
fi

# Step 6: Copy application files to installation directory
echo -e "${YELLOW}[6/8] Copying application files to installation directory...${NC}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PACKAGE_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

# Copy the pre-built binary and supporting files (excluding scripts directory)
# Note: We use rsync if available to exclude device-tokens.json from source package
if command -v rsync &> /dev/null; then
    rsync -a --exclude='scripts' --exclude='device-tokens.json' "$PACKAGE_DIR"/ "$INSTALL_PATH/"
else
    cp -r "$PACKAGE_DIR"/* "$INSTALL_PATH/"
    rm -rf "$INSTALL_PATH/scripts"
    rm -f "$INSTALL_PATH/device-tokens.json"  # Remove if accidentally copied from package
fi
chmod +x "$INSTALL_PATH/DonkeyWork.DeviceManager.DeviceClient"

# Restore device tokens if they were backed up
if [ -n "$TOKENS_BACKUP" ] && [ -f "$TOKENS_BACKUP" ]; then
    if cp "$TOKENS_BACKUP" "$TOKENS_FILE"; then
        # Verify the file was restored and has content
        if [ -f "$TOKENS_FILE" ] && [ -s "$TOKENS_FILE" ]; then
            rm "$TOKENS_BACKUP"
            echo -e "${GREEN}Device tokens restored from backup${NC}"
        else
            echo -e "${YELLOW}WARNING: Device tokens restore verification failed. Backup kept at: $TOKENS_BACKUP${NC}"
            echo -e "${YELLOW}Manual intervention required: cp $TOKENS_BACKUP $TOKENS_FILE${NC}"
        fi
    else
        echo -e "${RED}ERROR: Failed to restore device tokens. Backup preserved at: $TOKENS_BACKUP${NC}"
        echo -e "${RED}Manual intervention required: cp $TOKENS_BACKUP $TOKENS_FILE${NC}"
    fi
fi

# Grant CAP_SYS_BOOT capability for system restart/shutdown
echo -e "${YELLOW}Granting system control capabilities (restart & shutdown)...${NC}"
if command -v setcap &> /dev/null; then
    setcap cap_sys_boot+ep "$INSTALL_PATH/DonkeyWork.DeviceManager.DeviceClient" 2>/dev/null || {
        echo -e "${YELLOW}WARNING: Could not set CAP_SYS_BOOT capability.${NC}"
        echo -e "${YELLOW}System restart and shutdown commands will not work.${NC}"
    }

    # Verify capability was set
    if getcap "$INSTALL_PATH/DonkeyWork.DeviceManager.DeviceClient" | grep -q "cap_sys_boot"; then
        echo -e "${GREEN}Capabilities granted: restart and shutdown enabled${NC}"
    else
        echo -e "${YELLOW}WARNING: Capability verification failed.${NC}"
    fi
else
    echo -e "${YELLOW}WARNING: setcap not available.${NC}"
    echo -e "${YELLOW}Install libcap (Rocky/RHEL: dnf install libcap) to enable restart/shutdown.${NC}"
fi

# Update API URL in appsettings.json if specified
if [ "$API_BASE_URL" != "https://devicemanager.donkeywork.dev" ]; then
    echo -e "${YELLOW}[7/8] Updating API URL in configuration...${NC}"
    sed -i "s|http://devicemanager.donkeywork.dev|$API_BASE_URL|g" "$INSTALL_PATH/appsettings.json"
    sed -i "s|https://devicemanager.donkeywork.dev|$API_BASE_URL|g" "$INSTALL_PATH/appsettings.json"
    echo -e "${GREEN}API URL updated to: $API_BASE_URL${NC}"
else
    echo -e "${YELLOW}[7/8] Configuration file already present${NC}"
    echo -e "${GREEN}Using default API URL: $API_BASE_URL${NC}"
fi

chown -R "$SERVICE_USER:$SERVICE_USER" "$INSTALL_PATH"
echo -e "${GREEN}Files copied successfully.${NC}"

# Step 8: Create and start systemd service
echo -e "${YELLOW}[8/8] Installing systemd service...${NC}"
cat > "/etc/systemd/system/$SERVICE_NAME.service" << EOF
[Unit]
Description=DonkeyWork Device Manager Client
After=network.target

[Service]
Type=notify
User=$SERVICE_USER
Group=$SERVICE_USER
WorkingDirectory=$INSTALL_PATH
ExecStart=$INSTALL_PATH/DonkeyWork.DeviceManager.DeviceClient
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=$SERVICE_NAME
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
EOF

# Reload systemd, enable and start service
systemctl daemon-reload
systemctl enable "$SERVICE_NAME"
echo -e "${YELLOW}Starting service...${NC}"
systemctl start "$SERVICE_NAME"

# Wait a moment and check service status
sleep 2
if systemctl is-active --quiet "$SERVICE_NAME"; then
    echo -e "${GREEN}Service started successfully!${NC}"
else
    echo -e "${YELLOW}WARNING: Service was created but is not running.${NC}"
    echo -e "${YELLOW}Check logs with: journalctl -u $SERVICE_NAME -f${NC}"
fi

echo ""
echo -e "${CYAN}========================================${NC}"
echo -e "${GREEN}Installation Complete!${NC}"
echo -e "${CYAN}========================================${NC}"
echo ""
echo -e "Service Name:       ${SERVICE_NAME}"
echo -e "Installation Path:  ${INSTALL_PATH}"
echo -e "Configuration:      ${INSTALL_PATH}/appsettings.json"
echo -e "API Base URL:       ${API_BASE_URL}"
echo -e "Running as User:    ${SERVICE_USER}"
echo ""
echo -e "${YELLOW}NEXT STEPS:${NC}"
echo -e "1. The device client service is now running"
echo -e "2. View logs:        journalctl -u $SERVICE_NAME -f"
echo -e "3. Check status:     systemctl status $SERVICE_NAME"
echo -e "4. Stop service:     systemctl stop $SERVICE_NAME"
echo -e "5. To uninstall:     Run ./uninstall.sh"
echo ""
echo -e "${CYAN}For more information, visit: https://github.com/donkeywork/device-manager${NC}"
echo ""

# Wait for log file to appear and tail it
LOG_FILE="$INSTALL_PATH/DeviceClient.log"
echo -e "${YELLOW}Waiting for log file to appear...${NC}"
WAIT_COUNT=0
while [ ! -f "$LOG_FILE" ] && [ $WAIT_COUNT -lt 10 ]; do
    sleep 1
    WAIT_COUNT=$((WAIT_COUNT + 1))
done

if [ -f "$LOG_FILE" ]; then
    echo -e "${GREEN}Log file found. Tailing logs (Ctrl+C to exit):${NC}"
    echo -e "${CYAN}========================================${NC}"
    tail -f "$LOG_FILE"
else
    echo -e "${YELLOW}Log file not found yet. View logs with: journalctl -u $SERVICE_NAME -f${NC}"
fi
