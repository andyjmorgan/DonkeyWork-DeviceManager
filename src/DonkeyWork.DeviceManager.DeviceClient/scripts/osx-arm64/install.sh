#!/bin/bash
# DonkeyWork Device Manager Client - macOS Installation Script
# Installs the device client as a launchd service

set -e

# Fixed values
SERVICE_LABEL="com.donkeywork.device-client"
INSTALL_PATH="/usr/local/opt/donkeywork/device-client"
API_BASE_URL="https://devicemanager.donkeywork.dev"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

show_help() {
    cat << EOF
DonkeyWork Device Manager Client - macOS Installation

USAGE:
    sudo ./install.sh [OPTIONS]

OPTIONS:
    --service-label <label> Launchd service label (default: com.donkeywork.device-client)
    --help                  Show this help message

EXAMPLES:
    sudo ./install.sh
    sudo ./install.sh --service-label "com.example.device-client"

NOTES:
    - This script must be run as root (use sudo)
    - Self-contained binary includes all dependencies
    - The service will be configured to start automatically
    - Installs to: /usr/local/opt/donkeywork/device-client
    - API endpoint: https://devicemanager.donkeywork.dev

EOF
    exit 0
}

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --service-label)
            SERVICE_LABEL="$2"
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

# Detect architecture
ARCH=$(uname -m)
if [[ "$ARCH" == "arm64" ]]; then
    RUNTIME="osx-arm64"
elif [[ "$ARCH" == "x86_64" ]]; then
    RUNTIME="osx-x64"
else
    echo -e "${RED}ERROR: Unsupported architecture: $ARCH${NC}"
    exit 1
fi

PLIST_PATH="/Library/LaunchDaemons/$SERVICE_LABEL.plist"

echo -e "${CYAN}========================================${NC}"
echo -e "${CYAN}DonkeyWork Device Manager Client${NC}"
echo -e "${CYAN}macOS Installation Script${NC}"
echo -e "${CYAN}========================================${NC}"
echo ""

# Step 1: Stop and remove existing service
echo -e "${YELLOW}[1/6] Checking for existing service...${NC}"
if [ -f "$PLIST_PATH" ]; then
    echo -e "${YELLOW}Service '$SERVICE_LABEL' already exists. Stopping and removing...${NC}"
    launchctl bootout system "$PLIST_PATH" 2>/dev/null || true
    rm -f "$PLIST_PATH"
    echo -e "${GREEN}Existing service removed.${NC}"
fi

# Step 2: Backup device tokens (if upgrading)
echo -e "${YELLOW}[2/7] Backing up device tokens (if present)...${NC}"
TOKENS_FILE="$INSTALL_PATH/device-tokens.json"
TOKENS_BACKUP=""
if [ -f "$TOKENS_FILE" ]; then
    TOKENS_BACKUP=$(mktemp)
    cp "$TOKENS_FILE" "$TOKENS_BACKUP"
    echo -e "${GREEN}Device tokens backed up to temporary location${NC}"
else
    echo -e "${YELLOW}No existing device tokens found (fresh install)${NC}"
fi

# Step 3: Create installation directory
echo -e "${YELLOW}[3/7] Creating installation directory...${NC}"
if [ -d "$INSTALL_PATH" ]; then
    echo -e "${YELLOW}Installation directory already exists. Removing old files...${NC}"
    rm -rf "$INSTALL_PATH"
fi
mkdir -p "$INSTALL_PATH"
echo -e "${GREEN}Installation directory created at: $INSTALL_PATH${NC}"

# Step 4: Install OSQuery (if not already installed)
echo -e "${YELLOW}[4/7] Checking OSQuery installation...${NC}"
if ! command -v osqueryi &> /dev/null; then
    echo -e "${YELLOW}OSQuery not found. Installing via Homebrew...${NC}"

    if command -v brew &> /dev/null; then
        brew install --cask osquery
        if command -v osqueryi &> /dev/null; then
            echo -e "${GREEN}OSQuery installed successfully.${NC}"
        else
            echo -e "${YELLOW}WARNING: OSQuery installation failed. Query features will not work.${NC}"
        fi
    else
        echo -e "${RED}WARNING: Homebrew not found. Please install OSQuery manually.${NC}"
        echo -e "${YELLOW}Visit: https://osquery.io/downloads/official${NC}"
    fi
else
    echo -e "${GREEN}OSQuery is already installed.${NC}"
fi

# Step 5: Copy application files to installation directory
echo -e "${YELLOW}[5/7] Copying application files to installation directory...${NC}"
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

# Update API URL in appsettings.json if specified
if [ "$API_BASE_URL" != "https://devicemanager.donkeywork.dev" ]; then
    echo -e "${YELLOW}[6/7] Updating API URL in configuration...${NC}"
    sed -i '' "s|http://devicemanager.donkeywork.dev|$API_BASE_URL|g" "$INSTALL_PATH/appsettings.json"
    sed -i '' "s|https://devicemanager.donkeywork.dev|$API_BASE_URL|g" "$INSTALL_PATH/appsettings.json"
    echo -e "${GREEN}API URL updated to: $API_BASE_URL${NC}"
else
    echo -e "${YELLOW}[6/7] Configuration file already present${NC}"
    echo -e "${GREEN}Using default API URL: $API_BASE_URL${NC}"
fi

echo -e "${GREEN}Files copied successfully.${NC}"

# Step 7: Create and load launchd service
echo -e "${YELLOW}[7/7] Installing launchd service...${NC}"
cat > "$PLIST_PATH" << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>Label</key>
    <string>$SERVICE_LABEL</string>

    <key>ProgramArguments</key>
    <array>
        <string>$INSTALL_PATH/DonkeyWork.DeviceManager.DeviceClient</string>
    </array>

    <key>WorkingDirectory</key>
    <string>$INSTALL_PATH</string>

    <key>RunAtLoad</key>
    <true/>

    <key>KeepAlive</key>
    <dict>
        <key>SuccessfulExit</key>
        <false/>
    </dict>

    <key>StandardOutPath</key>
    <string>/var/log/donkeywork-device-client.log</string>

    <key>StandardErrorPath</key>
    <string>/var/log/donkeywork-device-client-error.log</string>

    <key>EnvironmentVariables</key>
    <dict>
        <key>DOTNET_PRINT_TELEMETRY_MESSAGE</key>
        <string>false</string>
    </dict>
</dict>
</plist>
EOF

# Set proper permissions on plist
chmod 644 "$PLIST_PATH"
chown root:wheel "$PLIST_PATH"

# Load the service
echo -e "${YELLOW}Starting service...${NC}"
launchctl bootstrap system "$PLIST_PATH"

# Wait a moment and check if service is loaded
sleep 2
if launchctl print system/"$SERVICE_LABEL" &> /dev/null; then
    echo -e "${GREEN}Service started successfully!${NC}"
else
    echo -e "${YELLOW}WARNING: Service was created but may not be running.${NC}"
    echo -e "${YELLOW}Check logs: tail -f /var/log/donkeywork-device-client.log${NC}"
fi

echo ""
echo -e "${CYAN}========================================${NC}"
echo -e "${GREEN}Installation Complete!${NC}"
echo -e "${CYAN}========================================${NC}"
echo ""
echo -e "Service Label:      ${SERVICE_LABEL}"
echo -e "Installation Path:  ${INSTALL_PATH}"
echo -e "Configuration:      ${INSTALL_PATH}/appsettings.json"
echo -e "API Base URL:       ${API_BASE_URL}"
echo -e "Architecture:       ${RUNTIME}"
echo ""
echo -e "${YELLOW}NEXT STEPS:${NC}"
echo -e "1. The device client service is now running"
echo -e "2. View logs:        tail -f /var/log/donkeywork-device-client.log"
echo -e "3. Check status:     launchctl print system/$SERVICE_LABEL"
echo -e "4. Stop service:     sudo launchctl bootout system $PLIST_PATH"
echo -e "5. To uninstall:     Run sudo ./uninstall.sh"
echo ""
echo -e "${CYAN}For more information, visit: https://github.com/donkeywork/device-manager${NC}"
